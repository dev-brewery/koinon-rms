/**
 * Client for communicating with the Koinon Print Bridge desktop application.
 * The print bridge runs locally on Windows and provides label printing via localhost:9632.
 */

const PRINT_BRIDGE_URL = 'http://localhost:9632';

export interface PrinterInfo {
  name: string;
  status: string;
  isDefault: boolean;
  isZebraPrinter: boolean;
  driverName: string;
  portName: string;
}

export interface PrintersResponse {
  printers: PrinterInfo[];
  count: number;
  zebraCount: number;
}

export interface PrintResponse {
  success: boolean;
  message: string;
  printerName: string;
  labelCount?: number;
}

export interface HealthResponse {
  status: string;
  version: string;
  timestamp: string;
}

export interface ErrorResponse {
  error: string;
  message: string;
  printerName?: string;
}

export type PrintBridgeResponse<T> =
  | { success: true; data: T }
  | { success: false; error: string };

/**
 * Print Bridge Client for label printing operations.
 */
export class PrintBridgeClient {
  private baseUrl: string;

  constructor(baseUrl: string = PRINT_BRIDGE_URL) {
    this.baseUrl = baseUrl;
  }

  /**
   * Checks if the print bridge is available and healthy.
   */
  async checkHealth(): Promise<PrintBridgeResponse<HealthResponse>> {
    try {
      const response = await fetch(`${this.baseUrl}/health`, {
        method: 'GET',
        signal: AbortSignal.timeout(3000), // 3 second timeout
      });

      if (!response.ok) {
        return { success: false, error: 'Print bridge returned error status' };
      }

      const data = await response.json() as HealthResponse;
      return { success: true, data };
    } catch (error) {
      if (error instanceof Error) {
        if (error.name === 'AbortError') {
          return { success: false, error: 'Print bridge connection timeout - is it running?' };
        }
        return { success: false, error: `Cannot connect to print bridge: ${error.message}` };
      }
      return { success: false, error: 'Cannot connect to print bridge' };
    }
  }

  /**
   * Gets all available printers from the print bridge.
   */
  async getPrinters(): Promise<PrintBridgeResponse<PrintersResponse>> {
    try {
      const response = await fetch(`${this.baseUrl}/api/printers`, {
        method: 'GET',
        signal: AbortSignal.timeout(10000), // 10 second timeout
      });

      if (!response.ok) {
        const errorData = await response.json() as ErrorResponse;
        return { success: false, error: errorData.message || 'Failed to get printers' };
      }

      const data = await response.json() as PrintersResponse;
      return { success: true, data };
    } catch (error) {
      if (error instanceof Error) {
        return { success: false, error: `Failed to get printers: ${error.message}` };
      }
      return { success: false, error: 'Failed to get printers' };
    }
  }

  /**
   * Refreshes the printer cache.
   */
  async refreshPrinters(): Promise<PrintBridgeResponse<{ message: string; count: number }>> {
    try {
      const response = await fetch(`${this.baseUrl}/api/printers/refresh`, {
        method: 'POST',
        signal: AbortSignal.timeout(10000), // 10 second timeout
      });

      if (!response.ok) {
        const errorData = await response.json() as ErrorResponse;
        return { success: false, error: errorData.message || 'Failed to refresh printers' };
      }

      const data = await response.json() as { message: string; count: number };
      return { success: true, data };
    } catch (error) {
      if (error instanceof Error) {
        return { success: false, error: `Failed to refresh printers: ${error.message}` };
      }
      return { success: false, error: 'Failed to refresh printers' };
    }
  }

  /**
   * Prints a test label to verify printer functionality.
   */
  async printTestLabel(printerName: string): Promise<PrintBridgeResponse<PrintResponse>> {
    try {
      const response = await fetch(`${this.baseUrl}/api/print/test`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ printerName }),
        signal: AbortSignal.timeout(10000), // 10 second timeout
      });

      if (!response.ok) {
        const errorData = await response.json() as ErrorResponse;
        return { success: false, error: errorData.message || 'Failed to print test label' };
      }

      const data = await response.json() as PrintResponse;
      return { success: true, data };
    } catch (error) {
      if (error instanceof Error) {
        return { success: false, error: `Failed to print test label: ${error.message}` };
      }
      return { success: false, error: 'Failed to print test label' };
    }
  }

  /**
   * Prints a single ZPL label.
   */
  async printLabel(printerName: string, zplContent: string): Promise<PrintBridgeResponse<PrintResponse>> {
    try {
      const response = await fetch(`${this.baseUrl}/api/print`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ printerName, zplContent }),
        signal: AbortSignal.timeout(10000), // 10 second timeout
      });

      if (!response.ok) {
        const errorData = await response.json() as ErrorResponse;
        return { success: false, error: errorData.message || 'Failed to print label' };
      }

      const data = await response.json() as PrintResponse;
      return { success: true, data };
    } catch (error) {
      if (error instanceof Error) {
        return { success: false, error: `Failed to print label: ${error.message}` };
      }
      return { success: false, error: 'Failed to print label' };
    }
  }

  /**
   * Prints multiple ZPL labels in batch.
   */
  async printBatch(printerName: string, zplContents: string[]): Promise<PrintBridgeResponse<PrintResponse>> {
    try {
      const response = await fetch(`${this.baseUrl}/api/print/batch`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ printerName, zplContents }),
        signal: AbortSignal.timeout(10000), // 10 second timeout
      });

      if (!response.ok) {
        const errorData = await response.json() as ErrorResponse;
        return { success: false, error: errorData.message || 'Failed to print labels' };
      }

      const data = await response.json() as PrintResponse;
      return { success: true, data };
    } catch (error) {
      if (error instanceof Error) {
        return { success: false, error: `Failed to print labels: ${error.message}` };
      }
      return { success: false, error: 'Failed to print labels' };
    }
  }

  /**
   * Gets the default Zebra printer, if available.
   */
  async getDefaultZebraPrinter(): Promise<PrintBridgeResponse<PrinterInfo | null>> {
    const printersResult = await this.getPrinters();

    if (!printersResult.success) {
      return { success: false, error: printersResult.error };
    }

    // Try to find default Zebra printer first
    const defaultZebra = printersResult.data.printers.find(
      p => p.isDefault && p.isZebraPrinter
    );

    if (defaultZebra) {
      return { success: true, data: defaultZebra };
    }

    // Otherwise, return first Zebra printer
    const firstZebra = printersResult.data.printers.find(p => p.isZebraPrinter);

    if (firstZebra) {
      return { success: true, data: firstZebra };
    }

    return { success: true, data: null };
  }
}

// Export singleton instance
export const printBridgeClient = new PrintBridgeClient();
