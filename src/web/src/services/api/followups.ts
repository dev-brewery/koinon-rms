/**
 * Follow-ups API service
 */

import { get, put } from './client';
import type {
  FollowUpDto,
  UpdateFollowUpStatusRequest,
  AssignFollowUpRequest,
} from '@/types/followup';

/**
 * Gets all pending follow-ups, optionally filtered by assignee.
 * Requires authentication
 */
export async function getPendingFollowUps(
  assignedToIdKey?: string
): Promise<FollowUpDto[]> {
  const params = new URLSearchParams();
  if (assignedToIdKey) {
    params.set('assignedToIdKey', assignedToIdKey);
  }

  const queryString = params.toString();
  const url = `/followups/pending${queryString ? `?${queryString}` : ''}`;

  const response = await get<{ data: FollowUpDto[] }>(url);
  return response.data;
}

/**
 * Gets a follow-up by IdKey.
 * Requires authentication
 */
export async function getFollowUp(idKey: string): Promise<FollowUpDto> {
  const response = await get<{ data: FollowUpDto }>(`/followups/${idKey}`);
  return response.data;
}

/**
 * Updates the status of a follow-up task.
 * Requires authentication
 */
export async function updateFollowUpStatus(
  idKey: string,
  request: UpdateFollowUpStatusRequest
): Promise<FollowUpDto> {
  const response = await put<{ data: FollowUpDto }>(
    `/followups/${idKey}/status`,
    request
  );
  return response.data;
}

/**
 * Assigns a follow-up task to a person.
 * Requires authentication
 */
export async function assignFollowUp(
  idKey: string,
  request: AssignFollowUpRequest
): Promise<void> {
  await put<void>(`/followups/${idKey}/assign`, request);
}
