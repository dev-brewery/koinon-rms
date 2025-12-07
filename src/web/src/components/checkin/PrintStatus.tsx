import { useEffect, useState } from 'react';
import { printBridgeClient, type PrinterInfo } from '@/services/printing/PrintBridgeClient';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';

export interface PrintStatusProps {
  onPrinterAvailable?: (printer: PrinterInfo) => void;
  onPrinterUnavailable?: () => void;
}

/**
 * Component that shows the current print bridge and printer status.
 * Checks if print bridge is running and if a Zebra printer is available.
 */
export function PrintStatus({ onPrinterAvailable, onPrinterUnavailable }: PrintStatusProps) {
  const [status, setStatus] = useState<'checking' | 'available' | 'unavailable' | 'no-printer'>('checking');
  const [printer, setPrinter] = useState<PrinterInfo | null>(null);
  const [error, setError] = useState<string | null>(null);

  const checkPrintBridge = async () => {
    setStatus('checking');
    setError(null);

    // Check health first
    const healthResult = await printBridgeClient.checkHealth();

    if (!healthResult.success) {
      setStatus('unavailable');
      setError(healthResult.error);
      onPrinterUnavailable?.();
      return;
    }

    // Get default Zebra printer
    const printerResult = await printBridgeClient.getDefaultZebraPrinter();

    if (!printerResult.success) {
      setStatus('unavailable');
      setError(printerResult.error);
      onPrinterUnavailable?.();
      return;
    }

    if (printerResult.data === null) {
      setStatus('no-printer');
      setError('No Zebra thermal printer found. Please install a Zebra printer.');
      onPrinterUnavailable?.();
      return;
    }

    setStatus('available');
    setPrinter(printerResult.data);
    onPrinterAvailable?.(printerResult.data);
  };

  useEffect(() => {
    checkPrintBridge();

    // Refresh status every 30 seconds
    const interval = setInterval(checkPrintBridge, 30000);

    return () => clearInterval(interval);
  }, []);

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

  if (status === 'unavailable') {
    return (
      <Card className="bg-yellow-50 border-yellow-200">
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <svg className="w-5 h-5 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
              <p className="text-yellow-800 text-sm font-medium">Print bridge unavailable</p>
            </div>
            <Button onClick={checkPrintBridge} variant="secondary" size="sm">
              Retry
            </Button>
          </div>
          <p className="text-yellow-700 text-xs">{error}</p>
          <p className="text-yellow-700 text-xs">
            Labels will not print automatically. Please start the Koinon Print Bridge application.
          </p>
        </div>
      </Card>
    );
  }

  if (status === 'no-printer') {
    return (
      <Card className="bg-yellow-50 border-yellow-200">
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <svg className="w-5 h-5 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
              <p className="text-yellow-800 text-sm font-medium">No thermal printer found</p>
            </div>
            <Button onClick={checkPrintBridge} variant="secondary" size="sm">
              Retry
            </Button>
          </div>
          <p className="text-yellow-700 text-xs">{error}</p>
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
        <Button onClick={checkPrintBridge} variant="secondary" size="sm">
          Refresh
        </Button>
      </div>
    </Card>
  );
}
