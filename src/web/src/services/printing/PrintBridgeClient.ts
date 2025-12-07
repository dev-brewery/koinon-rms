/**
 * PrintBridge Client
 *
 * Communicates with the local PrintBridge Windows application to send print jobs
 * to Zebra thermal printers.
 *
 * Architecture:
 * - PrintBridge runs on localhost:9632 (Windows only)
 * - Uses HTTP POST to send ZPL commands
 * - Handles offline scenarios gracefully
 * - No authentication (trusted local-only communication)
 */

import type {
  PrinterInfo,
  PrintRequest,
  PrintResult,
  TestPrintRequest,
  PrintersResponse,
  HealthResponse,
  PrintBridgeConfig,
} from './types';

/**
 * Default configuration for PrintBridge
 */
const DEFAULT_CONFIG: PrintBridgeConfig = {
  baseUrl: 'http://127.0.0.1:9632',
  timeout: 5000,
};

/**
 * PrintBridge client for communicating with the local print service
 */
export class PrintBridgeClient {
  private config: PrintBridgeConfig;
  private availableCache: boolean | null = null;
  private lastHealthCheckTime: number = 0;
  private healthCheckCacheDuration = 5000; // Cache for 5 seconds

  constructor(config: Partial<PrintBridgeConfig> = {}) {
    this.config = { ...DEFAULT_CONFIG, ...config };
  }

  /**
   * Check if PrintBridge is available (healthy)
   * Uses caching to avoid excessive health checks
   */
  async isAvailable(): Promise<boolean> {
    const now = Date.now();

    // Return cached result if fresh
    if (
      this.availableCache !== null &&
      now - this.lastHealthCheckTime < this.healthCheckCacheDuration
    ) {
      return this.availableCache;
    }

    try {
      await this.health();
      this.availableCache = true;
      this.lastHealthCheckTime = now;
      return true;
    } catch {
      this.availableCache = false;
      this.lastHealthCheckTime = now;
      return false;
    }
  }

  /**
   * Invalidate the availability cache (call after print failures)
   */
  invalidateCache(): void {
    this.availableCache = null;
    this.lastHealthCheckTime = 0;
  }

  /**
   * Get health status of PrintBridge
   * GET /api/v1/print/health
   */
  async health(): Promise<HealthResponse> {
    const url = `${this.config.baseUrl}/api/v1/print/health`;

    const response = await this.fetchWithTimeout(url, {
      method: 'GET',
    });

    if (!response.ok) {
      throw new PrintBridgeError(
        `Health check failed: ${response.status}`,
        'HEALTH_CHECK_FAILED'
      );
    }

    return response.json();
  }

  /**
   * Get list of available printers
   * GET /api/v1/print/printers
   */
  async getPrinters(): Promise<PrinterInfo[]> {
    const available = await this.isAvailable();
    if (!available) {
      throw new PrintBridgeError(
        'PrintBridge is not available',
        'NOT_AVAILABLE'
      );
    }

    const url = `${this.config.baseUrl}/api/v1/print/printers`;

    const response = await this.fetchWithTimeout(url, {
      method: 'GET',
    });

    if (!response.ok) {
      throw new PrintBridgeError(
        `Failed to get printers: ${response.status}`,
        'GET_PRINTERS_FAILED'
      );
    }

    const data = await response.json() as PrintersResponse;
    return data.printers;
  }

  /**
   * Send ZPL content to a printer
   * POST /api/v1/print/print
   *
   * @param request - Print request with ZPL content and optional printer name
   * @returns Result of the print operation
   */
  async print(request: PrintRequest): Promise<PrintResult> {
    // Validate input
    if (!request.zplContent || request.zplContent.trim().length === 0) {
      throw new PrintBridgeError('ZPL content cannot be empty', 'INVALID_ZPL');
    }

    if (request.copies !== undefined && (request.copies < 1 || request.copies > 999)) {
      throw new PrintBridgeError('Copies must be between 1 and 999', 'INVALID_COPIES');
    }

    // Check availability
    const available = await this.isAvailable();
    if (!available) {
      throw new PrintBridgeError(
        'PrintBridge is not available. Ensure the application is running on this kiosk.',
        'NOT_AVAILABLE'
      );
    }

    const url = `${this.config.baseUrl}/api/v1/print/print`;

    try {
      const response = await this.fetchWithTimeout(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          printerName: request.printerName,
          zplContent: request.zplContent,
          copies: request.copies ?? 1,
        }),
      });

      if (!response.ok) {
        const errorData = await this.safeParse<PrintResult>(response);
        throw new PrintBridgeError(
          errorData?.message || `Print failed with status ${response.status}`,
          'PRINT_FAILED'
        );
      }

      const result = await response.json() as PrintResult;
      return result;
    } catch (error) {
      if (error instanceof PrintBridgeError) {
        throw error;
      }

      // Invalidate cache on network errors
      this.invalidateCache();

      throw new PrintBridgeError(
        `Network error: ${error instanceof Error ? error.message : 'Unknown error'}`,
        'NETWORK_ERROR'
      );
    }
  }

  /**
   * Print a test label to verify printer connectivity
   * POST /api/v1/print/test
   *
   * @param printerName - Optional specific printer to test
   * @returns Result of the test print operation
   */
  async testPrint(printerName?: string): Promise<PrintResult> {
    const available = await this.isAvailable();
    if (!available) {
      throw new PrintBridgeError(
        'PrintBridge is not available',
        'NOT_AVAILABLE'
      );
    }

    const url = `${this.config.baseUrl}/api/v1/print/test`;

    const request: TestPrintRequest = { printerName };

    const response = await this.fetchWithTimeout(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const errorData = await this.safeParse<PrintResult>(response);
      throw new PrintBridgeError(
        errorData?.message || `Test print failed with status ${response.status}`,
        'TEST_PRINT_FAILED'
      );
    }

    return response.json();
  }

  /**
   * Safely parse JSON response (handles invalid JSON)
   */
  private async safeParse<T>(response: Response): Promise<T | null> {
    try {
      const contentType = response.headers.get('content-type');
      if (contentType?.includes('application/json')) {
        return (await response.json()) as T;
      }
    } catch {
      // Ignore parsing errors
    }
    return null;
  }

  /**
   * Fetch with timeout
   */
  private fetchWithTimeout(url: string, init: RequestInit): Promise<Response> {
    return Promise.race<Response>([
      fetch(url, init),
      new Promise<Response>((_, reject) =>
        setTimeout(
          () =>
            reject(
              new PrintBridgeError(
                `Request timeout after ${this.config.timeout}ms`,
                'TIMEOUT'
              )
            ),
          this.config.timeout
        )
      ),
    ]);
  }
}

/**
 * Custom error class for PrintBridge errors
 */
export class PrintBridgeError extends Error {
  constructor(
    message: string,
    public readonly code: string
  ) {
    super(message);
    this.name = 'PrintBridgeError';
  }
}

/**
 * Singleton instance of PrintBridge client
 */
let clientInstance: PrintBridgeClient | null = null;

/**
 * Get or create the PrintBridge client singleton
 */
export function getPrintBridgeClient(
  config?: Partial<PrintBridgeConfig>
): PrintBridgeClient {
  if (!clientInstance) {
    clientInstance = new PrintBridgeClient(config);
  }
  return clientInstance;
}

/**
 * Reset the PrintBridge client (useful for testing)
 */
export function resetPrintBridgeClient(): void {
  clientInstance = null;
}
