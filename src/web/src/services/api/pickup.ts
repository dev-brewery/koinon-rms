/**
 * Pickup Verification API service
 *
 * Handles child safety by verifying authorized pickup persons during checkout.
 */

import { get, post } from './client';
import type {
  PickupVerificationResultDto,
  PickupLogDto,
  VerifyPickupRequest,
  RecordPickupRequest,
} from '@/types/authorized-pickup';

// ============================================================================
// Pickup Verification
// ============================================================================

/**
 * Verifies if a person is authorized to pick up a child.
 * Checks the authorized pickup list and authorization levels.
 * Requires CheckInVolunteer or Supervisor role
 */
export async function verifyPickup(
  request: VerifyPickupRequest
): Promise<PickupVerificationResultDto> {
  const response = await post<{ data: PickupVerificationResultDto }>(
    '/checkin/verify-pickup',
    request
  );
  return response.data;
}

/**
 * Records a pickup event in the audit log and checks out the child.
 * Requires CheckInVolunteer or Supervisor role.
 * Supervisor override requires Supervisor role.
 */
export async function recordPickup(
  request: RecordPickupRequest
): Promise<PickupLogDto> {
  // POST returns 201 Created with body directly (not wrapped in data)
  return post<PickupLogDto>('/checkin/record-pickup', request);
}

// ============================================================================
// Pickup History
// ============================================================================

/**
 * Gets the pickup history for a child.
 * Requires Supervisor role
 */
export async function getPickupHistory(
  childIdKey: string,
  fromDate?: string,
  toDate?: string
): Promise<PickupLogDto[]> {
  const params = new URLSearchParams();
  if (fromDate) {
    params.set('fromDate', fromDate);
  }
  if (toDate) {
    params.set('toDate', toDate);
  }

  const queryString = params.toString();
  const url = `/checkin/people/${childIdKey}/pickup-history${queryString ? `?${queryString}` : ''}`;

  const response = await get<{ data: PickupLogDto[] }>(url);
  return response.data;
}
