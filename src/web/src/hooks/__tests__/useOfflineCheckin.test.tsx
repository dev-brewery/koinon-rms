/**
 * useOfflineCheckin tests - wave 7 of #679.
 *
 * The hook wraps the already-well-tested `OfflineCheckinQueue` service (#498
 * wave 3, 84% coverage). We mock that module directly rather than touching
 * IndexedDB — the storage layer is not what this hook's tests should prove.
 *
 * What we DO prove here:
 *   - online/offline state tracks `navigator.onLine` + window events.
 *   - `recordCheckin` picks the online vs. offline path and falls back on
 *     network failure.
 *   - coming back online triggers a queue sync.
 *   - sync status transitions are reflected in hook state.
 *   - listeners are torn down on unmount.
 *
 * Rationale for unit-not-E2E: simulating offline/online transitions and
 * partial-network failures in Playwright is slow and flaky. Object.defineProperty
 * on `navigator.onLine` + dispatching `online`/`offline` events gives us
 * bit-for-bit deterministic coverage.
 */

import { act, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

// Mock the offline queue module — each test reshapes the mock for its case.
vi.mock('@/services/offline/OfflineCheckinQueue', () => ({
  offlineCheckinQueue: {
    getQueuedCount: vi.fn(),
    processQueue: vi.fn(),
    addToQueue: vi.fn(),
    clearQueue: vi.fn(),
  },
}));

// Mock recordAttendance — we don't exercise the network call in these tests.
vi.mock('@/services/api/checkin', () => ({
  recordAttendance: vi.fn(),
}));

// Import the mocked modules after vi.mock so we get the mocked references.
import { offlineCheckinQueue } from '@/services/offline/OfflineCheckinQueue';
import { recordAttendance } from '@/services/api/checkin';
import { ApiClientError } from '@/services/api/client';
import { useOfflineCheckin } from '../useOfflineCheckin';

/** Set navigator.onLine via property redefinition (read-only by default). */
function setOnline(value: boolean): void {
  Object.defineProperty(navigator, 'onLine', {
    configurable: true,
    get: () => value,
  });
}

beforeEach(() => {
  vi.mocked(offlineCheckinQueue.getQueuedCount).mockResolvedValue(0);
  vi.mocked(offlineCheckinQueue.processQueue).mockResolvedValue([]);
  vi.mocked(offlineCheckinQueue.addToQueue).mockResolvedValue('queue-id-1');
  vi.mocked(offlineCheckinQueue.clearQueue).mockResolvedValue(undefined);
  vi.mocked(recordAttendance).mockReset();
  setOnline(true);
});

afterEach(() => {
  vi.clearAllMocks();
});

describe('useOfflineCheckin — online/offline state', () => {
  it('reflects the initial navigator.onLine value', async () => {
    setOnline(true);
    const { result, unmount } = renderHookWithQuery(() => useOfflineCheckin());
    expect(result.current.state.isOnline).toBe(true);
    expect(result.current.state.mode).toBe('online');
    unmount();

    setOnline(false);
    const { result: offlineResult, unmount: offlineUnmount } = renderHookWithQuery(
      () => useOfflineCheckin()
    );
    expect(offlineResult.current.state.isOnline).toBe(false);
    expect(offlineResult.current.state.mode).toBe('offline');
    offlineUnmount();
  });

  it('flips to offline mode when window fires an offline event', async () => {
    const { result, unmount } = renderHookWithQuery(() => useOfflineCheckin());

    act(() => {
      setOnline(false);
      window.dispatchEvent(new Event('offline'));
    });

    await waitFor(() => expect(result.current.state.isOnline).toBe(false));
    expect(result.current.state.mode).toBe('offline');
    unmount();
  });

  it('auto-syncs the queue when an online event fires', async () => {
    setOnline(false);
    const { result, unmount } = renderHookWithQuery(() => useOfflineCheckin());
    await waitFor(() => expect(result.current.state.isOnline).toBe(false));

    vi.mocked(offlineCheckinQueue.processQueue).mockResolvedValueOnce([
      { id: 'a', success: true },
    ]);

    act(() => {
      setOnline(true);
      window.dispatchEvent(new Event('online'));
    });

    await waitFor(() =>
      expect(offlineCheckinQueue.processQueue).toHaveBeenCalledTimes(1)
    );
    unmount();
  });
});

describe('useOfflineCheckin — recordCheckin dispatch', () => {
  it('calls recordAttendance when online', async () => {
    setOnline(true);
    vi.mocked(recordAttendance).mockResolvedValueOnce({
      successCount: 1,
      failureCount: 0,
      results: [],
    } as never);

    const { result, unmount } = renderHookWithQuery(() => useOfflineCheckin());

    await act(async () => {
      await result.current.recordCheckin([{ personIdKey: 'p1' } as never]);
    });

    expect(recordAttendance).toHaveBeenCalledTimes(1);
    expect(offlineCheckinQueue.addToQueue).not.toHaveBeenCalled();
    unmount();
  });

  it('queues the request when offline instead of calling the API', async () => {
    setOnline(false);
    const { result, unmount } = renderHookWithQuery(() => useOfflineCheckin());

    await act(async () => {
      await result.current.recordCheckin([{ personIdKey: 'p2' } as never]);
    });

    expect(recordAttendance).not.toHaveBeenCalled();
    expect(offlineCheckinQueue.addToQueue).toHaveBeenCalledTimes(1);
    unmount();
  });

  it('falls back to the queue on a network failure (non-ApiClientError)', async () => {
    setOnline(true);
    vi.mocked(recordAttendance).mockRejectedValueOnce(
      new TypeError('Failed to fetch')
    );

    const { result, unmount } = renderHookWithQuery(() => useOfflineCheckin());

    await act(async () => {
      await result.current.recordCheckin([{ personIdKey: 'p3' } as never]);
    });

    expect(offlineCheckinQueue.addToQueue).toHaveBeenCalledTimes(1);
    unmount();
  });

  it('re-throws ApiClientError instead of queuing (server rejection)', async () => {
    setOnline(true);
    // ApiClientError's real constructor takes (status, errorData, format?, message?).
    // Use the legacy shape so it preserves message via `errorData.message`.
    vi.mocked(recordAttendance).mockRejectedValueOnce(
      new ApiClientError(400, { code: 'BAD', message: 'Bad request' } as never)
    );

    const { result, unmount } = renderHookWithQuery(() => useOfflineCheckin());

    await expect(
      result.current.recordCheckin([{ personIdKey: 'p4' } as never])
    ).rejects.toBeInstanceOf(ApiClientError);
    expect(offlineCheckinQueue.addToQueue).not.toHaveBeenCalled();
    unmount();
  });
});

describe('useOfflineCheckin — sync status', () => {
  it('clears queue when clearQueue() is invoked', async () => {
    const { result, unmount } = renderHookWithQuery(() => useOfflineCheckin());

    await act(async () => {
      await result.current.clearQueue();
    });

    expect(offlineCheckinQueue.clearQueue).toHaveBeenCalledTimes(1);
    unmount();
  });

  it('sets syncStatus=error when processQueue rejects', async () => {
    setOnline(true);
    vi.mocked(offlineCheckinQueue.processQueue).mockRejectedValueOnce(
      new Error('sync failed')
    );

    const { result, unmount } = renderHookWithQuery(() => useOfflineCheckin());

    await act(async () => {
      await result.current.syncQueue();
    });

    expect(result.current.state.syncStatus).toBe('error');
    unmount();
  });

  it('sets syncStatus=success when processQueue resolves without failures', async () => {
    setOnline(true);
    vi.mocked(offlineCheckinQueue.processQueue).mockResolvedValueOnce([
      { id: 'a', success: true },
    ]);

    const { result, unmount } = renderHookWithQuery(() => useOfflineCheckin());

    await act(async () => {
      await result.current.syncQueue();
    });

    expect(result.current.state.syncStatus).toBe('success');
    expect(result.current.state.lastSyncResults).toHaveLength(1);
    unmount();
  });

  it('sets syncStatus=error when any result in the batch has success=false', async () => {
    setOnline(true);
    vi.mocked(offlineCheckinQueue.processQueue).mockResolvedValueOnce([
      { id: 'a', success: true },
      { id: 'b', success: false, error: 'boom' },
    ]);

    const { result, unmount } = renderHookWithQuery(() => useOfflineCheckin());

    await act(async () => {
      await result.current.syncQueue();
    });

    expect(result.current.state.syncStatus).toBe('error');
    unmount();
  });

  it('no-ops syncQueue when offline', async () => {
    setOnline(false);
    const { result, unmount } = renderHookWithQuery(() => useOfflineCheckin());

    await act(async () => {
      await result.current.syncQueue();
    });

    expect(offlineCheckinQueue.processQueue).not.toHaveBeenCalled();
    unmount();
  });
});

describe('useOfflineCheckin — lifecycle', () => {
  it('removes online/offline listeners on unmount', () => {
    const addSpy = vi.spyOn(window, 'addEventListener');
    const removeSpy = vi.spyOn(window, 'removeEventListener');

    const { unmount } = renderHookWithQuery(() => useOfflineCheckin());

    const addedOnline = addSpy.mock.calls.some((c) => c[0] === 'online');
    const addedOffline = addSpy.mock.calls.some((c) => c[0] === 'offline');
    expect(addedOnline).toBe(true);
    expect(addedOffline).toBe(true);

    unmount();

    const removedOnline = removeSpy.mock.calls.some((c) => c[0] === 'online');
    const removedOffline = removeSpy.mock.calls.some((c) => c[0] === 'offline');
    expect(removedOnline).toBe(true);
    expect(removedOffline).toBe(true);
  });
});
