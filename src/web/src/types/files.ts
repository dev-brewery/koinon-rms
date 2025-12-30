/**
 * File Upload/Download TypeScript Types
 * Maps C# DTOs from Files domain
 */

import type { IdKey, DateTime, DefinedValueDto } from '@/services/api/types';

// ============================================================================
// File Metadata Types
// ============================================================================

/**
 * File metadata response
 * Maps to FileMetadataDto in C#
 */
export interface FileMetadataDto {
  /** Encoded ID for use in URLs */
  idKey: IdKey;
  /** Original filename */
  fileName: string;
  /** MIME type/content type */
  mimeType: string;
  /** File size in bytes */
  fileSizeBytes: number;
  /** Image width in pixels (null for non-images) */
  width?: number;
  /** Image height in pixels (null for non-images) */
  height?: number;
  /** File type category (DefinedValue) */
  binaryFileType?: DefinedValueDto;
  /** Optional description or alt text */
  description?: string;
  /** When the file was uploaded */
  createdDateTime: DateTime;
  /** URL to download/view the file */
  url: string;
}

// ============================================================================
// Request Types
// ============================================================================

/**
 * Request for file upload
 * Note: Stream handled separately via FormData upload in frontend
 * Maps to UploadFileRequest in C# (without Stream property)
 */
export interface UploadFileRequest {
  /** Original filename */
  fileName: string;
  /** MIME type/content type */
  contentType: string;
  /** File size in bytes */
  length: number;
  /** Optional description or alt text for the file */
  description?: string;
  /** Optional file type category IdKey (from DefinedValue) */
  binaryFileTypeIdKey?: string;
}
