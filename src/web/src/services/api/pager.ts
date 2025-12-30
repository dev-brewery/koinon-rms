/**
 * Pager API service
 */

import { get, post } from './client';
import type {
  PagerAssignmentDto,
  PagerMessageDto,
  PageHistoryDto,
  SendPageRequest,
} from '@/types/pager';

// ============================================================================
// Send Page
// ============================================================================

/**
 * Sends a page to a parent via SMS.
 * Requires Supervisor role
 */
export async function sendPage(request: SendPageRequest): Promise<PagerMessageDto> {
  const response = await post<{ data: PagerMessageDto }>('/pager/send', request);
  return response.data;
}

// ============================================================================
// Search & Lookup
// ============================================================================

/**
 * Searches for pager assignments by pager number or child name.
 * Requires Supervisor role
 */
export async function searchPagers(
  searchTerm?: string,
  date?: string
): Promise<PagerAssignmentDto[]> {
  const params = new URLSearchParams();
  if (searchTerm) {
    params.set('searchTerm', searchTerm);
  }
  if (date) {
    params.set('date', date);
  }

  const queryString = params.toString();
  const url = `/pager/search${queryString ? `?${queryString}` : ''}`;

  const response = await get<{ data: PagerAssignmentDto[] }>(url);
  return response.data;
}

/**
 * Gets page history for a specific pager number.
 * Requires Supervisor role
 */
export async function getPagerHistory(
  pagerNumber: number,
  date?: string
): Promise<PageHistoryDto> {
  const params = new URLSearchParams();
  if (date) {
    params.set('date', date);
  }

  const queryString = params.toString();
  const url = `/pager/${pagerNumber}/history${queryString ? `?${queryString}` : ''}`;

  const response = await get<{ data: PageHistoryDto }>(url);
  return response.data;
}

/**
 * Gets the next available pager number for a campus.
 * Requires authentication
 */
export async function getNextPagerNumber(campusId?: string): Promise<number> {
  const params = new URLSearchParams();
  if (campusId) {
    params.set('campusId', campusId);
  }

  const queryString = params.toString();
  const url = `/pager/next-number${queryString ? `?${queryString}` : ''}`;

  const response = await get<{ data: number }>(url);
  return response.data;
}
