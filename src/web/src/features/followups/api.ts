/**
 * Follow-up API service
 */

import { get, put } from '@/services/api/client';
import type { IdKey } from '@/services/api/types';
import {
  FollowUpStatus,
  type FollowUpDto,
  type UpdateFollowUpStatusRequest,
  type AssignFollowUpRequest,
} from '@/types/followup';

// Re-export types for backward compatibility
export type { FollowUpDto, UpdateFollowUpStatusRequest, AssignFollowUpRequest };
export { FollowUpStatus };

// ============================================================================
// API Functions
// ============================================================================

/**
 * Get all pending follow-ups
 * Optionally filter by assigned user
 */
export async function getPendingFollowUps(
  assignedToIdKey?: IdKey
): Promise<FollowUpDto[]> {
  const queryParams = new URLSearchParams();

  if (assignedToIdKey) {
    queryParams.append('assignedToIdKey', assignedToIdKey);
  }

  const query = queryParams.toString();
  const endpoint = `/followups/pending${query ? `?${query}` : ''}`;

  const response = await get<{ data: FollowUpDto[] }>(endpoint);
  return response.data;
}

/**
 * Get a specific follow-up by ID
 */
export async function getFollowUp(idKey: IdKey): Promise<FollowUpDto> {
  const response = await get<{ data: FollowUpDto }>(`/followups/${idKey}`);
  return response.data;
}

/**
 * Update the status of a follow-up
 */
export async function updateFollowUpStatus(
  idKey: IdKey,
  status: FollowUpStatus,
  notes?: string
): Promise<FollowUpDto> {
  const request: UpdateFollowUpStatusRequest = { status, notes };
  const response = await put<{ data: FollowUpDto }>(
    `/followups/${idKey}/status`,
    request
  );
  return response.data;
}

/**
 * Assign a follow-up to a user
 */
export async function assignFollowUp(
  idKey: IdKey,
  assignedToIdKey: IdKey
): Promise<void> {
  const request: AssignFollowUpRequest = { assignedToIdKey };
  await put(`/followups/${idKey}/assign`, request);
}
