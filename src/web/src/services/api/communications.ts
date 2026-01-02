/**
 * Communications API service
 */

import { get, post } from './client';
import type { PagedResult } from './types';
import type {
  CreateCommunicationDto,
  CommunicationDto,
  CommunicationRecipientDto,
  CommunicationSummaryDto,
  CommunicationsParams,
} from '@/types/communication';

// Re-export types from central module for backward compatibility
export type {
  CreateCommunicationDto,
  CommunicationDto,
  CommunicationRecipientDto,
  CommunicationSummaryDto,
  CommunicationsParams,
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
