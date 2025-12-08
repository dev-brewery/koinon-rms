/**
 * Authorized Pickup API service
 */

import { get, post, put, del } from '@/services/api/client';
import type { IdKey, DateTime } from '@/services/api/types';

// ============================================================================
// Types
// ============================================================================

export enum PickupRelationship {
  Parent = 0,
  Grandparent = 1,
  Sibling = 2,
  Guardian = 3,
  Aunt = 4,
  Uncle = 5,
  Friend = 6,
  Other = 7,
}

export enum AuthorizationLevel {
  Always = 0,
  EmergencyOnly = 1,
  Never = 2,
}

export interface AuthorizedPickup {
  idKey: IdKey;
  childIdKey: IdKey;
  childName: string;
  authorizedPersonIdKey?: IdKey;
  authorizedPersonName?: string;
  name?: string;
  phoneNumber?: string;
  relationship: PickupRelationship;
  authorizationLevel: AuthorizationLevel;
  photoUrl?: string;
  isActive: boolean;
}

export interface PickupLog {
  idKey: IdKey;
  attendanceIdKey: IdKey;
  childName: string;
  pickupPersonName: string;
  wasAuthorized: boolean;
  supervisorOverride: boolean;
  supervisorName?: string;
  checkoutDateTime: DateTime;
  notes?: string;
}

export interface PickupVerificationResult {
  isAuthorized: boolean;
  authorizationLevel?: AuthorizationLevel;
  authorizedPickupIdKey?: IdKey;
  message: string;
  requiresSupervisorOverride: boolean;
}

export interface CreateAuthorizedPickupRequest {
  authorizedPersonIdKey?: IdKey;
  name?: string;
  phoneNumber?: string;
  relationship: PickupRelationship;
  authorizationLevel: AuthorizationLevel;
  photoUrl?: string;
  custodyNotes?: string;
}

export interface UpdateAuthorizedPickupRequest {
  relationship?: PickupRelationship;
  authorizationLevel?: AuthorizationLevel;
  photoUrl?: string;
  custodyNotes?: string;
  isActive?: boolean;
}

export interface VerifyPickupRequest {
  attendanceIdKey: IdKey;
  pickupPersonIdKey?: IdKey;
  pickupPersonName?: string;
  securityCode: string;
}

export interface RecordPickupRequest {
  attendanceIdKey: IdKey;
  pickupPersonIdKey?: IdKey;
  pickupPersonName?: string;
  wasAuthorized: boolean;
  authorizedPickupIdKey?: IdKey;
  supervisorOverride: boolean;
  supervisorPersonIdKey?: IdKey;
  notes?: string;
}

// ============================================================================
// API Functions
// ============================================================================

/**
 * Get all authorized pickups for a child
 */
export async function getAuthorizedPickups(
  childIdKey: IdKey
): Promise<AuthorizedPickup[]> {
  const response = await get<AuthorizedPickup[]>(
    `/people/${childIdKey}/authorized-pickups`
  );
  return response;
}

/**
 * Add a new authorized pickup for a child
 */
export async function addAuthorizedPickup(
  childIdKey: IdKey,
  request: CreateAuthorizedPickupRequest
): Promise<AuthorizedPickup> {
  const response = await post<AuthorizedPickup>(
    `/people/${childIdKey}/authorized-pickups`,
    request
  );
  return response;
}

/**
 * Update an existing authorized pickup
 */
export async function updateAuthorizedPickup(
  pickupIdKey: IdKey,
  request: UpdateAuthorizedPickupRequest
): Promise<AuthorizedPickup> {
  const response = await put<AuthorizedPickup>(
    `/authorized-pickups/${pickupIdKey}`,
    request
  );
  return response;
}

/**
 * Delete (deactivate) an authorized pickup
 */
export async function deleteAuthorizedPickup(pickupIdKey: IdKey): Promise<void> {
  await del(`/authorized-pickups/${pickupIdKey}`);
}

/**
 * Auto-populate authorized pickups with family members
 */
export async function autoPopulateFamilyMembers(
  childIdKey: IdKey
): Promise<{ message: string; count: number; pickups: AuthorizedPickup[] }> {
  const response = await post<{
    message: string;
    count: number;
    pickups: AuthorizedPickup[];
  }>(`/people/${childIdKey}/authorized-pickups/auto-populate`);
  return response;
}

/**
 * Verify if a person is authorized to pick up a child
 */
export async function verifyPickup(
  request: VerifyPickupRequest
): Promise<PickupVerificationResult> {
  const response = await post<PickupVerificationResult>(
    '/checkin/verify-pickup',
    request
  );
  return response;
}

/**
 * Record a pickup event and checkout the child
 */
export async function recordPickup(
  request: RecordPickupRequest
): Promise<PickupLog> {
  const response = await post<PickupLog>('/checkin/record-pickup', request);
  return response;
}

/**
 * Get pickup history for a child
 */
export async function getPickupHistory(
  childIdKey: IdKey,
  fromDate?: DateTime,
  toDate?: DateTime
): Promise<PickupLog[]> {
  const queryParams = new URLSearchParams();

  if (fromDate) {
    queryParams.append('fromDate', fromDate);
  }

  if (toDate) {
    queryParams.append('toDate', toDate);
  }

  const query = queryParams.toString();
  const endpoint = `/people/${childIdKey}/pickup-history${query ? `?${query}` : ''}`;

  const response = await get<PickupLog[]>(endpoint);
  return response;
}
