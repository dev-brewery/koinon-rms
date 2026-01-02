/**
 * Files API service
 */

import { get, del, apiClient, getAccessToken } from './client';
import type { FileMetadataDto, UploadFileOptions } from '@/types/files';

const API_BASE_URL =
  import.meta.env.VITE_API_URL || 'http://localhost:5000/api/v1';

// Re-export for backward compatibility
export type { UploadFileOptions };

/**
 * Uploads a file
 * Requires authentication
 */
export async function uploadFile(
  file: File,
  options?: UploadFileOptions
): Promise<FileMetadataDto> {
  const formData = new FormData();
  formData.append('file', file);

  if (options?.description) {
    formData.append('description', options.description);
  }
  if (options?.binaryFileTypeIdKey) {
    formData.append('binaryFileTypeIdKey', options.binaryFileTypeIdKey);
  }

  // Use apiClient directly - don't set Content-Type, let browser set it with boundary
  const response = await apiClient<{ data: FileMetadataDto }>('/files', {
    method: 'POST',
    body: formData,
  });
  return response.data;
}

/**
 * Gets file metadata by IdKey
 * Requires authentication
 */
export async function getFileMetadata(idKey: string): Promise<FileMetadataDto> {
  const response = await get<{ data: FileMetadataDto }>(
    `/files/${idKey}/metadata`
  );
  return response.data;
}

/**
 * Downloads a file by IdKey
 * Returns a Blob for the file content
 * Requires authentication
 */
export async function downloadFile(idKey: string): Promise<Blob> {
  // Use fetch directly for blob response since apiClient expects JSON
  const headers: HeadersInit = {};
  const token = getAccessToken();
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE_URL}/files/${idKey}`, {
    method: 'GET',
    headers,
  });

  if (!response.ok) {
    throw new Error(`Failed to download file: ${response.statusText}`);
  }

  return response.blob();
}

/**
 * Deletes a file by IdKey
 * Requires authentication
 */
export async function deleteFile(idKey: string): Promise<void> {
  await del<void>(`/files/${idKey}`);
}
