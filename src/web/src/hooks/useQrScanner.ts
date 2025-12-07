import { useEffect, useRef, useState, useCallback } from 'react';
import { Html5Qrcode } from 'html5-qrcode';
import { validateQrCode } from '@/utils/qrValidation';

export interface UseQrScannerOptions {
  onScan: (idKey: string) => void;
  onError?: (error: string) => void;
  fps?: number;
  qrbox?: number | { width: number; height: number };
}

export interface UseQrScannerResult {
  isScanning: boolean;
  startScanning: () => Promise<void>;
  stopScanning: () => Promise<void>;
  error: string | null;
}

/**
 * Hook for QR code scanning using html5-qrcode
 * Supports both camera scanning and keyboard wedge barcode scanners
 */
export function useQrScanner({
  onScan,
  onError,
  fps = 10,
  qrbox = 250,
}: UseQrScannerOptions): UseQrScannerResult {
  const scannerRef = useRef<Html5Qrcode | null>(null);
  const [isScanning, setIsScanning] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const keyBufferRef = useRef<string>('');
  const keyTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Use refs to avoid stale closures in event handlers
  const onScanRef = useRef(onScan);
  const onErrorRef = useRef(onError);

  // Keep refs up to date
  useEffect(() => {
    onScanRef.current = onScan;
    onErrorRef.current = onError;
  }, [onScan, onError]);

  // Process and validate scanned QR code
  const processQrCode = useCallback((decodedText: string) => {
    const result = validateQrCode(decodedText);

    if (result.valid && result.idKey) {
      onScanRef.current(result.idKey);
    } else if (onErrorRef.current) {
      onErrorRef.current(result.error || 'Invalid QR code');
    }
  }, []);

  // Keyboard wedge scanner handler - stable reference using refs
  useEffect(() => {
    const handleKeyPress = (event: KeyboardEvent) => {
      // Ignore if user is typing in an input field
      if (
        event.target instanceof HTMLInputElement ||
        event.target instanceof HTMLTextAreaElement
      ) {
        return;
      }

      // Clear previous timer
      if (keyTimerRef.current) {
        clearTimeout(keyTimerRef.current);
      }

      // Handle Enter key - process buffered input
      if (event.key === 'Enter') {
        if (keyBufferRef.current.length > 0) {
          const scanned = keyBufferRef.current.trim();
          keyBufferRef.current = '';
          processQrCode(scanned);
        }
        return;
      }

      // Append character to buffer (keyboard scanners type very fast)
      if (event.key.length === 1) {
        keyBufferRef.current += event.key;

        // Auto-process after 100ms of no input (keyboard scanner finishes)
        keyTimerRef.current = setTimeout(() => {
          if (keyBufferRef.current.length > 0) {
            const scanned = keyBufferRef.current.trim();
            keyBufferRef.current = '';
            processQrCode(scanned);
          }
        }, 100);
      }
    };

    // Add keyboard listener when scanning starts
    if (isScanning) {
      document.addEventListener('keypress', handleKeyPress);
    }

    // Cleanup
    return () => {
      document.removeEventListener('keypress', handleKeyPress);
      if (keyTimerRef.current) {
        clearTimeout(keyTimerRef.current);
        keyTimerRef.current = null;
      }
      keyBufferRef.current = '';
    };
  }, [isScanning, processQrCode]);

  // Start camera scanning
  const startScanning = useCallback(async () => {
    setError(null);

    // Check if DOM element exists
    const element = document.getElementById('qr-reader');
    if (!element) {
      const errorMsg = 'QR reader element not found';
      setError(errorMsg);
      if (onErrorRef.current) {
        onErrorRef.current(errorMsg);
      }
      return;
    }

    try {
      const scanner = new Html5Qrcode('qr-reader');
      scannerRef.current = scanner;

      await scanner.start(
        { facingMode: 'environment' }, // Use back camera
        {
          fps,
          qrbox,
        },
        (decodedText) => {
          processQrCode(decodedText);
        },
        (errorMessage) => {
          // Ignore common scanning errors (no QR in frame)
          // Only log critical errors
          if (!errorMessage.includes('NotFoundException')) {
            console.debug('QR Scan Error:', errorMessage);
          }
        }
      );

      setIsScanning(true);
    } catch (err) {
      const errorMsg =
        err instanceof Error ? err.message : 'Failed to start camera';
      setError(errorMsg);
      if (onErrorRef.current) {
        onErrorRef.current(errorMsg);
      }
    }
  }, [fps, qrbox, processQrCode]);

  // Stop camera scanning
  const stopScanning = useCallback(async () => {
    if (scannerRef.current?.isScanning) {
      try {
        await scannerRef.current.stop();
        await scannerRef.current.clear();
      } catch (err) {
        console.error('Error stopping scanner:', err);
      }
    }

    scannerRef.current = null;
    setIsScanning(false);
  }, []);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (scannerRef.current?.isScanning) {
        scannerRef.current.stop().catch(console.error);
      }
    };
  }, []);

  return {
    isScanning,
    startScanning,
    stopScanning,
    error,
  };
}
