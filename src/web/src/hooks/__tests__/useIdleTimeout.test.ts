import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { useIdleTimeout } from '../useIdleTimeout';

describe('useIdleTimeout', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.useRealTimers();
  });

  it('should initialize with isWarning false and secondsRemaining 0', () => {
    const onTimeout = vi.fn();
    const { result } = renderHook(() =>
      useIdleTimeout({
        timeout: 60000,
        warningTime: 50000,
        onTimeout,
      })
    );

    expect(result.current.isWarning).toBe(false);
    expect(result.current.secondsRemaining).toBe(0);
  });

  it('should trigger warning after warningTime', () => {
    const onTimeout = vi.fn();
    const onWarning = vi.fn();
    const { result } = renderHook(() =>
      useIdleTimeout({
        timeout: 60000,
        warningTime: 50000,
        onTimeout,
        onWarning,
      })
    );

    // Fast-forward to warning time
    act(() => {
      vi.advanceTimersByTime(50000);
    });

    expect(result.current.isWarning).toBe(true);
    expect(onWarning).toHaveBeenCalledTimes(1);
    expect(onTimeout).not.toHaveBeenCalled();
  });

  it('should trigger timeout after timeout duration', () => {
    const onTimeout = vi.fn();
    const { result } = renderHook(() =>
      useIdleTimeout({
        timeout: 60000,
        warningTime: 50000,
        onTimeout,
      })
    );

    // Fast-forward to timeout
    act(() => {
      vi.advanceTimersByTime(60000);
    });

    expect(onTimeout).toHaveBeenCalledTimes(1);
    expect(result.current.isWarning).toBe(false);
    expect(result.current.secondsRemaining).toBe(0);
  });

  it('should show countdown during warning period', () => {
    const onTimeout = vi.fn();
    const { result } = renderHook(() =>
      useIdleTimeout({
        timeout: 60000,
        warningTime: 50000,
        onTimeout,
      })
    );

    // Fast-forward to warning time
    act(() => {
      vi.advanceTimersByTime(50000);
    });

    expect(result.current.isWarning).toBe(true);
    expect(result.current.secondsRemaining).toBe(10);

    // Fast-forward 1 second
    act(() => {
      vi.advanceTimersByTime(1000);
    });

    expect(result.current.secondsRemaining).toBe(9);

    // Fast-forward 5 more seconds
    act(() => {
      vi.advanceTimersByTime(5000);
    });

    expect(result.current.secondsRemaining).toBe(4);
  });

  it('should reset timer on user activity', () => {
    const onTimeout = vi.fn();
    const onWarning = vi.fn();
    const { result } = renderHook(() =>
      useIdleTimeout({
        timeout: 60000,
        warningTime: 50000,
        onTimeout,
        onWarning,
      })
    );

    // Fast-forward almost to warning
    act(() => {
      vi.advanceTimersByTime(45000);
    });

    // Simulate user activity
    act(() => {
      window.dispatchEvent(new MouseEvent('mousedown'));
    });

    // Fast-forward 45s more (should not trigger warning if timer was reset)
    act(() => {
      vi.advanceTimersByTime(45000);
    });

    expect(result.current.isWarning).toBe(false);
    expect(onWarning).not.toHaveBeenCalled();
    expect(onTimeout).not.toHaveBeenCalled();
  });

  it('should reset timer when resetTimer is called', () => {
    const onTimeout = vi.fn();
    const onWarning = vi.fn();
    const { result } = renderHook(() =>
      useIdleTimeout({
        timeout: 60000,
        warningTime: 50000,
        onTimeout,
        onWarning,
      })
    );

    // Fast-forward to warning
    act(() => {
      vi.advanceTimersByTime(50000);
    });

    expect(result.current.isWarning).toBe(true);

    // Call resetTimer
    act(() => {
      result.current.resetTimer();
    });

    expect(result.current.isWarning).toBe(false);
    expect(result.current.secondsRemaining).toBe(0);

    // Fast-forward again to warning time
    act(() => {
      vi.advanceTimersByTime(50000);
    });

    expect(result.current.isWarning).toBe(true);
    expect(onWarning).toHaveBeenCalledTimes(2); // Once initially, once after reset
  });

  it('should cancel warning on user activity during warning period', () => {
    const onTimeout = vi.fn();
    const { result } = renderHook(() =>
      useIdleTimeout({
        timeout: 60000,
        warningTime: 50000,
        onTimeout,
      })
    );

    // Fast-forward to warning
    act(() => {
      vi.advanceTimersByTime(50000);
    });

    expect(result.current.isWarning).toBe(true);

    // Simulate user activity during warning
    act(() => {
      window.dispatchEvent(new MouseEvent('click'));
    });

    expect(result.current.isWarning).toBe(false);
    expect(onTimeout).not.toHaveBeenCalled();
  });

  it('should not start timers when disabled', () => {
    const onTimeout = vi.fn();
    const onWarning = vi.fn();
    const { result } = renderHook(() =>
      useIdleTimeout({
        timeout: 60000,
        warningTime: 50000,
        onTimeout,
        onWarning,
        enabled: false,
      })
    );

    // Fast-forward past timeout
    act(() => {
      vi.advanceTimersByTime(70000);
    });

    expect(result.current.isWarning).toBe(false);
    expect(onWarning).not.toHaveBeenCalled();
    expect(onTimeout).not.toHaveBeenCalled();
  });

  it('should clear timers when enabled changes from true to false', () => {
    const onTimeout = vi.fn();
    const onWarning = vi.fn();
    let enabled = true;

    const { result, rerender } = renderHook(() =>
      useIdleTimeout({
        timeout: 60000,
        warningTime: 50000,
        onTimeout,
        onWarning,
        enabled,
      })
    );

    // Fast-forward to warning
    act(() => {
      vi.advanceTimersByTime(50000);
    });

    expect(result.current.isWarning).toBe(true);

    // Disable
    enabled = false;
    rerender();

    expect(result.current.isWarning).toBe(false);
    expect(result.current.secondsRemaining).toBe(0);

    // Fast-forward - should not trigger timeout
    act(() => {
      vi.advanceTimersByTime(20000);
    });

    expect(onTimeout).not.toHaveBeenCalled();
  });

  it('should restart timers when enabled changes from false to true', () => {
    const onTimeout = vi.fn();
    let enabled = false;

    const { rerender } = renderHook(() =>
      useIdleTimeout({
        timeout: 60000,
        warningTime: 50000,
        onTimeout,
        enabled,
      })
    );

    // Fast-forward - should not trigger anything
    act(() => {
      vi.advanceTimersByTime(70000);
    });

    expect(onTimeout).not.toHaveBeenCalled();

    // Enable
    enabled = true;
    rerender();

    // Fast-forward to timeout
    act(() => {
      vi.advanceTimersByTime(60000);
    });

    expect(onTimeout).toHaveBeenCalledTimes(1);
  });

  it('should cleanup timers on unmount', () => {
    const onTimeout = vi.fn();
    const { unmount } = renderHook(() =>
      useIdleTimeout({
        timeout: 60000,
        warningTime: 50000,
        onTimeout,
      })
    );

    // Unmount before timeout
    unmount();

    // Fast-forward past timeout
    act(() => {
      vi.advanceTimersByTime(70000);
    });

    // Should not trigger timeout after unmount
    expect(onTimeout).not.toHaveBeenCalled();
  });

  it('should handle multiple activity event types', () => {
    const onTimeout = vi.fn();
    renderHook(() =>
      useIdleTimeout({
        timeout: 60000,
        warningTime: 50000,
        onTimeout,
      })
    );

    const activityEvents = [
      'mousedown',
      'mousemove',
      'mouseup',
      'touchstart',
      'touchmove',
      'touchend',
      'keydown',
      'click',
      'scroll',
    ];

    activityEvents.forEach((eventType) => {
      // Fast-forward almost to timeout
      act(() => {
        vi.advanceTimersByTime(55000);
      });

      // Trigger activity event - this should reset the timer
      act(() => {
        window.dispatchEvent(new Event(eventType));
      });
    });

    // After all events, timeout should not have been called
    expect(onTimeout).not.toHaveBeenCalled();
  });

  it('should countdown to 0 when timeout is reached', () => {
    const onTimeout = vi.fn();
    const { result } = renderHook(() =>
      useIdleTimeout({
        timeout: 60000,
        warningTime: 50000,
        onTimeout,
      })
    );

    // Fast-forward to warning
    act(() => {
      vi.advanceTimersByTime(50000);
    });

    expect(result.current.secondsRemaining).toBe(10);

    // Fast-forward to 1 second before timeout
    act(() => {
      vi.advanceTimersByTime(9000);
    });

    expect(result.current.secondsRemaining).toBe(1);

    // Fast-forward to timeout
    act(() => {
      vi.advanceTimersByTime(1000);
    });

    expect(result.current.secondsRemaining).toBe(0);
    expect(onTimeout).toHaveBeenCalledTimes(1);
  });
});
