import { useEffect } from 'react';
import { Button, Card } from '@/components/ui';
import { useQrScanner } from '@/hooks/useQrScanner';

export interface QrScannerProps {
  onScan: (familyIdKey: string) => void;
  onCancel?: () => void;
}

/**
 * QR code scanner for fast family check-in
 * Supports both camera scanning and keyboard wedge barcode scanners
 */
export function QrScanner({ onScan, onCancel }: QrScannerProps) {
  // useQrScanner now handles validation and returns IdKey directly
  const { isScanning, startScanning, stopScanning, error } = useQrScanner({
    onScan,
    fps: 10,
    qrbox: { width: 250, height: 250 },
  });

  // Auto-start scanning on mount
  useEffect(() => {
    startScanning();

    return () => {
      stopScanning();
    };
  }, [startScanning, stopScanning]);

  const handleCancel = () => {
    stopScanning();
    onCancel?.();
  };

  return (
    <div className="max-w-2xl mx-auto">
      <Card className="p-8">
        {/* Title */}
        <h2 className="text-3xl font-bold text-center mb-2 text-gray-900">
          Scan QR Code
        </h2>
        <p className="text-center text-gray-600 mb-8">
          Scan your family QR code or use a barcode scanner
        </p>

        {/* Camera View */}
        <div className="mb-6">
          <div
            id="qr-reader"
            className="rounded-lg overflow-hidden bg-gray-900 mx-auto"
            style={{ maxWidth: '500px' }}
          />
        </div>

        {/* Error Display */}
        {error && (
          <div className="mb-6">
            <Card className="bg-red-50 border border-red-200">
              <p className="text-red-800 text-center font-medium">{error}</p>
            </Card>
          </div>
        )}

        {/* Instructions */}
        <div className="mb-6 space-y-3">
          <div className="flex items-start gap-3">
            <div className="flex-shrink-0 w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center text-blue-600 font-bold">
              1
            </div>
            <div>
              <p className="text-gray-900 font-medium">Position QR Code</p>
              <p className="text-sm text-gray-600">
                Hold your QR code in front of the camera
              </p>
            </div>
          </div>

          <div className="flex items-start gap-3">
            <div className="flex-shrink-0 w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center text-blue-600 font-bold">
              2
            </div>
            <div>
              <p className="text-gray-900 font-medium">Or Use Barcode Scanner</p>
              <p className="text-sm text-gray-600">
                Scan your QR code with the handheld scanner
              </p>
            </div>
          </div>

          <div className="flex items-start gap-3">
            <div className="flex-shrink-0 w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center text-blue-600 font-bold">
              3
            </div>
            <div>
              <p className="text-gray-900 font-medium">Automatic Check-in</p>
              <p className="text-sm text-gray-600">
                Your family will be loaded automatically
              </p>
            </div>
          </div>
        </div>

        {/* Status Indicator */}
        <div className="mb-6 text-center">
          {isScanning ? (
            <div className="inline-flex items-center gap-2 bg-green-100 text-green-800 px-4 py-2 rounded-full">
              <div className="w-2 h-2 bg-green-600 rounded-full animate-pulse" />
              <span className="font-medium">Scanner Active</span>
            </div>
          ) : (
            <div className="inline-flex items-center gap-2 bg-gray-100 text-gray-600 px-4 py-2 rounded-full">
              <div className="w-2 h-2 bg-gray-400 rounded-full" />
              <span className="font-medium">Scanner Inactive</span>
            </div>
          )}
        </div>

        {/* Cancel Button */}
        {onCancel && (
          <Button
            onClick={handleCancel}
            variant="secondary"
            size="lg"
            className="w-full text-xl"
          >
            Cancel
          </Button>
        )}

        {/* Help Text */}
        <p className="text-center text-sm text-gray-500 mt-4">
          Don't have a QR code?{' '}
          <button
            onClick={handleCancel}
            className="text-blue-600 hover:text-blue-700 font-medium"
          >
            Search by phone or name instead
          </button>
        </p>
      </Card>
    </div>
  );
}
