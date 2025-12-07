import React, { useEffect } from 'react';
import { QRCodeSVG } from 'qrcode.react';

export interface FamilyQrCardProps {
  familyName: string;
  familyIdKey: string;
  members: string[];
}

/**
 * Singleton manager for print styles injection
 * Ensures styles are only injected once across all component instances
 */
class PrintStylesManager {
  private static injected = false;
  private static readonly STYLE_ID = 'koinon-qr-card-print-styles';

  static inject(): void {
    if (this.injected || typeof document === 'undefined') {
      return;
    }

    if (document.getElementById(this.STYLE_ID)) {
      this.injected = true;
      return;
    }

    const style = document.createElement('style');
    style.id = this.STYLE_ID;
    style.textContent = `
      @media print {
        body * {
          visibility: hidden;
        }
        .print-card, .print-card * {
          visibility: visible;
        }
        .print-card {
          position: absolute;
          left: 0;
          top: 0;
          width: 6in;
          height: 4in;
          margin: 0;
          padding: 0.5in;
          page-break-after: always;
        }
        .no-print {
          display: none !important;
        }
      }
    `;
    document.head.appendChild(style);
    this.injected = true;
  }
}

/**
 * Printable QR code card for families
 * Format: 4x6 inch card with QR code and family info
 */
export function FamilyQrCard({ familyName, familyIdKey, members }: FamilyQrCardProps) {
  const qrCodeValue = `koinon://family/${familyIdKey}`;

  // Inject print styles once on mount
  useEffect(() => {
    PrintStylesManager.inject();
  }, []);

  return (
    <div className="print-card">
      {/* Card Content - 4x6 inch landscape */}
      <div
        className="bg-white border-4 border-blue-600 rounded-lg p-8 flex flex-col items-center justify-center"
        style={{
          width: '6in',
          height: '4in',
          boxSizing: 'border-box',
        }}
      >
        {/* Header */}
        <div className="text-center mb-6">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Koinon RMS</h1>
          <h2 className="text-xl font-semibold text-blue-600">Express Check-In</h2>
        </div>

        {/* QR Code */}
        <div className="mb-6 p-4 bg-white rounded-lg border-2 border-gray-300">
          <QRCodeSVG
            value={qrCodeValue}
            size={200}
            level="H"
            includeMargin={true}
          />
        </div>

        {/* Family Info */}
        <div className="text-center">
          <h3 className="text-2xl font-bold text-gray-900 mb-2">{familyName}</h3>
          {members.length > 0 && (
            <div className="flex flex-wrap justify-center gap-2">
              {members.slice(0, 6).map((member, index) => (
                <span
                  key={`${member}-${index}`}
                  className="text-sm text-gray-600 bg-gray-100 px-3 py-1 rounded-full"
                >
                  {member}
                </span>
              ))}
              {members.length > 6 && (
                <span className="text-sm text-gray-600 bg-gray-100 px-3 py-1 rounded-full">
                  +{members.length - 6} more
                </span>
              )}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="mt-auto text-center text-xs text-gray-500">
          <p>Scan this code at the check-in kiosk for fast entry</p>
        </div>
      </div>
    </div>
  );
}

/**
 * Button component to trigger print of QR card
 */
export interface PrintQrCardButtonProps {
  familyName: string;
  familyIdKey: string;
  members: string[];
  className?: string;
}

export function PrintQrCardButton({
  familyName,
  familyIdKey,
  members,
  className = '',
}: PrintQrCardButtonProps) {
  const [showCard, setShowCard] = React.useState(false);

  const handlePrint = () => {
    setShowCard(true);
    // Wait for card to render before printing using requestAnimationFrame
    // Double RAF ensures render is complete before printing
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        window.print();
        setShowCard(false);
      });
    });
  };

  return (
    <>
      <button
        onClick={handlePrint}
        className={`px-6 py-3 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700 transition-colors ${className}`}
      >
        Print QR Code Card
      </button>

      {/* Hidden card for printing */}
      {showCard && (
        <div className="fixed top-0 left-0 w-screen h-screen bg-white z-50">
          <FamilyQrCard
            familyName={familyName}
            familyIdKey={familyIdKey}
            members={members}
          />
        </div>
      )}
    </>
  );
}
