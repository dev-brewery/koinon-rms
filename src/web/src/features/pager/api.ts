/**
 * Pager API service
 */

import { get, post } from '@/services/api/client';
import type { IdKey, DateTime } from '@/services/api/types';

// ============================================================================
// Types
// ============================================================================

export enum PagerMessageType {
  PickupNeeded = 'PickupNeeded',
  NeedsAttention = 'NeedsAttention',
  ServiceEnding = 'ServiceEnding',
  Custom = 'Custom',
}

export enum PagerMessageStatus {
  Pending = 'Pending',
  Sent = 'Sent',
  Delivered = 'Delivered',
  Failed = 'Failed',
}

export interface PagerAssignment {
  idKey: IdKey;
  pagerNumber: number;
  attendanceIdKey: IdKey;
  childName: string;
  groupName: string;
  locationName: string;
  parentPhoneNumber: string | null;
  checkedInAt: DateTime;
  messagesSentCount: number;
}

export interface PagerMessage {
  idKey: IdKey;
  messageType: PagerMessageType;
  messageText: string;
  status: PagerMessageStatus;
  sentDateTime: DateTime;
  deliveredDateTime: DateTime | null;
  sentByPersonName: string;
}

export interface PageHistory {
  idKey: IdKey;
  pagerNumber: number;
  childName: string;
  parentPhoneNumber: string;
  messages: PagerMessage[];
}

export interface SendPageRequest {
  pagerNumber: string;
  messageType: PagerMessageType;
  customMessage?: string;
}

// ============================================================================
// API Functions
// ============================================================================

/**
 * Search for pager assignments by pager number or child name
 * @param searchTerm Optional search term (pager number or child name)
 * @param date Optional date filter (defaults to today)
 */
export async function searchPagers(
  searchTerm?: string,
  date?: string
): Promise<PagerAssignment[]> {
  const queryParams = new URLSearchParams();

  if (searchTerm) {
    queryParams.append('searchTerm', searchTerm);
  }

  if (date) {
    queryParams.append('date', date);
  }

  const query = queryParams.toString();
  const endpoint = `/pager/search${query ? `?${query}` : ''}`;

  const response = await get<PagerAssignment[]>(endpoint);
  return response;
}

/**
 * Send a page to a parent via SMS
 * @param request Page request with pager number, message type, and optional custom message
 */
export async function sendPage(request: SendPageRequest): Promise<PagerMessage> {
  const response = await post<PagerMessage>('/pager/send', request);
  return response;
}

/**
 * Get page history for a specific pager number
 * @param pagerNumber The numeric pager number
 * @param date Optional date filter (defaults to today)
 */
export async function getPageHistory(
  pagerNumber: number,
  date?: string
): Promise<PageHistory> {
  const queryParams = new URLSearchParams();

  if (date) {
    queryParams.append('date', date);
  }

  const query = queryParams.toString();
  const endpoint = `/pager/${pagerNumber}/history${query ? `?${query}` : ''}`;

  const response = await get<PageHistory>(endpoint);
  return response;
}

/**
 * Get the next available pager number for a campus
 * @param campusId Optional campus IdKey
 */
export async function getNextPagerNumber(campusId?: IdKey): Promise<number> {
  const queryParams = new URLSearchParams();

  if (campusId) {
    queryParams.append('campusId', campusId);
  }

  const query = queryParams.toString();
  const endpoint = `/pager/next-number${query ? `?${query}` : ''}`;

  const response = await get<number>(endpoint);
  return response;
}
