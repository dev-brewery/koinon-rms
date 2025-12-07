import { useState, useCallback, useRef, useEffect } from 'react';
import { validateSupervisorPin } from '@/services/api/auth';

export interface UseSupervisorModeOptions {
  timeout?: number; // Auto-exit timeout in milliseconds
}

export interface UseSupervisorModeReturn {
  isActive: boolean;
  supervisorName: string | null;
  personIdKey: string | null;
  enterSupervisorMode: (pin: string) => Promise<boolean>;
  exitSupervisorMode: () => void;
  idleTimeRemaining: number;
  resetIdleTimer: () => void;
  lastError: string | null;
}

// Read timeout from environment variable or use default
const getDefaultTimeout = (): number => {
  const envTimeout = import.meta.env.VITE_SUPERVISOR_TIMEOUT_MS;
  if (envTimeout) {
    const parsed = parseInt(envTimeout, 10);
    if (!isNaN(parsed) && parsed > 0) {
      return parsed;
    }
  }
  return 2 * 60 * 1000; // Default: 2 minutes
};

const DEFAULT_TIMEOUT = getDefaultTimeout();

/**
 * Hook to manage supervisor mode state and authentication
 *
 * Features:
 * - PIN-based authentication via API
 * - Auto-exit after timeout (configurable via VITE_SUPERVISOR_TIMEOUT_MS)
 * - Idle timer reset on activity
 *
 * @example
 * ```tsx
 * const { isActive, supervisorName, enterSupervisorMode, exitSupervisorMode, idleTimeRemaining } =
 *   useSupervisorMode({ timeout: 120000 });
 *
 * // Enter mode
 * const success = await enterSupervisorMode('1234');
 *
 * // Exit mode
 * exitSupervisorMode();
 * ```
 */
export function useSupervisorMode(
  options: UseSupervisorModeOptions = {}
): UseSupervisorModeReturn {
  const { timeout = DEFAULT_TIMEOUT } = options;

  // State
  const [isActive, setIsActive] = useState(false);
  const [supervisorName, setSupervisorName] = useState<string | null>(null);
  const [personIdKey, setPersonIdKey] = useState<string | null>(null);
  const [idleTimeRemaining, setIdleTimeRemaining] = useState(0);
  const [lastError, setLastError] = useState<string | null>(null);

  // Refs for timers - use ReturnType for proper cross-environment typing
  const timeoutTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const countdownIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const startTimeRef = useRef<number>(Date.now());

  // Clear all timers
  const clearTimers = useCallback(() => {
    if (timeoutTimerRef.current !== null) {
      clearTimeout(timeoutTimerRef.current);
      timeoutTimerRef.current = null;
    }
    if (countdownIntervalRef.current !== null) {
      clearInterval(countdownIntervalRef.current);
      countdownIntervalRef.current = null;
    }
  }, []);

  // Calculate time remaining
  const calculateTimeRemaining = useCallback(() => {
    const elapsed = Date.now() - startTimeRef.current;
    const remaining = Math.max(0, timeout - elapsed);
    return Math.ceil(remaining / 1000);
  }, [timeout]);

  // Start countdown interval
  const startCountdown = useCallback(() => {
    // Update immediately
    setIdleTimeRemaining(calculateTimeRemaining());

    // Update every second
    countdownIntervalRef.current = setInterval(() => {
      const remaining = calculateTimeRemaining();
      setIdleTimeRemaining(remaining);

      if (remaining === 0) {
        if (countdownIntervalRef.current !== null) {
          clearInterval(countdownIntervalRef.current);
          countdownIntervalRef.current = null;
        }
      }
    }, 1000);
  }, [calculateTimeRemaining]);

  // Exit supervisor mode
  const exitSupervisorMode = useCallback(() => {
    setIsActive(false);
    setSupervisorName(null);
    setPersonIdKey(null);
    setIdleTimeRemaining(0);
    clearTimers();
  }, [clearTimers]);

  // Reset idle timer (called on activity)
  const resetIdleTimer = useCallback(() => {
    if (!isActive) {
      return;
    }

    // Reset start time
    startTimeRef.current = Date.now();

    // Clear existing timers
    clearTimers();

    // Start new countdown
    startCountdown();

    // Set timeout
    timeoutTimerRef.current = setTimeout(() => {
      exitSupervisorMode();
    }, timeout);
  }, [isActive, timeout, clearTimers, startCountdown, exitSupervisorMode]);

  // Enter supervisor mode with PIN validation
  const enterSupervisorMode = useCallback(
    async (pin: string): Promise<boolean> => {
      try {
        setLastError(null); // Clear previous error
        const response = await validateSupervisorPin(pin);

        if (response.valid && response.supervisorName && response.personIdKey) {
          setIsActive(true);
          setSupervisorName(response.supervisorName);
          setPersonIdKey(response.personIdKey);

          // Start idle timer
          startTimeRef.current = Date.now();
          startCountdown();

          timeoutTimerRef.current = setTimeout(() => {
            exitSupervisorMode();
          }, timeout);

          return true;
        }

        // Invalid PIN (not a network error)
        setLastError('Invalid PIN');
        return false;
      } catch (error) {
        // Network or other error
        const errorMessage = error instanceof Error ? error.message : 'Network error occurred';
        setLastError(errorMessage);
        if (import.meta.env.DEV) {
          console.error('Supervisor PIN validation failed:', error);
        }
        return false;
      }
    },
    [timeout, startCountdown, exitSupervisorMode]
  );

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      clearTimers();
    };
  }, [clearTimers]);

  return {
    isActive,
    supervisorName,
    personIdKey,
    enterSupervisorMode,
    exitSupervisorMode,
    idleTimeRemaining,
    resetIdleTimer,
    lastError,
  };
}
