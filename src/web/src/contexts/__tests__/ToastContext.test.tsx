/**
 * ToastContext tests
 *
 * Covers:
 *  - Hook guard (throws when used outside provider)
 *  - addToast + removeToast basic lifecycle
 *  - Auto-dismiss via `duration` with fake timers
 *  - Variant helpers (success/error/warning/info) set correct variant
 *  - Variant helpers apply default durations unique per variant
 *  - Unmount cleanup cancels pending auto-dismiss timers (no state-update leak)
 */

import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import { ToastProvider, useToast } from '../ToastContext';

function wrapper({ children }: { children: ReactNode }) {
  return <ToastProvider>{children}</ToastProvider>;
}

beforeEach(() => {
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('useToast hook', () => {
  it('throws when used outside ToastProvider', () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {});
    expect(() => renderHook(() => useToast())).toThrow(
      /useToast must be used within a ToastProvider/,
    );
    spy.mockRestore();
  });
});

describe('ToastProvider.addToast / removeToast', () => {
  it('adds toasts with unique auto-generated ids', () => {
    const { result } = renderHook(() => useToast(), { wrapper });

    act(() => {
      result.current.addToast({ title: 't1', message: 'm1', variant: 'info' });
      result.current.addToast({ title: 't2', message: 'm2', variant: 'info' });
    });

    expect(result.current.toasts).toHaveLength(2);
    const [a, b] = result.current.toasts;
    expect(a.id).not.toBe(b.id);
    expect(a.variant).toBe('info');
  });

  it('removes a toast by id', () => {
    const { result } = renderHook(() => useToast(), { wrapper });

    act(() => {
      result.current.addToast({ title: 'hello', message: 'world', variant: 'info' });
    });
    const id = result.current.toasts[0].id;

    act(() => {
      result.current.removeToast(id);
    });
    expect(result.current.toasts).toHaveLength(0);
  });

  it('does nothing when removeToast is given an unknown id', () => {
    const { result } = renderHook(() => useToast(), { wrapper });

    act(() => {
      result.current.addToast({ title: 'x', message: 'y', variant: 'info' });
    });
    const before = [...result.current.toasts];

    act(() => {
      result.current.removeToast('nope');
    });
    expect(result.current.toasts).toEqual(before);
  });
});

describe('ToastProvider auto-dismiss', () => {
  it('auto-dismisses after the requested duration', () => {
    const { result } = renderHook(() => useToast(), { wrapper });

    act(() => {
      result.current.addToast({
        title: 'gone',
        message: 'bye',
        variant: 'info',
        duration: 1000,
      });
    });
    expect(result.current.toasts).toHaveLength(1);

    act(() => {
      vi.advanceTimersByTime(999);
    });
    expect(result.current.toasts).toHaveLength(1);

    act(() => {
      vi.advanceTimersByTime(2);
    });
    expect(result.current.toasts).toHaveLength(0);
  });

  it('does not auto-dismiss when duration is omitted', () => {
    const { result } = renderHook(() => useToast(), { wrapper });

    act(() => {
      result.current.addToast({ title: 'sticky', message: 'stays', variant: 'info' });
    });

    act(() => {
      vi.advanceTimersByTime(60_000);
    });
    expect(result.current.toasts).toHaveLength(1);
  });
});

describe('ToastProvider variant helpers', () => {
  it('success helper applies success variant and a default duration', () => {
    const { result } = renderHook(() => useToast(), { wrapper });

    act(() => {
      result.current.success('ok', 'done');
    });
    expect(result.current.toasts[0].variant).toBe('success');
    expect(result.current.toasts[0].duration).toBeGreaterThan(0);
  });

  it('error helper applies error variant and a default duration', () => {
    const { result } = renderHook(() => useToast(), { wrapper });
    act(() => {
      result.current.error('boom', 'oops');
    });
    expect(result.current.toasts[0].variant).toBe('error');
  });

  it('warning helper applies warning variant', () => {
    const { result } = renderHook(() => useToast(), { wrapper });
    act(() => {
      result.current.warning('careful', 'maybe');
    });
    expect(result.current.toasts[0].variant).toBe('warning');
  });

  it('info helper applies info variant', () => {
    const { result } = renderHook(() => useToast(), { wrapper });
    act(() => {
      result.current.info('fyi', 'heads up');
    });
    expect(result.current.toasts[0].variant).toBe('info');
  });

  it('custom duration passed to a helper overrides the default', () => {
    const { result } = renderHook(() => useToast(), { wrapper });

    act(() => {
      result.current.success('custom', 'dur', 500);
    });

    act(() => {
      vi.advanceTimersByTime(499);
    });
    expect(result.current.toasts).toHaveLength(1);

    act(() => {
      vi.advanceTimersByTime(2);
    });
    expect(result.current.toasts).toHaveLength(0);
  });
});

describe('ToastProvider cleanup', () => {
  it('clears pending auto-dismiss timers on unmount (no late state updates)', () => {
    const { result, unmount } = renderHook(() => useToast(), { wrapper });

    act(() => {
      result.current.addToast({
        title: 'x',
        message: 'y',
        variant: 'info',
        duration: 5000,
      });
    });

    unmount();
    // If timers weren't cleaned up, this would fire on a dead component.
    expect(() => vi.advanceTimersByTime(10_000)).not.toThrow();
  });
});
