import { useState, useEffect, useCallback } from 'react';
import type { SupervisorInfoDto } from '@/services/api/types';

const SUPERVISOR_SESSION_TIMEOUT = 2 * 60 * 1000; // 2 minutes in milliseconds

export interface SupervisorSession {
  token: string;
  supervisor: SupervisorInfoDto;
  expiresAt: string;
}

export interface UseSupervisorModeResult {
  isActive: boolean;
  supervisor: SupervisorInfoDto | null;
  sessionToken: string | null;
  timeRemaining: number | null;
  startSession: (session: SupervisorSession) => void;
  endSession: () => void;
  resetTimeout: () => void;
}

/**
 * Hook to manage supervisor mode session state and auto-timeout
 */
export function useSupervisorMode(): UseSupervisorModeResult {
  const [session, setSession] = useState<SupervisorSession | null>(null);
  const [timeRemaining, setTimeRemaining] = useState<number | null>(null);
  const [lastActivity, setLastActivity] = useState<number>(Date.now());

  // Start supervisor session
  const startSession = useCallback((newSession: SupervisorSession) => {
    setSession(newSession);
    setLastActivity(Date.now());
  }, []);

  // End supervisor session
  const endSession = useCallback(() => {
    setSession(null);
    setTimeRemaining(null);
    setLastActivity(Date.now());
  }, []);

  // Reset activity timeout
  const resetTimeout = useCallback(() => {
    setLastActivity(Date.now());
  }, []);

  // Auto-timeout effect
  useEffect(() => {
    let interval: ReturnType<typeof setInterval> | null = null;

    if (session) {
      interval = setInterval(() => {
        const now = Date.now();
        const elapsed = now - lastActivity;
        const remaining = SUPERVISOR_SESSION_TIMEOUT - elapsed;

        if (remaining <= 0) {
          // Session expired
          endSession();
        } else {
          setTimeRemaining(Math.ceil(remaining / 1000)); // Convert to seconds
        }
      }, 1000);
    } else {
      setTimeRemaining(null);
    }

    return () => {
      if (interval) clearInterval(interval);
    };
  }, [session, lastActivity, endSession]);

  // Check backend expiration
  useEffect(() => {
    if (!session) return;

    const checkExpiration = () => {
      const expiresAt = new Date(session.expiresAt).getTime();
      const now = Date.now();

      if (now >= expiresAt) {
        endSession();
      }
    };

    const interval = setInterval(checkExpiration, 5000); // Check every 5 seconds
    checkExpiration(); // Check immediately

    return () => clearInterval(interval);
  }, [session, endSession]);

  return {
    isActive: session !== null,
    supervisor: session?.supervisor ?? null,
    sessionToken: session?.token ?? null,
    timeRemaining,
    startSession,
    endSession,
    resetTimeout,
  };
}
