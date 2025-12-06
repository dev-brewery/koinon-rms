import { useEffect, useCallback, useRef, useState } from 'react';

export interface UseIdleTimeoutOptions {
  timeout: number; // Total timeout in ms (default: 60000 = 60s)
  warningTime: number; // When to show warning in ms (default: 50000 = 50s)
  onTimeout: () => void; // Called when timeout expires
  onWarning?: () => void; // Called when warning time reached
  enabled?: boolean; // Enable/disable timeout
}

export interface UseIdleTimeoutReturn {
  isWarning: boolean;
  secondsRemaining: number;
  resetTimer: () => void;
}

const ACTIVITY_EVENTS = [
  'mousedown',
  'mousemove',
  'mouseup',
  'touchstart',
  'touchmove',
  'touchend',
  'keydown',
  'click',
  'scroll',
] as const;

/**
 * Hook to detect user inactivity and trigger timeout/warning callbacks
 *
 * Used for kiosk privacy protection - automatically resets screen after inactivity
 *
 * @example
 * ```tsx
 * const { isWarning, secondsRemaining, resetTimer } = useIdleTimeout({
 *   timeout: 60000,        // 60 seconds total
 *   warningTime: 50000,    // Warning at 50 seconds
 *   onTimeout: handleReset,
 *   enabled: step !== 'search',
 * });
 * ```
 */
export function useIdleTimeout(options: UseIdleTimeoutOptions): UseIdleTimeoutReturn {
  const {
    timeout,
    warningTime,
    onTimeout,
    onWarning,
    enabled = true,
  } = options;

  // State
  const [isWarning, setIsWarning] = useState(false);
  const [secondsRemaining, setSecondsRemaining] = useState(0);

  // Refs to track timers and mount status
  const isMountedRef = useRef(true);
  const lastActivityRef = useRef<number>(Date.now());
  const timeoutTimerRef = useRef<number | null>(null);
  const warningTimerRef = useRef<number | null>(null);
  const countdownIntervalRef = useRef<number | null>(null);

  // Calculate seconds remaining based on elapsed time
  const calculateSecondsRemaining = useCallback(() => {
    const elapsed = Date.now() - lastActivityRef.current;
    const remaining = Math.max(0, timeout - elapsed);
    return Math.ceil(remaining / 1000);
  }, [timeout]);

  // Clear all timers
  const clearTimers = useCallback(() => {
    if (timeoutTimerRef.current !== null) {
      window.clearTimeout(timeoutTimerRef.current);
      timeoutTimerRef.current = null;
    }
    if (warningTimerRef.current !== null) {
      window.clearTimeout(warningTimerRef.current);
      warningTimerRef.current = null;
    }
    if (countdownIntervalRef.current !== null) {
      window.clearInterval(countdownIntervalRef.current);
      countdownIntervalRef.current = null;
    }
  }, []);

  // Start countdown interval when warning shown
  const startCountdown = useCallback(() => {
    // Update immediately
    if (isMountedRef.current) {
      setSecondsRemaining(calculateSecondsRemaining());
    }

    // Update every second
    countdownIntervalRef.current = window.setInterval(() => {
      const remaining = calculateSecondsRemaining();

      if (isMountedRef.current) {
        setSecondsRemaining(remaining);
      }

      if (remaining === 0) {
        if (countdownIntervalRef.current !== null) {
          window.clearInterval(countdownIntervalRef.current);
          countdownIntervalRef.current = null;
        }
      }
    }, 1000);
  }, [calculateSecondsRemaining]);

  // Reset timer to current time and restart all timers
  const resetTimer = useCallback(() => {
    lastActivityRef.current = Date.now();

    if (isMountedRef.current) {
      setIsWarning(false);
      setSecondsRemaining(0);
    }

    clearTimers();

    if (!enabled) {
      return;
    }

    // Set timeout for warning
    warningTimerRef.current = window.setTimeout(() => {
      if (isMountedRef.current) {
        setIsWarning(true);
        onWarning?.();
        startCountdown();
      }
    }, warningTime);

    // Set timeout for final timeout
    timeoutTimerRef.current = window.setTimeout(() => {
      onTimeout();

      if (isMountedRef.current) {
        setIsWarning(false);
        setSecondsRemaining(0);
      }

      clearTimers();
    }, timeout);
  }, [enabled, timeout, warningTime, onTimeout, onWarning, clearTimers, startCountdown]);

  // Handle activity events
  const handleActivity = useCallback(() => {
    if (!enabled) {
      return;
    }

    // Only reset if not already in warning state OR if warning is active
    // This allows any interaction during warning to cancel it
    resetTimer();
  }, [enabled, resetTimer]);

  // Setup event listeners
  useEffect(() => {
    // Set mounted status
    isMountedRef.current = true;

    if (!enabled) {
      clearTimers();
      if (isMountedRef.current) {
        setIsWarning(false);
        setSecondsRemaining(0);
      }
      return;
    }

    // Register all activity event listeners
    ACTIVITY_EVENTS.forEach((event) => {
      window.addEventListener(event, handleActivity, { passive: true });
    });

    // Initialize timer on mount
    resetTimer();

    // Cleanup
    return () => {
      isMountedRef.current = false;
      ACTIVITY_EVENTS.forEach((event) => {
        window.removeEventListener(event, handleActivity);
      });
      clearTimers();
    };
  }, [enabled, handleActivity, resetTimer, clearTimers]);

  return {
    isWarning,
    secondsRemaining,
    resetTimer,
  };
}
