/**
 * Import System TypeScript Types
 * Maps C# DTOs from Import domain
 */

import type { IdKey, DateTime } from '@/services/api/types';

// ============================================================================
// Legacy Types (preserved for backward compatibility)
// ============================================================================

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

// ============================================================================
// Enums
// ============================================================================

/**
 * Type of data being imported
 * Maps to ImportType enum in C#
 */
export enum ImportType {
  /** People import (individuals and families) */
  People = 1,
  /** Attendance records import */
  Attendance = 2,
  /** Giving/contribution records import */
  Giving = 3,
}

/**
 * Status of an import job execution
 * Maps to ImportJobStatus enum in C#
 */
export enum ImportJobStatus {
  /** Job is pending execution */
  Pending = 0,
  /** Job is currently processing */
  Processing = 1,
  /** Job completed successfully */
  Completed = 2,
  /** Job failed due to errors */
  Failed = 3,
  /** Job was cancelled by user or system */
  Cancelled = 4,
}

// ============================================================================
// Error Types
// ============================================================================

/**
 * Row-level error during import
 */
export interface ImportRowErrorDto {
  row: number;
  column: string;
  value: string;
  message: string;
}

/**
 * Validation error found in a CSV file row
 */
export interface CsvValidationErrorDto {
  rowNumber: number;
  columnName: string;
  value: string;
  errorMessage: string;
}

// ============================================================================
// Preview Types
// ============================================================================

/**
 * Preview of a CSV file including headers, sample data, and metadata
 */
export interface CsvPreviewDto {
  headers: string[];
  sampleRows: string[][];
  totalRowCount: number;
  detectedDelimiter: string;
  detectedEncoding: string;
}

// ============================================================================
// Template Types
// ============================================================================

/**
 * Column to field mapping configuration
 */
export interface FieldMappingDto {
  sourceColumn: string;
  targetField: string;
}

/**
 * Import template with saved field mappings
 */
export interface ImportTemplateDto {
  idKey: IdKey;
  guid: string;
  name: string;
  description?: string;
  importType: string;
  fieldMappings: Record<string, string>;
  isActive: boolean;
  isSystem: boolean;
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

// ============================================================================
// Job Types
// ============================================================================

/**
 * Import job execution status and progress
 */
export interface ImportJobDto {
  idKey: IdKey;
  guid: string;
  importTemplateIdKey?: string;
  importType: string;
  status: string;
  fileName: string;
  totalRows: number;
  processedRows: number;
  successCount: number;
  errorCount: number;
  errors?: ImportRowErrorDto[];
  startedAt?: DateTime;
  completedAt?: DateTime;
  createdDateTime: DateTime;
}

// ============================================================================
// Request Types
// ============================================================================

/**
 * Request to create a new import template
 */
export interface CreateImportTemplateRequest {
  name: string;
  description?: string;
  importType: string;
  fieldMappings: Record<string, string>;
}

/**
 * Request to validate import mappings before execution
 * Note: File handled separately via FormData upload
 */
export interface ValidateImportRequest {
  fileName: string;
  importType: string;
  fieldMappings: Record<string, string>;
}

/**
 * Request to start an import job
 * Note: File handled separately via FormData upload
 */
export interface StartImportRequest {
  fileName: string;
  importType: string;
  fieldMappings: Record<string, string>;
  importTemplateIdKey?: string;
}
