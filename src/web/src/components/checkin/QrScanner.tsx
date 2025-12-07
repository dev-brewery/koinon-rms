import { useEffect, useRef, useCallback } from 'react';
import { useQrScanner } from '@/hooks/useQrScanner';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';

export interface QrScannerProps {
  onScan: (familyIdKey: string) => void;
  onError?: (error: string) => void;
  onCancel: () => void;
}

/**
 * Full-screen QR code scanner component
 * Uses device camera to scan Koinon family QR codes
 */
export function QrScanner({ onScan, onError, onCancel }: QrScannerProps) {
  const scannerElementId = 'qr-scanner-reader';
  const hasStartedRef = useRef(false);

  const { isScanning, isCameraReady, error, startScanning, stopScanning } = useQrScanner({
    onScan: (familyIdKey) => {
      // Stop scanning after successful scan
      stopScanning().then(() => {
        onScan(familyIdKey);
      });
    },
    onError,
  });

  // Wrap cancel handler in useCallback to stabilize reference
  const handleCancel = useCallback(() => {
    stopScanning().then(() => {
      onCancel();
    });
  }, [stopScanning, onCancel]);

  // Start scanning when component mounts
  useEffect(() => {
    if (!hasStartedRef.current) {
      hasStartedRef.current = true;
      startScanning(scannerElementId);
    }

    return () => {
      stopScanning();
    };
  }, [startScanning, stopScanning]);

  return (
    <div className="fixed inset-0 bg-black z-50 flex flex-col">
      {/* Header */}
      <div className="bg-gray-900 text-white p-4 flex justify-between items-center">
        <div>
          <h2 className="text-2xl font-bold">Scan QR Code</h2>
          <p className="text-sm text-gray-400">
            Point camera at family QR code
          </p>
        </div>
        <Button
          onClick={handleCancel}
          variant="outline"
          size="lg"
          className="min-h-[48px] min-w-[48px] bg-white text-gray-900 border-white hover:bg-gray-100"
        >
          Cancel
        </Button>
      </div>

      {/* Scanner Area */}
      <div className="flex-1 relative flex items-center justify-center bg-black">
        {/* QR Scanner Element */}
        <div
          id={scannerElementId}
          className="w-full h-full"
          style={{ maxWidth: '100%', maxHeight: '100%' }}
        />

        {/* Loading Indicator */}
        {isScanning && !isCameraReady && (
          <div className="absolute inset-0 flex items-center justify-center bg-black bg-opacity-75">
            <div className="text-center">
              <div className="inline-block animate-spin rounded-full h-16 w-16 border-b-4 border-white mb-4"></div>
              <p className="text-white text-xl">Starting camera...</p>
            </div>
          </div>
        )}

        {/* Error Display */}
        {error && (
          <div className="absolute bottom-0 left-0 right-0 p-4">
            <Card className="bg-red-50 border-2 border-red-200">
              <div className="p-4">
                <p className="text-red-800 text-lg font-semibold text-center">
                  {error}
                </p>
              </div>
            </Card>
          </div>
        )}

        {/* Instructions */}
        {isCameraReady && !error && (
          <div className="absolute bottom-0 left-0 right-0 p-4">
            <Card className="bg-white bg-opacity-90">
              <div className="p-4">
                <p className="text-gray-900 text-lg font-medium text-center">
                  Position QR code within the frame
                </p>
              </div>
            </Card>
          </div>
        )}
      </div>

      {/* Alternative Input Note */}
      <div className="bg-gray-900 text-white p-4 text-center">
        <p className="text-sm text-gray-400">
          You can also use a barcode scanner to scan family codes
        </p>
      </div>
    </div>
  );
}
