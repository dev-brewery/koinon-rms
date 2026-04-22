/**
 * useSupervisorMode — local session state with activity timeout + backend-expiry
 * polling. All timers are faked so we can drive the two intervals deterministically.
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { act, renderHook } from '@testing-library/react';
import { useSupervisorMode } from '../useSupervisorMode';

function makeSession(overrides: Partial<{ expiresAt: string; token: string }> = {}) {
  return {
    token: 'tok',
    supervisor: {
      idKey: 's1',
      firstName: 'Ad',
      lastName: 'Min',
      fullName: 'Admin',
    } as never,
    expiresAt: overrides.expiresAt ?? new Date(Date.now() + 10 * 60 * 1000).toISOString(),
    ...overrides,
  };
}

beforeEach(() => {
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('useSupervisorMode', () => {
  it('starts inactive', () => {
    const { result } = renderHook(() => useSupervisorMode());
    expect(result.current.isActive).toBe(false);
    expect(result.current.supervisor).toBeNull();
    expect(result.current.sessionToken).toBeNull();
    expect(result.current.timeRemaining).toBeNull();
  });

  it('startSession activates + computes timeRemaining after one tick', () => {
    const { result } = renderHook(() => useSupervisorMode());
    act(() => {
      result.current.startSession(makeSession());
    });
    expect(result.current.isActive).toBe(true);
    expect(result.current.sessionToken).toBe('tok');

    // Advance 1s to trigger activity-timeout interval and backend-check interval.
    act(() => {
      vi.advanceTimersByTime(1000);
    });
    expect(result.current.timeRemaining).toBeGreaterThan(0);
    // 2-minute inactivity window; at 1s elapsed we should see ~119 seconds.
    expect(result.current.timeRemaining).toBeLessThanOrEqual(120);
  });

  it('endSession clears state', () => {
    const { result } = renderHook(() => useSupervisorMode());
    act(() => {
      result.current.startSession(makeSession());
    });
    act(() => {
      result.current.endSession();
    });
    expect(result.current.isActive).toBe(false);
    expect(result.current.supervisor).toBeNull();
    expect(result.current.timeRemaining).toBeNull();
  });

  it('auto-expires after 2 minutes of inactivity', () => {
    const { result } = renderHook(() => useSupervisorMode());
    act(() => {
      result.current.startSession(makeSession());
    });
    act(() => {
      vi.advanceTimersByTime(2 * 60 * 1000 + 1000);
    });
    expect(result.current.isActive).toBe(false);
  });

  it('resetTimeout pushes the expiry window forward', () => {
    const { result } = renderHook(() => useSupervisorMode());
    act(() => {
      result.current.startSession(makeSession());
    });

    // After 90 seconds of inactivity, still active.
    act(() => {
      vi.advanceTimersByTime(90 * 1000);
    });
    expect(result.current.isActive).toBe(true);

    // Reset right before timeout — should continue past 2 minutes from original start.
    act(() => {
      result.current.resetTimeout();
    });

    // 60s more — still active (would have expired without reset).
    act(() => {
      vi.advanceTimersByTime(60 * 1000);
    });
    expect(result.current.isActive).toBe(true);
  });

  it('ends session when the backend expiresAt is in the past', () => {
    const { result } = renderHook(() => useSupervisorMode());
    // expiresAt already in the past — backend check runs immediately.
    act(() => {
      result.current.startSession(
        makeSession({ expiresAt: new Date(Date.now() - 1000).toISOString() })
      );
    });
    // The "check immediately" path fires in the effect; allow React to flush.
    act(() => {
      vi.advanceTimersByTime(0);
    });
    expect(result.current.isActive).toBe(false);
  });
});
