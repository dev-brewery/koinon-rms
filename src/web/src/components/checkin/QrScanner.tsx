import { useEffect, useState, useRef, useCallback } from 'react';
import { Button } from '@/components/ui';

export interface QrScannerProps {
  onScan: (familyIdKey: string) => void;
  onCancel?: () => void;
  onManualEntry?: () => void;
}

// Use shorter timeout in test/dev environments
const SCANNER_TIMEOUT =
  import.meta.env.VITE_QR_SCANNER_TIMEOUT
    ? Number(import.meta.env.VITE_QR_SCANNER_TIMEOUT)
    : import.meta.env.DEV
      ? 5000
      : 60000;

type ScannerStatus = 'active' | 'invalid' | 'not-found' | 'timed-out';

/**
 * QR code scanner modal for fast family check-in.
 * Renders as a fixed full-screen overlay with all required a11y attributes.
 * Listens for `qr-detected` CustomEvents on `document`.
 */
export function QrScanner({ onScan, onCancel, onManualEntry }: QrScannerProps) {
  const [status, setStatus] = useState<ScannerStatus>('active');
  const [cameraError, setCameraError] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [torchOn, setTorchOn] = useState(false);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const lastScanRef = useRef<string | null>(null);
  const lastScanTimeRef = useRef<number>(0);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const hasScannedRef = useRef(false);

  // Try to init camera — track permission error separately.
  // Uses a flag to avoid state updates after unmount.
  useEffect(() => {
    let cancelled = false;

    const initCamera = async () => {
      try {
        // Check if getUserMedia is available
        if (!navigator.mediaDevices?.getUserMedia) {
          if (!cancelled) setCameraError(true);
          return;
        }
        const stream = await navigator.mediaDevices.getUserMedia({
          video: { facingMode: 'environment' },
        });
        // Got permission — stop the stream (placeholder video element handles display)
        stream.getTracks().forEach((t) => t.stop());
      } catch {
        if (!cancelled) {
          setCameraError(true);
        }
      }
    };

    initCamera();
    return () => {
      cancelled = true;
    };
  }, []);

  // Timeout handler — runs whenever status is 'active'
  useEffect(() => {
    if (status !== 'active') return;

    timeoutRef.current = setTimeout(() => {
      if (!hasScannedRef.current) {
        setStatus('timed-out');
      }
    }, SCANNER_TIMEOUT);

    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, [status]);

  const handleClose = useCallback(() => {
    onCancel?.();
  }, [onCancel]);

  const handleManualEntry = useCallback(() => {
    if (onManualEntry) {
      onManualEntry();
    } else {
      onCancel?.();
    }
  }, [onCancel, onManualEntry]);

  const handleTryAgain = useCallback(() => {
    setStatus('active');
    setErrorMessage(null);
    setStatusMessage(null);
    lastScanRef.current = null;
    lastScanTimeRef.current = 0;
    hasScannedRef.current = false;
  }, []);

  // Listen for qr-detected custom events on document
  useEffect(() => {
    const handleQrDetected = (e: Event) => {
      const customEvent = e as CustomEvent;
      const detail = customEvent.detail;

      if (typeof detail !== 'string' || !detail) {
        setStatus('invalid');
        setErrorMessage('Invalid QR code — not recognized');
        return;
      }

      // Debounce: ignore rapid duplicate scans within 2s
      const now = Date.now();
      if (
        detail === lastScanRef.current &&
        now - lastScanTimeRef.current < 2000
      ) {
        return;
      }
      lastScanRef.current = detail;
      lastScanTimeRef.current = now;

      // Try to parse as JSON
      try {
        const parsed = JSON.parse(detail);
        if (parsed && typeof parsed === 'object') {
          // JSON format: extract id or phone
          const idKey = parsed.id || parsed.phone;
          if (idKey) {
            hasScannedRef.current = true;
            setStatusMessage('Family found — loading');
            onScan(idKey);
            return;
          }
        }
        // JSON but no id/phone — invalid
        setStatus('invalid');
        setErrorMessage('Invalid QR code — not recognized');
      } catch {
        // Not JSON — check if it's a plain phone number (digits only, 7-15 chars)
        if (/^\d{7,15}$/.test(detail)) {
          hasScannedRef.current = true;
          setStatusMessage('Family found — loading');
          onScan(detail);
          return;
        }

        // Not a valid format
        setStatus('invalid');
        setErrorMessage('Invalid QR code — not recognized');
      }
    };

    document.addEventListener('qr-detected', handleQrDetected);
    return () => {
      document.removeEventListener('qr-detected', handleQrDetected);
    };
  }, [onScan]);

  const isActive = status === 'active';

  return (
    <div
      data-testid="qr-scanner-modal"
      role="dialog"
      aria-label="QR Code Scanner"
      aria-modal="true"
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/70"
    >
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg mx-4 p-6 flex flex-col items-center">
        {/* Heading */}
        <h2 className="text-2xl font-bold text-gray-900 mb-4">Scan QR Code</h2>

        {/* Video / viewfinder area */}
        <div className="relative w-full aspect-square max-w-sm bg-gray-900 rounded-lg overflow-hidden mb-4">
          <video
            data-testid="qr-scanner-video"
            autoPlay
            muted
            playsInline
            className="w-full h-full object-cover"
          />
          {/* Viewfinder overlay */}
          <div
            data-testid="qr-scanner-overlay"
            className="absolute inset-0 flex items-center justify-center"
          >
            <div className="w-48 h-48 border-2 border-white/80 rounded-lg" />
          </div>
        </div>

        {/* Status / error messages */}
        <div role="status" aria-live="polite" className="mb-4 text-center min-h-[2rem]">
          {isActive && !cameraError && (
            <p className="text-gray-600">Scanning... Point camera at QR code</p>
          )}
          {isActive && cameraError && (
            <p className="text-red-600 font-medium" role="alert">
              Camera permission denied. Please allow camera access to scan QR codes.
            </p>
          )}
          {status === 'invalid' && (
            <p className="text-red-600 font-medium" role="alert">
              {errorMessage || 'Invalid QR code — not recognized'}
            </p>
          )}
          {status === 'not-found' && (
            <p className="text-red-600 font-medium" role="alert">
              Family not found. No family matches this QR code.
            </p>
          )}
          {status === 'timed-out' && (
            <p className="text-amber-600 font-medium" role="alert">
              Scanner timeout. No QR code detected — try again.
            </p>
          )}
          {statusMessage && isActive && (
            <p className="text-green-600 font-medium">{statusMessage}</p>
          )}
        </div>

        {/* Action buttons */}
        <div className="flex flex-col gap-3 w-full">
          {/* Torch toggle */}
          {isActive && (
            <Button
              data-testid="torch-toggle"
              onClick={() => setTorchOn(!torchOn)}
              variant="secondary"
              size="md"
              aria-label={torchOn ? 'Turn off flashlight' : 'Turn on flashlight'}
              className="w-full"
            >
              {torchOn ? 'Flashlight On' : 'Flashlight Off'}
            </Button>
          )}

          {/* Camera select */}
          {isActive && (
            <Button
              data-testid="camera-switcher"
              onClick={() => {
                /* noop in placeholder */
              }}
              variant="secondary"
              size="md"
              aria-label="Switch camera"
              className="w-full"
            >
              Switch Camera
            </Button>
          )}

          {/* Try Again — shown on invalid, not-found, or timed-out */}
          {(status === 'invalid' || status === 'not-found' || status === 'timed-out') && (
            <Button
              onClick={handleTryAgain}
              variant="primary"
              size="lg"
              className="w-full"
              aria-label="Try again"
            >
              Try Again
            </Button>
          )}

          {/* Manual entry */}
          <Button
            onClick={handleManualEntry}
            variant="secondary"
            size="md"
            className="w-full"
            aria-label="Enter manually"
          >
            Enter Manually
          </Button>

          {/* Cancel / close */}
          <Button
            onClick={handleClose}
            variant="secondary"
            size="md"
            className="w-full"
            aria-label="Cancel"
          >
            Cancel
          </Button>
        </div>
      </div>
    </div>
  );
}
