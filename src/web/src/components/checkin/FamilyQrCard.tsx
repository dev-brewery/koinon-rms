import { QRCodeSVG } from 'qrcode.react';
import { Card } from '@/components/ui';

export interface FamilyQrCardProps {
  familyIdKey: string;
  familyName: string;
  qrSize?: number;
  showPrintButton?: boolean;
}

/**
 * Printable QR code card for family check-in
 * Encodes koinon://family/{idKey} for fast scanning
 */
export function FamilyQrCard({
  familyIdKey,
  familyName,
  qrSize = 200,
  showPrintButton = true,
}: FamilyQrCardProps) {
  const qrValue = `koinon://family/${familyIdKey}`;

  const handlePrint = () => {
    window.print();
  };

  return (
    <div className="max-w-md mx-auto family-qr-card-container">
      <Card className="p-8 text-center print:shadow-none">
        {/* Header */}
        <div className="mb-6 print:mb-4">
          <h2 className="text-2xl font-bold text-gray-900 mb-1">
            {familyName}
          </h2>
          <p className="text-gray-600">Check-In QR Code</p>
        </div>

        {/* QR Code */}
        <div className="flex justify-center mb-6 print:mb-4">
          <div className="bg-white p-4 rounded-lg border-2 border-gray-200 inline-block">
            <QRCodeSVG
              value={qrValue}
              size={qrSize}
              level="H" // High error correction
              includeMargin={true}
            />
          </div>
        </div>

        {/* Instructions */}
        <div className="mb-6 print:mb-4 space-y-2 text-sm text-gray-700">
          <p className="font-medium">How to Use:</p>
          <ol className="text-left list-decimal list-inside space-y-1">
            <li>Visit the check-in kiosk</li>
            <li>Select "Scan QR Code"</li>
            <li>Show this code to the camera or scanner</li>
          </ol>
        </div>

        {/* Footer Info */}
        <div className="text-xs text-gray-500 mb-4 print:mb-2">
          <p>Family ID: {familyIdKey}</p>
          <p className="mt-1">Keep this card for fast check-in</p>
        </div>

        {/* Print Button (hidden when printing) */}
        {showPrintButton && (
          <button
            onClick={handlePrint}
            className="bg-blue-600 hover:bg-blue-700 text-white font-semibold px-6 py-3 rounded-lg transition-colors print:hidden"
          >
            Print QR Code
          </button>
        )}
      </Card>

      {/* Scoped Print Styles - only affects this component */}
      <style>{`
        @media print {
          body *:not(.family-qr-card-container):not(.family-qr-card-container *) {
            visibility: hidden;
          }
          .family-qr-card-container {
            position: absolute;
            left: 50%;
            top: 50%;
            transform: translate(-50%, -50%);
          }
        }
      `}</style>
    </div>
  );
}
