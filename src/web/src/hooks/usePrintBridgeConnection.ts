import { useCallback, useEffect, useRef, useState } from 'react';
import { printBridgeClient, type PrinterInfo } from '@/services/printing/PrintBridgeClient';

export type ConnectionStatus = 'checking' | 'available' | 'unavailable' | 'no-printer';

export interface ErrorDetails {
  type: 'not-running' | 'no-printer' | 'timeout' | 'unknown';
  message: string;
}

export interface PrintBridgeConnectionState {
  status: ConnectionStatus;
  printer: PrinterInfo | null;
  error: ErrorDetails | null;
  reconnectIn: number | null;
  retry: () => void;
}

const INITIAL_RETRY_DELAY = 2000; // 2 seconds
const MAX_RETRY_DELAY = 30000; // 30 seconds
const NORMAL_POLL_INTERVAL = 30000; // 30 seconds

/**
 * Determines the error type from the error message.
 */
function categorizeError(errorMessage: string): ErrorDetails['type'] {
  if (errorMessage.includes('timeout')) {
    return 'timeout';
  }
  if (errorMessage.includes('Cannot connect') || errorMessage.includes('is it running')) {
    return 'not-running';
  }
  return 'unknown';
}

/**
 * Custom hook for managing print bridge connection with exponential backoff.
 *
 * Features:
 * - Automatic reconnection with exponential backoff (2s, 4s, 8s, 16s, max 30s)
 * - Countdown to next retry
 * - Normal polling (30s) when connected
 * - Manual retry capability
 */
export function usePrintBridgeConnection(): PrintBridgeConnectionState {
  const [status, setStatus] = useState<ConnectionStatus>('checking');
  const [printer, setPrinter] = useState<PrinterInfo | null>(null);
  const [error, setError] = useState<ErrorDetails | null>(null);
  const [reconnectIn, setReconnectIn] = useState<number | null>(null);

  const retryDelayRef = useRef<number>(INITIAL_RETRY_DELAY);
  const intervalRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const countdownIntervalRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const nextRetryTimeRef = useRef<number | null>(null);

  const clearIntervals = useCallback(() => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
    if (countdownIntervalRef.current) {
      clearInterval(countdownIntervalRef.current);
      countdownIntervalRef.current = null;
    }
  }, []);

  const startCountdown = useCallback((delayMs: number) => {
    nextRetryTimeRef.current = Date.now() + delayMs;
    setReconnectIn(Math.ceil(delayMs / 1000));

    // Update countdown every second
    countdownIntervalRef.current = setInterval(() => {
      if (nextRetryTimeRef.current) {
        const remaining = Math.ceil((nextRetryTimeRef.current - Date.now()) / 1000);
        setReconnectIn(remaining > 0 ? remaining : 0);
      }
    }, 1000);
  }, []);

  const checkPrintBridge = useCallback(async () => {
    setStatus('checking');
    setError(null);
    setReconnectIn(null);

    // Clear countdown while checking
    if (countdownIntervalRef.current) {
      clearInterval(countdownIntervalRef.current);
      countdownIntervalRef.current = null;
    }

    // Check health first
    const healthResult = await printBridgeClient.checkHealth();

    if (!healthResult.success) {
      const errorType = categorizeError(healthResult.error);
      setStatus('unavailable');
      setError({
        type: errorType,
        message: healthResult.error,
      });

      // Exponential backoff for retry
      const currentDelay = retryDelayRef.current;
      startCountdown(currentDelay);

      // Double the delay for next time, up to max
      retryDelayRef.current = Math.min(currentDelay * 2, MAX_RETRY_DELAY);

      // Schedule next retry
      clearIntervals();
      intervalRef.current = setTimeout(() => {
        checkPrintBridge();
      }, currentDelay);

      return;
    }

    // Get default Zebra printer
    const printerResult = await printBridgeClient.getDefaultZebraPrinter();

    if (!printerResult.success) {
      const errorType = categorizeError(printerResult.error);
      setStatus('unavailable');
      setError({
        type: errorType,
        message: printerResult.error,
      });

      // Exponential backoff for retry
      const currentDelay = retryDelayRef.current;
      startCountdown(currentDelay);

      // Double the delay for next time, up to max
      retryDelayRef.current = Math.min(currentDelay * 2, MAX_RETRY_DELAY);

      // Schedule next retry
      clearIntervals();
      intervalRef.current = setTimeout(() => {
        checkPrintBridge();
      }, currentDelay);

      return;
    }

    if (printerResult.data === null) {
      setStatus('no-printer');
      setError({
        type: 'no-printer',
        message: 'No Zebra thermal printer found. Please install a Zebra printer.',
      });

      // Exponential backoff for retry
      const currentDelay = retryDelayRef.current;
      startCountdown(currentDelay);

      // Double the delay for next time, up to max
      retryDelayRef.current = Math.min(currentDelay * 2, MAX_RETRY_DELAY);

      // Schedule next retry
      clearIntervals();
      intervalRef.current = setTimeout(() => {
        checkPrintBridge();
      }, currentDelay);

      return;
    }

    // Success - reset to normal polling
    setStatus('available');
    setPrinter(printerResult.data);
    setError(null);
    setReconnectIn(null);

    // Reset retry delay to initial value
    retryDelayRef.current = INITIAL_RETRY_DELAY;

    // Normal polling interval
    clearIntervals();
    intervalRef.current = setTimeout(() => {
      checkPrintBridge();
    }, NORMAL_POLL_INTERVAL);
  }, [clearIntervals, startCountdown]);

  const retry = useCallback(() => {
    // Reset retry delay and immediately check
    retryDelayRef.current = INITIAL_RETRY_DELAY;
    clearIntervals();
    checkPrintBridge();
  }, [checkPrintBridge, clearIntervals]);

  useEffect(() => {
    checkPrintBridge();

    return () => {
      clearIntervals();
    };
  }, [checkPrintBridge, clearIntervals]);

  return {
    status,
    printer,
    error,
    reconnectIn,
    retry,
  };
}
