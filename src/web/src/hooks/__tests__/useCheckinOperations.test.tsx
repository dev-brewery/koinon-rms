/**
 * useCheckinOperations hooks: live dashboard query + toggle mutation (#482).
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/checkinOperations', () => ({
  getCheckinOperationsDashboard: vi.fn(),
  toggleCheckinOperationsRoom: vi.fn(),
}));

import * as api from '@/services/api/checkinOperations';
import {
  CHECKIN_OPS_POLL_INTERVAL_MS,
  CHECKIN_OPS_QUERY_KEY,
  useCheckinOperationsDashboard,
  useToggleCheckinOperationsRoom,
} from '../useCheckinOperations';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useCheckinOperationsDashboard', () => {
  it('polls constants are stable', () => {
    expect(CHECKIN_OPS_POLL_INTERVAL_MS).toBe(5000);
    expect(CHECKIN_OPS_QUERY_KEY).toEqual(['checkin-operations', 'dashboard']);
  });

  it('fetches dashboard by default (enabled=true)', async () => {
    vi.mocked(api.getCheckinOperationsDashboard).mockResolvedValueOnce({
      rooms: [],
      attendees: [],
      summary: { totalCheckedIn: 0, currentlyPresent: 0, checkedOut: 0 },
      generatedAt: 'now',
    } as never);
    const { result } = renderHookWithQuery(() => useCheckinOperationsDashboard());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getCheckinOperationsDashboard).toHaveBeenCalled();
  });

  it('is idle when enabled=false', () => {
    const { result } = renderHookWithQuery(() => useCheckinOperationsDashboard(false));
    expect(result.current.fetchStatus).toBe('idle');
    expect(api.getCheckinOperationsDashboard).not.toHaveBeenCalled();
  });
});

describe('useToggleCheckinOperationsRoom', () => {
  it('invalidates dashboard query on success', async () => {
    vi.mocked(api.toggleCheckinOperationsRoom).mockResolvedValueOnce({
      locationIdKey: 'l1',
      isOpen: false,
    } as never);
    const { result, client } = renderHookWithQuery(() =>
      useToggleCheckinOperationsRoom()
    );
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('l1');
    });
    expect(api.toggleCheckinOperationsRoom).toHaveBeenCalledWith('l1');
    expect(inv).toHaveBeenCalledWith({ queryKey: CHECKIN_OPS_QUERY_KEY });
  });
});
