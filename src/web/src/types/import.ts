/**
 * CSV Import Type Definitions
 * For validation and progress tracking during import operations
 */

export type ValidationSeverity = 'error' | 'warning' | 'info';

export interface ValidationError {
  rowNumber: number;
  columnName: string;
  value: string;
  message: string;
  severity: ValidationSeverity;
}

export type ImportStatus = 'importing' | 'completed' | 'failed' | 'cancelled';

export interface ImportProgress {
  processedRows: number;
  totalRows: number;
  successCount: number;
  errorCount: number;
  elapsedSeconds: number;
  status: ImportStatus;
}
