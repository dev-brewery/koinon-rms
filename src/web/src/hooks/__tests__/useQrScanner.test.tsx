/**
 * useQrScanner tests - wave 7 of #679.
 *
 * The hook drives two scanning paths:
 *   1. Camera-based: wraps `Html5Qrcode` from html5-qrcode. We mock the class
 *      entirely so no real camera permission / media track is touched.
 *   2. Keyboard wedge: attaches a `keypress` document listener while scanning.
 *      Wedge scanners type fast then press Enter (or pause >100ms). We drive
 *      both the Enter-terminated and timeout-terminated paths.
 *
 * Rationale for unit-not-E2E: camera permission grants, MediaDevices, and
 * device-specific camera availability are intrinsically flaky in browser
 * automation. Mocking at the library boundary gives deterministic coverage
 * of the hook's own state machine and wedge handling, which is where bugs
 * actually hide.
 */

import { renderHook, act } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

// Mutable mock state — allows each test to reshape scanner behaviour without
// re-mocking the module (vi.mock is hoisted and cannot close over per-test refs).
type ScanCallback = (text: string) => void;
type ScanErrorCallback = (err: string) => void;

interface MockScannerInstance {
  start: ReturnType<typeof vi.fn>;
  stop: ReturnType<typeof vi.fn>;
  clear: ReturnType<typeof vi.fn>;
  isScanning: boolean;
  // Captured callbacks from the last start() invocation so tests can drive them.
  successCb?: ScanCallback;
  errorCb?: ScanErrorCallback;
}

const scannerState: { instance: MockScannerInstance | null } = { instance: null };
// Tests set this to make the next scanner.start() throw a given error (simulates
// camera permission denied etc.).
let nextStartError: Error | null = null;

vi.mock('html5-qrcode', () => {
  class Html5Qrcode {
    start = vi.fn(
      async (
        _cameraConfig: unknown,
        _scanConfig: unknown,
        successCb: ScanCallback,
        errorCb: ScanErrorCallback
      ) => {
        if (nextStartError) {
          const e = nextStartError;
          nextStartError = null;
          throw e;
        }
        this.isScanning = true;
        this.successCb = successCb;
        this.errorCb = errorCb;
      }
    );
    stop = vi.fn(async () => {
      this.isScanning = false;
    });
    clear = vi.fn(async () => {});
    isScanning = false;
    successCb?: ScanCallback;
    errorCb?: ScanErrorCallback;

    constructor(_elementId: string) {
      scannerState.instance = this as unknown as MockScannerInstance;
    }
  }
  return { Html5Qrcode };
});

import { useQrScanner } from '../useQrScanner';

beforeEach(() => {
  scannerState.instance = null;
  nextStartError = null;
  // Hook reads document.getElementById('qr-reader') before creating a scanner;
  // provide it so the happy path can execute.
  const el = document.createElement('div');
  el.id = 'qr-reader';
  document.body.appendChild(el);
});

afterEach(() => {
  document.getElementById('qr-reader')?.remove();
  vi.restoreAllMocks();
});

