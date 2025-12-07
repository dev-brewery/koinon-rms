import { useEffect, useRef, useState, useCallback } from 'react';
import { Html5Qrcode } from 'html5-qrcode';

export interface UseQrScannerOptions {
  onScan: (familyIdKey: string) => void;
  onError?: (error: string) => void;
}

export interface UseQrScannerResult {
  isScanning: boolean;
  isCameraReady: boolean;
  error: string | null;
  startScanning: (elementId: string) => Promise<void>;
  stopScanning: () => Promise<void>;
}

const QR_CODE_PREFIX = 'koinon://family/';

// IdKey validation pattern - alphanumeric, underscores, and hyphens
// Minimum length 8 to prevent trivial brute force attacks
const ID_KEY_PATTERN = /^[A-Za-z0-9_-]{8,}$/;
const ID_KEY_MIN_LENGTH = 8;

/**
 * Custom hook for QR code scanning with html5-qrcode
 * Handles camera lifecycle and QR code parsing
 */
export function useQrScanner({ onScan, onError }: UseQrScannerOptions): UseQrScannerResult {
  const [isScanning, setIsScanning] = useState(false);
  const [isCameraReady, setIsCameraReady] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const scannerRef = useRef<Html5Qrcode | null>(null);
  const isCleaningUpRef = useRef(false);

  const stopScanning = useCallback(async (): Promise<void> => {
    if (!scannerRef.current || isCleaningUpRef.current) {
      return;
    }

    try {
      isCleaningUpRef.current = true;
      if (scannerRef.current.isScanning) {
        await scannerRef.current.stop();
      }
      scannerRef.current.clear();
    } catch (err) {
      // Log cleanup errors but don't throw - cleanup should be resilient
      if (import.meta.env.DEV) {
        console.error('Failed to stop scanner:', err);
      }
    } finally {
      scannerRef.current = null;
      setIsScanning(false);
      setIsCameraReady(false);
      setError(null);
      isCleaningUpRef.current = false;
    }
  }, []);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (scannerRef.current?.isScanning && !isCleaningUpRef.current) {
        isCleaningUpRef.current = true;
        scannerRef.current.stop().catch((err) => {
          if (import.meta.env.DEV) {
            console.error('Failed to stop scanner on unmount:', err);
          }
        });
      }
    };
  }, []);

  const startScanning = useCallback(async (elementId: string): Promise<void> => {
    if (isScanning) {
      return;
    }

    try {
      setError(null);
      setIsScanning(true);

      // Initialize scanner
      const scanner = new Html5Qrcode(elementId);
      scannerRef.current = scanner;

      // QR code scan success callback
      const onScanSuccess = (decodedText: string) => {
        // Parse QR code format: koinon://family/{idKey}
        if (decodedText.startsWith(QR_CODE_PREFIX)) {
          const familyIdKey = decodedText.substring(QR_CODE_PREFIX.length);

          // Validate IdKey format and length
          if (!familyIdKey) {
            const errorMsg = 'Invalid QR code format - missing IdKey';
            setError(errorMsg);
            onError?.(errorMsg);
            return;
          }

          if (familyIdKey.length < ID_KEY_MIN_LENGTH) {
            const errorMsg = 'Invalid QR code format - IdKey too short';
            setError(errorMsg);
            onError?.(errorMsg);
            return;
          }

          if (!ID_KEY_PATTERN.test(familyIdKey)) {
            const errorMsg = 'Invalid QR code format - IdKey contains invalid characters';
            setError(errorMsg);
            onError?.(errorMsg);
            return;
          }

          onScan(familyIdKey);
        } else {
          const errorMsg = 'QR code is not a Koinon family code';
          setError(errorMsg);
          onError?.(errorMsg);
        }
      };

      // QR code scan error callback (throttled by library)
      const onScanFailure = (errorMessage: string) => {
        // Most failures are just "No QR code found" which is normal
        // Only log actual errors in development
        if (import.meta.env.DEV && !errorMessage.includes('No MultiFormat Readers')) {
          console.debug('QR scan:', errorMessage);
        }
      };

      // Start camera with back camera preferred (kiosk mode)
      await scanner.start(
        { facingMode: 'environment' }, // Back camera
        {
          fps: 10, // Scan 10 times per second
          qrbox: { width: 250, height: 250 }, // Scanning box size
          aspectRatio: 1.0,
        },
        onScanSuccess,
        onScanFailure
      );

      setIsCameraReady(true);
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to start camera';

      // Provide user-friendly error messages
      let userMsg = errorMsg;
      if (errorMsg.includes('NotAllowedError') || errorMsg.includes('Permission denied')) {
        userMsg = 'Camera access denied. Please allow camera permissions.';
      } else if (errorMsg.includes('NotFoundError') || errorMsg.includes('No camera found')) {
        userMsg = 'No camera found on this device.';
      } else if (errorMsg.includes('NotReadableError')) {
        userMsg = 'Camera is being used by another application.';
      }

      setError(userMsg);
      setIsScanning(false);
      onError?.(userMsg);
    }
  }, [onScan, onError]);

  return {
    isScanning,
    isCameraReady,
    error,
    startScanning,
    stopScanning,
  };
}
