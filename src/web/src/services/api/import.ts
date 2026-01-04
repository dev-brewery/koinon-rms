/**
 * Import API service
 */

import { get, post, del, apiClient, getAccessToken } from './client';
import type {
  CsvPreviewDto,
  ImportTemplateDto,
  CreateImportTemplateRequest,
  ImportJobDto,
} from '@/types/import';

const API_BASE_URL =
  import.meta.env.VITE_API_URL || 'http://localhost:5000/api/v1';

// ============================================================================
// CSV Preview
// ============================================================================

/**
 * Upload a CSV file and generate a preview with headers and sample rows.
 * Requires authentication
 */
export async function uploadCsvPreview(file: File): Promise<CsvPreviewDto> {
  const formData = new FormData();
  formData.append('file', file);

  // Use apiClient directly - don't set Content-Type, let browser set it with boundary
  const response = await apiClient<{ data: CsvPreviewDto }>('/import/upload', {
    method: 'POST',
    body: formData,
  });
  return response.data;
}

// ============================================================================
// Templates
// ============================================================================

/**
 * Gets all import templates, optionally filtered by type.
 * Requires authentication
 */
export async function getImportTemplates(
  type?: string
): Promise<ImportTemplateDto[]> {
  const params = new URLSearchParams();
  if (type) {
    params.set('type', type);
  }

  const queryString = params.toString();
  const url = `/import/templates${queryString ? `?${queryString}` : ''}`;

  const response = await get<{ data: ImportTemplateDto[] }>(url);
  return response.data;
}

/**
 * Gets an import template by IdKey.
 * Requires authentication
 */
export async function getImportTemplate(
  idKey: string
): Promise<ImportTemplateDto> {
  const response = await get<{ data: ImportTemplateDto }>(
    `/import/templates/${idKey}`
  );
  return response.data;
}

/**
 * Creates a new import template.
 * Requires admin authentication
 */
export async function createImportTemplate(
  request: CreateImportTemplateRequest
): Promise<ImportTemplateDto> {
  // POST returns 201 Created with body in data wrapper
  const response = await post<{ data: ImportTemplateDto }>(
    '/import/templates',
    request
  );
  return response.data;
}

/**
 * Deletes an import template.
 * Requires admin authentication
 */
export async function deleteImportTemplate(idKey: string): Promise<void> {
  await del<void>(`/import/templates/${idKey}`);
}

// ============================================================================
// Validation & Execution
// ============================================================================

/**
 * Validates field mappings against a CSV file before import.
 * Requires authentication
 */
export async function validateImport(
  file: File,
  importType: string,
  fieldMappings: Record<string, string>
): Promise<ImportJobDto> {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('importType', importType);
  formData.append('fieldMappingsJson', JSON.stringify(fieldMappings));

  const response = await apiClient<{ data: ImportJobDto }>('/import/validate', {
    method: 'POST',
    body: formData,
  });
  return response.data;
}

/**
 * Starts an import job to process CSV data.
 * Requires admin authentication
 */
export async function executeImport(
  file: File,
  importType: string,
  fieldMappings: Record<string, string>,
  templateIdKey?: string
): Promise<ImportJobDto> {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('importType', importType);
  formData.append('fieldMappingsJson', JSON.stringify(fieldMappings));
  if (templateIdKey) {
    formData.append('templateIdKey', templateIdKey);
  }

  const response = await apiClient<{ data: ImportJobDto }>('/import/execute', {
    method: 'POST',
    body: formData,
  });
  return response.data;
}

// ============================================================================
// Job Status
// ============================================================================

/**
 * Gets all import jobs, optionally filtered by type.
 * Requires authentication
 */
export async function getImportJobs(
  type?: string
): Promise<ImportJobDto[]> {
  const params = new URLSearchParams();
  if (type) {
    params.set('type', type);
  }

  const queryString = params.toString();
  const url = `/import/jobs${queryString ? `?${queryString}` : ''}`;

  const response = await get<{ data: ImportJobDto[] }>(url);
  return response.data;
}

/**
 * Gets the status and progress of an import job.
 * Requires authentication
 */
export async function getImportJobStatus(idKey: string): Promise<ImportJobDto> {
  const response = await get<{ data: ImportJobDto }>(`/import/jobs/${idKey}`);
  return response.data;
}

/**
 * Downloads CSV error report for a completed import job.
 * Returns a Blob for the CSV content.
 * Requires authentication
 */
export async function downloadImportErrors(idKey: string): Promise<Blob> {
  // Use fetch directly for blob response since apiClient expects JSON
  const headers: HeadersInit = {};
  const token = getAccessToken();
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE_URL}/import/jobs/${idKey}/errors`, {
    method: 'GET',
    headers,
  });

  if (!response.ok) {
    throw new Error(`Failed to download error report: ${response.statusText}`);
  }

  return response.blob();
}
