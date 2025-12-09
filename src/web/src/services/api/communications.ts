/**
 * Communications API service
 */

import { get, post } from './client';
import type { PagedResult, IdKey } from './types';

// ============================================================================
// Request Types
// ============================================================================

export interface CreateCommunicationRequest {
  communicationType: 'Email' | 'Sms';
  subject?: string;
  body: string;
  fromEmail?: string;
  fromName?: string;
  replyToEmail?: string;
  note?: string;
  groupIdKeys: string[];
}

// ============================================================================
// Response Types
// ============================================================================

export interface CommunicationRecipientDto {
  idKey: IdKey;
  personIdKey: IdKey;
  address: string;
  recipientName?: string;
  status: string;
  deliveredDateTime?: string;
  openedDateTime?: string;
  errorMessage?: string;
  groupIdKey?: IdKey;
}

export interface CommunicationDto {
  idKey: IdKey;
  guid: string;
  communicationType: string;
  status: string;
  subject?: string;
  body: string;
  fromEmail?: string;
  fromName?: string;
  replyToEmail?: string;
  sentDateTime?: string;
  recipientCount: number;
  deliveredCount: number;
  failedCount: number;
  openedCount: number;
  note?: string;
  createdDateTime: string;
  modifiedDateTime?: string;
  recipients: CommunicationRecipientDto[];
}

export interface CommunicationSummaryDto {
  idKey: IdKey;
  guid: string;
  communicationType: string;
  status: string;
  subject?: string;
  sentDateTime?: string;
  recipientCount: number;
  deliveredCount: number;
  failedCount: number;
  createdDateTime: string;
}

// ============================================================================
// Query Parameters
// ============================================================================

export interface CommunicationsParams {
  page?: number;
  pageSize?: number;
  status?: string;
}

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
