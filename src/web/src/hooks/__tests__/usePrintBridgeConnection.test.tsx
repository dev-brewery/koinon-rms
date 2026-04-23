/**
 * usePrintBridgeConnection tests
 *
 * Drives the PrintBridge polling state-machine:
 *   - Initial status = 'checking'.
 *   - Health success + printer found -> 'available' + printer set.
 *   - Health success + printer data=null -> 'no-printer'.
 *   - Health failure with "Cannot connect" -> 'unavailable' + error.type='not-running'.
 *   - Health failure with "timeout" -> 'unavailable' + error.type='timeout'.
 *   - getDefaultZebraPrinter failure -> 'unavailable' + error.
 *   - retry() calls checkHealth immediately.
 *
 * We mock printBridgeClient and use fake timers to control the polling loop.
 */
import { act, renderHook, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/services/printing/PrintBridgeClient', () => ({
  printBridgeClient: {
    checkHealth: vi.fn(),
    getDefaultZebraPrinter: vi.fn(),
  },
}));

import { printBridgeClient } from '@/services/printing/PrintBridgeClient';
import { usePrintBridgeConnection } from '../usePrintBridgeConnection';

const zebraDefault = {
  name: 'ZD420',
  status: 'idle',
  isDefault: true,
  isZebraPrinter: true,
  driverName: '',
  portName: '',
};

describe('usePrintBridgeConnection', () => {
  beforeEach(() => {
    vi.mocked(printBridgeClient.checkHealth).mockReset();
    vi.mocked(printBridgeClient.getDefaultZebraPrinter).mockReset();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('starts with status=checking and transitions to available when healthy', async () => {
    vi.mocked(printBridgeClient.checkHealth).mockResolvedValue({
      success: true,
      data: { status: 'ok', version: '1', timestamp: 't' },
    });
    vi.mocked(printBridgeClient.getDefaultZebraPrinter).mockResolvedValue({
      success: true,
      data: zebraDefault,
    });

    const { result, unmount } = renderHook(() => usePrintBridgeConnection());
    expect(result.current.status).toBe('checking');
    await waitFor(() => expect(result.current.status).toBe('available'));
    expect(result.current.printer).toEqual(zebraDefault);
    expect(result.current.error).toBeNull();
    unmount();
  });

  it('sets status=no-printer when no Zebra printer is attached', async () => {
    vi.mocked(printBridgeClient.checkHealth).mockResolvedValue({
      success: true,
      data: { status: 'ok', version: '1', timestamp: 't' },
    });
    vi.mocked(printBridgeClient.getDefaultZebraPrinter).mockResolvedValue({
      success: true,
      data: null,
    });

    const { result, unmount } = renderHook(() => usePrintBridgeConnection());
    await waitFor(() => expect(result.current.status).toBe('no-printer'));
    expect(result.current.error?.type).toBe('no-printer');
    unmount();
  });

  it('categorizes "Cannot connect" as not-running', async () => {
    vi.mocked(printBridgeClient.checkHealth).mockResolvedValue({
      success: false,
      error: 'Cannot connect to print bridge: is it running?',
    });

    const { result, unmount } = renderHook(() => usePrintBridgeConnection());
    await waitFor(() => expect(result.current.status).toBe('unavailable'));
    expect(result.current.error?.type).toBe('not-running');
    unmount();
  });

  it('categorizes "timeout" as timeout', async () => {
    vi.mocked(printBridgeClient.checkHealth).mockResolvedValue({
      success: false,
      error: 'Print bridge connection timeout - is it running?',
    });

    const { result, unmount } = renderHook(() => usePrintBridgeConnection());
    await waitFor(() => expect(result.current.status).toBe('unavailable'));
    // "timeout" wins over "is it running" because includes('timeout') fires first.
    expect(result.current.error?.type).toBe('timeout');
    unmount();
  });

  it('categorizes arbitrary messages as unknown', async () => {
    vi.mocked(printBridgeClient.checkHealth).mockResolvedValue({
      success: false,
      error: 'weird thing happened',
    });

    const { result, unmount } = renderHook(() => usePrintBridgeConnection());
    await waitFor(() => expect(result.current.status).toBe('unavailable'));
    expect(result.current.error?.type).toBe('unknown');
    unmount();
  });

  it('surfaces getDefaultZebraPrinter failures as unavailable', async () => {
    vi.mocked(printBridgeClient.checkHealth).mockResolvedValue({
      success: true,
      data: { status: 'ok', version: '1', timestamp: 't' },
    });
    vi.mocked(printBridgeClient.getDefaultZebraPrinter).mockResolvedValue({
      success: false,
      error: 'Cannot connect to print bridge',
    });

    const { result, unmount } = renderHook(() => usePrintBridgeConnection());
    await waitFor(() => expect(result.current.status).toBe('unavailable'));
    expect(result.current.error?.type).toBe('not-running');
    unmount();
  });

  it('retry() triggers a new health check', async () => {
    vi.mocked(printBridgeClient.checkHealth).mockResolvedValue({
      success: false,
      error: 'Cannot connect',
    });

    const { result, unmount } = renderHook(() => usePrintBridgeConnection());
    await waitFor(() => expect(result.current.status).toBe('unavailable'));
    const callCountBefore = vi.mocked(printBridgeClient.checkHealth).mock.calls.length;

    act(() => result.current.retry());
    // Another call should fire.
    await waitFor(() => {
      expect(vi.mocked(printBridgeClient.checkHealth).mock.calls.length).toBeGreaterThan(
        callCountBefore
      );
    });
    unmount();
  });
});
