/**
 * Printing service exports
 */

export { PrintBridgeClient, PrintBridgeError, getPrintBridgeClient, resetPrintBridgeClient } from './PrintBridgeClient';
export type {
  PrinterInfo,
  PrintRequest,
  PrintResult,
  TestPrintRequest,
  PrintersResponse,
  HealthResponse,
  PrintBridgeConfig,
} from './types';
