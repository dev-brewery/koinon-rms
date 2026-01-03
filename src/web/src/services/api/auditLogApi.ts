/**
 * Audit Log API service
 */

import { get, getAccessToken } from './client';
import type {
  PagedResult,
  AuditLogDto,
  AuditLogSearchParams,
  AuditLogExportParams,
} from './types';

const API_BASE_URL =
  import.meta.env.VITE_API_URL || 'http://localhost:5000/api/v1';

/**
 * Search audit logs with filters and pagination
 */
export async function searchAuditLogs(
  params: AuditLogSearchParams = {}
): Promise<PagedResult<AuditLogDto>> {
  const queryParams = new URLSearchParams();

  if (params.startDate) queryParams.set('startDate', params.startDate);
  if (params.endDate) queryParams.set('endDate', params.endDate);
  if (params.entityType) queryParams.set('entityType', params.entityType);
  if (params.actionType) queryParams.set('actionType', params.actionType);
  if (params.personIdKey) queryParams.set('personIdKey', params.personIdKey);
  if (params.entityIdKey) queryParams.set('entityIdKey', params.entityIdKey);
  if (params.page) queryParams.set('page', String(params.page));
  if (params.pageSize) queryParams.set('pageSize', String(params.pageSize));

  const query = queryParams.toString();
  const endpoint = `/audit-logs${query ? `?${query}` : ''}`;

  return get<PagedResult<AuditLogDto>>(endpoint);
}

/**
 * Get audit history for a specific entity
 */
export async function getEntityAuditHistory(
  entityType: string,
  idKey: string
): Promise<AuditLogDto[]> {
  return get<AuditLogDto[]>(`/audit-logs/entity/${entityType}/${idKey}`);
}

/**
 * Export audit logs to file (CSV, JSON, or Excel)
 * Returns Blob for download
 */
export async function exportAuditLogs(
  params: AuditLogExportParams
): Promise<Blob> {
  const queryParams = new URLSearchParams();

  if (params.startDate) queryParams.set('startDate', params.startDate);
  if (params.endDate) queryParams.set('endDate', params.endDate);
  if (params.entityType) queryParams.set('entityType', params.entityType);
  if (params.actionType) queryParams.set('actionType', params.actionType);
  if (params.personIdKey) queryParams.set('personIdKey', params.personIdKey);
  if (params.format) queryParams.set('format', params.format);

  const query = queryParams.toString();
  const endpoint = `/audit-logs/export${query ? `?${query}` : ''}`;

  // Use fetch directly for blob response since apiClient expects JSON
  const headers: HeadersInit = {};
  const token = getAccessToken();
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    method: 'GET',
    headers,
  });

  if (!response.ok) {
    throw new Error(`Failed to export audit logs: ${response.statusText}`);
  }

  return response.blob();
}
