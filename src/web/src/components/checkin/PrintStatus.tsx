import { useEffect, useRef, useState } from 'react';
import { type PrinterInfo } from '@/services/printing/PrintBridgeClient';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { usePrintBridgeConnection, type ConnectionStatus } from '@/hooks/usePrintBridgeConnection';

export interface PrintStatusProps {
  onPrinterAvailable?: (printer: PrinterInfo) => void;
  onPrinterUnavailable?: () => void;
}

interface TroubleshootingStep {
  title: string;
  steps: string[];
}

/**
 * Gets troubleshooting steps based on error type.
 */
function getTroubleshootingSteps(errorType: 'not-running' | 'no-printer' | 'timeout' | 'unknown'): TroubleshootingStep {
  switch (errorType) {
    case 'not-running':
      return {
        title: 'Print Bridge Not Running',
        steps: [
          'Start the Koinon Print Bridge application on this computer',
          'If not installed, download and install it from the link below',
          'Ensure the application is running before clicking "Test Connection"',
        ],
      };
    case 'no-printer':
      return {
        title: 'No Zebra Printer Found',
        steps: [
          'Connect a Zebra thermal printer to this computer',
          'Install the latest Zebra printer drivers from zebra.com',
          'Open Windows Settings > Devices > Printers & scanners',
          'Verify your Zebra printer appears in the list',
          'Set the Zebra printer as the default printer (recommended)',
          'Click "Test Connection" after setup',
        ],
      };
    case 'timeout':
      return {
        title: 'Connection Timeout',
        steps: [
          'Check if Print Bridge is running (look for the icon in system tray)',
          'Restart the Print Bridge application',
          'Check Windows Firewall settings - allow localhost:9632',
          'If the issue persists, restart this computer',
        ],
      };
    default:
      return {
        title: 'Connection Error',
        steps: [
          'Ensure Print Bridge is running',
          'Check that a Zebra printer is connected and installed',
          'Restart the Print Bridge application',
          'Contact support if the issue persists',
        ],
      };
  }
}

/**
 * Component that shows the current print bridge and printer status.
 * Checks if print bridge is running and if a Zebra printer is available.
 * Features automatic reconnection with exponential backoff.
 */
export function PrintStatus({ onPrinterAvailable, onPrinterUnavailable }: PrintStatusProps) {
  const { status, printer, error, reconnectIn, retry } = usePrintBridgeConnection();
  const [showTroubleshooting, setShowTroubleshooting] = useState(false);

  // Track previous status to avoid calling callbacks on every render
  const previousStatusRef = useRef<ConnectionStatus | null>(null);
  const previousPrinterRef = useRef<PrinterInfo | null>(null);

  // Notify parent of status changes - moved to useEffect to avoid side effects during render
  useEffect(() => {
    const statusChanged = previousStatusRef.current !== status;
    const printerChanged = previousPrinterRef.current?.name !== printer?.name;

    if (statusChanged || printerChanged) {
      if (status === 'available' && printer) {
        onPrinterAvailable?.(printer);
      } else {
        onPrinterUnavailable?.();
      }

      previousStatusRef.current = status;
      previousPrinterRef.current = printer;
    }
  }, [status, printer, onPrinterAvailable, onPrinterUnavailable]);

  if (status === 'checking') {
    return (
      <Card className="bg-blue-50 border-blue-200">
        <div className="flex items-center gap-3">
          <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600"></div>
          <p className="text-blue-800 text-sm">Checking print bridge status...</p>
        </div>
      </Card>
    );
  }

  if (status === 'unavailable' || status === 'no-printer') {
    const troubleshooting = error ? getTroubleshootingSteps(error.type) : null;

    return (
      <Card className="bg-yellow-50 border-yellow-200">
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <svg className="w-5 h-5 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
              <p className="text-yellow-800 text-sm font-medium">
                {status === 'no-printer' ? 'No thermal printer found' : 'Print bridge unavailable'}
              </p>
            </div>
            <Button onClick={retry} variant="secondary" size="sm">
              Test Connection
            </Button>
          </div>

          {error && (
            <p className="text-yellow-700 text-xs">{error.message}</p>
          )}

          {reconnectIn !== null && reconnectIn > 0 && (
            <div className="flex items-center gap-2">
              <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-yellow-600"></div>
              <p className="text-yellow-700 text-xs">
                Auto-retry in {reconnectIn} second{reconnectIn !== 1 ? 's' : ''}
              </p>
            </div>
          )}

          <p className="text-yellow-700 text-xs font-medium">
            Labels will not print automatically. Please resolve the issue below.
          </p>

          {/* Troubleshooting Section */}
          <div className="border-t border-yellow-300 pt-3">
            <button
              onClick={() => setShowTroubleshooting(!showTroubleshooting)}
              className="flex items-center justify-between w-full text-left"
            >
              <span className="text-yellow-800 text-sm font-medium">Troubleshooting</span>
              <svg
                className={`w-4 h-4 text-yellow-600 transition-transform ${showTroubleshooting ? 'rotate-180' : ''}`}
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
              </svg>
            </button>

            {showTroubleshooting && troubleshooting && (
              <div className="mt-3 space-y-3">
                <div>
                  <p className="text-yellow-800 text-sm font-medium mb-2">{troubleshooting.title}</p>
                  <ol className="list-decimal list-inside space-y-1">
                    {troubleshooting.steps.map((step, index) => (
                      <li key={index} className="text-yellow-700 text-xs">
                        {step}
                      </li>
                    ))}
                  </ol>
                </div>

                <div className="space-y-2">
                  <p className="text-yellow-800 text-xs font-medium">Download Print Bridge:</p>
                  <p className="text-yellow-700 text-xs">
                    Print Bridge installer download will be available soon. Contact your system administrator for installation assistance.
                  </p>
                </div>

                <Button onClick={retry} variant="primary" size="sm" className="w-full">
                  Test Connection
                </Button>
              </div>
            )}
          </div>
        </div>
      </Card>
    );
  }

  // status === 'available'
  return (
    <Card className="bg-green-50 border-green-200">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <svg className="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
          </svg>
          <div>
            <p className="text-green-800 text-sm font-medium">Printer ready</p>
            <p className="text-green-700 text-xs">{printer?.name}</p>
          </div>
        </div>
        <Button onClick={retry} variant="secondary" size="sm">
          Refresh
        </Button>
      </div>
    </Card>
  );
}