describe('useQrScanner — camera path', () => {
  it('startScanning instantiates Html5Qrcode and flips isScanning', async () => {
    const onScan = vi.fn();
    const { result } = renderHook(() => useQrScanner({ onScan }));

    expect(result.current.isScanning).toBe(false);

    await act(async () => {
      await result.current.startScanning();
    });

    expect(scannerState.instance).not.toBeNull();
    expect(scannerState.instance?.start).toHaveBeenCalledTimes(1);
    expect(result.current.isScanning).toBe(true);
    expect(result.current.error).toBeNull();
  });

  it('routes a valid koinon:// QR payload to onScan with the IdKey', async () => {
    const onScan = vi.fn();
    const { result } = renderHook(() => useQrScanner({ onScan }));

    await act(async () => {
      await result.current.startScanning();
    });

    // Drive the library's success callback directly — the hook passes this to
    // Html5Qrcode.start() and we captured it in the mock.
    act(() => {
      scannerState.instance?.successCb?.('koinon://family/ABC123');
    });

    expect(onScan).toHaveBeenCalledWith('ABC123');
  });

  it('invokes onError for an invalid QR payload', async () => {
    const onScan = vi.fn();
    const onError = vi.fn();
    const { result } = renderHook(() => useQrScanner({ onScan, onError }));

    await act(async () => {
      await result.current.startScanning();
    });

    act(() => {
      scannerState.instance?.successCb?.('not-a-koinon-url');
    });

    expect(onScan).not.toHaveBeenCalled();
    expect(onError).toHaveBeenCalled();
  });

  it('surfaces camera permission denied (NotAllowedError) as hook error state', async () => {
    const onScan = vi.fn();
    const onError = vi.fn();
    nextStartError = Object.assign(new Error('Permission denied'), {
      name: 'NotAllowedError',
    });

    const { result } = renderHook(() => useQrScanner({ onScan, onError }));

    await act(async () => {
      await result.current.startScanning();
    });

    expect(result.current.isScanning).toBe(false);
    expect(result.current.error).toBe('Permission denied');
    expect(onError).toHaveBeenCalledWith('Permission denied');
  });

  it('sets an error when the required DOM element is missing', async () => {
    document.getElementById('qr-reader')?.remove();
    const onScan = vi.fn();
    const onError = vi.fn();
    const { result } = renderHook(() => useQrScanner({ onScan, onError }));

    await act(async () => {
      await result.current.startScanning();
    });

    expect(result.current.error).toBe('QR reader element not found');
    expect(scannerState.instance).toBeNull();
    expect(onError).toHaveBeenCalledWith('QR reader element not found');
  });

  it('stopScanning clears the scanner and resets isScanning', async () => {
    const onScan = vi.fn();
    const { result } = renderHook(() => useQrScanner({ onScan }));

    await act(async () => {
      await result.current.startScanning();
    });
    expect(result.current.isScanning).toBe(true);

    await act(async () => {
      await result.current.stopScanning();
    });

    expect(scannerState.instance?.stop).toHaveBeenCalled();
    expect(scannerState.instance?.clear).toHaveBeenCalled();
    expect(result.current.isScanning).toBe(false);
  });
});

describe('useQrScanner — keyboard wedge path', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it('processes Enter-terminated wedge input as a full scan', async () => {
    const onScan = vi.fn();
    const { result } = renderHook(() => useQrScanner({ onScan }));

    // Wedge listener only attaches while scanning; start a camera scan first.
    await act(async () => {
      await result.current.startScanning();
    });

    // Type the payload then press Enter.
    const payload = 'koinon://family/WEDGE1';
    act(() => {
      for (const ch of payload) {
        document.dispatchEvent(new KeyboardEvent('keypress', { key: ch }));
      }
      document.dispatchEvent(new KeyboardEvent('keypress', { key: 'Enter' }));
    });

    expect(onScan).toHaveBeenCalledWith('WEDGE1');
  });

  it('auto-processes wedge input after 100ms pause (no Enter)', async () => {
    const onScan = vi.fn();
    const { result } = renderHook(() => useQrScanner({ onScan }));

    await act(async () => {
      await result.current.startScanning();
    });

    const payload = 'koinon://family/PAUSED';
    act(() => {
      for (const ch of payload) {
        document.dispatchEvent(new KeyboardEvent('keypress', { key: ch }));
      }
    });

    // Before the 100ms debounce fires, onScan should not be called.
    expect(onScan).not.toHaveBeenCalled();

    act(() => {
      vi.advanceTimersByTime(100);
    });

    expect(onScan).toHaveBeenCalledWith('PAUSED');
  });

  it('ignores keypress events that originate in input/textarea fields', async () => {
    const onScan = vi.fn();
    const { result } = renderHook(() => useQrScanner({ onScan }));

    await act(async () => {
      await result.current.startScanning();
    });

    const input = document.createElement('input');
    document.body.appendChild(input);
    input.focus();

    act(() => {
      // Dispatch with the input as the target so the hook's early-return fires.
      const ev = new KeyboardEvent('keypress', { key: 'a', bubbles: true });
      Object.defineProperty(ev, 'target', { value: input });
      document.dispatchEvent(ev);
      const enter = new KeyboardEvent('keypress', { key: 'Enter', bubbles: true });
      Object.defineProperty(enter, 'target', { value: input });
      document.dispatchEvent(enter);
      vi.advanceTimersByTime(200);
    });

    expect(onScan).not.toHaveBeenCalled();
    input.remove();
  });
});
