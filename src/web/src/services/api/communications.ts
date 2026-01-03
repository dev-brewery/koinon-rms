/**
 * Communications API service
 */

import { get, post, put } from './client';
import type { PagedResult } from './types';
import type {
  CreateCommunicationDto,
  CommunicationDto,
  CommunicationRecipientDto,
  CommunicationSummaryDto,
  CommunicationsParams,
  MergeFieldDto,
  CommunicationPreviewRequest,
  CommunicationPreviewResponse,
  CommunicationPreferenceDto,
  UpdateCommunicationPreferenceDto,
  BulkUpdatePreferencesDto,
} from '@/types/communication';

// Re-export types from central module for backward compatibility
export type {
  CreateCommunicationDto,
  CommunicationDto,
  CommunicationRecipientDto,
  CommunicationSummaryDto,
  CommunicationsParams,
  MergeFieldDto,
  CommunicationPreviewRequest,
  CommunicationPreviewResponse,
  CommunicationPreferenceDto,
  UpdateCommunicationPreferenceDto,
  BulkUpdatePreferencesDto,
};

// Type alias for API compatibility
export type CreateCommunicationRequest = CreateCommunicationDto;

// ============================================================================
// API Functions
// ============================================================================

/**
 * Create a new communication
 */
export async function createCommunication(
  request: CreateCommunicationRequest
): Promise<CommunicationDto> {
  const response = await post<{ data: CommunicationDto }>('/communications', request);
  return response.data;
}

/**
 * Send (queue) a communication for delivery
 */
export async function sendCommunication(idKey: string): Promise<CommunicationDto> {
  const response = await post<{ data: CommunicationDto }>(`/communications/${idKey}/send`);
  return response.data;
}

/**
 * Schedule a communication for future delivery
 */
export async function scheduleCommunication(
  idKey: string,
  scheduledDateTime: string
): Promise<CommunicationDto> {
  const response = await post<{ data: CommunicationDto }>(
    `/communications/${idKey}/schedule`,
    { scheduledDateTime }
  );
  return response.data;
}

/**
 * Cancel a scheduled communication
 */
export async function cancelSchedule(idKey: string): Promise<CommunicationDto> {
  const response = await post<{ data: CommunicationDto }>(
    `/communications/${idKey}/cancel-schedule`
  );
  return response.data;
}

/**
 * Get a single communication by IdKey
 */
export async function getCommunication(idKey: string): Promise<CommunicationDto> {
  const response = await get<{ data: CommunicationDto }>(`/communications/${idKey}`);
  return response.data;
}

/**
 * List communications with optional filters
 */
export async function getCommunications(
  params: CommunicationsParams = {}
): Promise<PagedResult<CommunicationSummaryDto>> {
  const queryParams = new URLSearchParams();

  if (params.page) queryParams.set('page', String(params.page));
  if (params.pageSize) queryParams.set('pageSize', String(params.pageSize));
  if (params.status) queryParams.set('status', params.status);

  const query = queryParams.toString();
  const endpoint = `/communications${query ? `?${query}` : ''}`;

  return get<PagedResult<CommunicationSummaryDto>>(endpoint);
}

/**
 * Get available merge fields for communications
 */
export async function getMergeFields(): Promise<MergeFieldDto[]> {
  const response = await get<{ data: MergeFieldDto[] }>('/communications/merge-fields');
  return response.data;
}

/**
 * Preview a communication with merge fields resolved
 */
export async function previewCommunication(
  request: CommunicationPreviewRequest
): Promise<CommunicationPreviewResponse> {
  const response = await post<{ data: CommunicationPreviewResponse }>(
    '/communications/preview',
    request
  );
  return response.data;
}

// ============================================================================
// Communication Preferences API
// ============================================================================

/**
 * Get communication preferences for a person
 * Returns array of preferences (Email and/or SMS)
 * No record means opted-in (default)
 */
export async function getCommunicationPreferences(
  personIdKey: string
): Promise<CommunicationPreferenceDto[]> {
  const response = await get<{ data: CommunicationPreferenceDto[] }>(
    `/people/${personIdKey}/communication-preferences`
  );
  return response.data;
}

/**
 * Update a single communication preference for a person
 */
export async function updateCommunicationPreference(
  personIdKey: string,
  type: 'Email' | 'Sms',
  request: UpdateCommunicationPreferenceDto
): Promise<CommunicationPreferenceDto> {
  const response = await put<{ data: CommunicationPreferenceDto }>(
    `/people/${personIdKey}/communication-preferences/${type}`,
    request
  );
  return response.data;
}

/**
 * Bulk update multiple communication preferences for a person
 */
export async function bulkUpdateCommunicationPreferences(
  personIdKey: string,
  request: BulkUpdatePreferencesDto
): Promise<CommunicationPreferenceDto[]> {
  const response = await put<{ data: CommunicationPreferenceDto[] }>(
    `/people/${personIdKey}/communication-preferences`,
    request
  );
  return response.data;
}
