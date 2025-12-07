/**
 * Types for PrintBridge client communication
 * PrintBridge is a local Windows application that handles printing to thermal printers
 * Communication is via HTTP to localhost:9632
 */

/**
 * Information about a detected printer
 */
export interface PrinterInfo {
  name: string;
  type: 'ZPL' | 'EPL' | 'Unknown';
  status: 'Ready' | 'Paused' | 'Error' | 'PaperJam' | 'PaperOut' | 'ManualFeed' | 'Unknown';
  isDefault: boolean;
}

/**
 * Request to print ZPL content to a specific printer
 */
export interface PrintRequest {
  printerName?: string;  // Optional, uses default if not provided
  zplContent: string;    // ZPL command string
  copies?: number;       // Optional, defaults to 1
}

/**
 * Response from a print operation
 */
export interface PrintResult {
  success: boolean;
  message: string;
  printerName?: string;
}

/**
 * Request to print a test label
 */
export interface TestPrintRequest {
  printerName?: string;
}

/**
 * Response containing list of available printers
 */
export interface PrintersResponse {
  printers: PrinterInfo[];
}

/**
 * Health status of PrintBridge
 */
export interface HealthResponse {
  status: 'healthy' | 'unhealthy';
  version: string;
  defaultPrinter?: string;
  timestamp: string;  // ISO 8601 datetime
}

/**
 * Configuration for PrintBridge discovery and connection
 */
export interface PrintBridgeConfig {
  baseUrl: string;  // e.g., 'http://127.0.0.1:9632'
  timeout: number;  // milliseconds
}
