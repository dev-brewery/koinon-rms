/**
 * Authorized Pickup API service
 */

import { get, post, put, del } from './client';
import type {
  AuthorizedPickupDto,
  CreateAuthorizedPickupRequest,
  UpdateAuthorizedPickupRequest,
} from '@/types/authorized-pickup';

/**
 * Gets all authorized pickup persons for a child
 * Requires Supervisor role
 */
export async function getAuthorizedPickups(
  childIdKey: string
): Promise<AuthorizedPickupDto[]> {
  const response = await get<{ data: AuthorizedPickupDto[] }>(
    `/people/${childIdKey}/authorized-pickups`
  );
  return response.data;
}

/**
 * Creates a new authorized pickup person for a child
 * Requires Supervisor role
 */
export async function createAuthorizedPickup(
  childIdKey: string,
  request: CreateAuthorizedPickupRequest
): Promise<AuthorizedPickupDto> {
  const response = await post<{ data: AuthorizedPickupDto }>(
    `/people/${childIdKey}/authorized-pickups`,
    request
  );
  return response.data;
}

/**
 * Updates an existing authorized pickup person
 * Requires Supervisor role
 */
export async function updateAuthorizedPickup(
  pickupIdKey: string,
  request: UpdateAuthorizedPickupRequest
): Promise<AuthorizedPickupDto> {
  const response = await put<{ data: AuthorizedPickupDto }>(
    `/authorized-pickups/${pickupIdKey}`,
    request
  );
  return response.data;
}

/**
 * Deletes (deactivates) an authorized pickup person
 * Requires Supervisor role
 */
export async function deleteAuthorizedPickup(
  pickupIdKey: string
): Promise<void> {
  await del<void>(`/authorized-pickups/${pickupIdKey}`);
}

/**
 * Auto-populates the authorized pickup list with adult family members
 * Adds parents and guardians from the child's family as authorized pickups
 * Requires Supervisor role
 */
export async function autoPopulateFamilyMembers(
  childIdKey: string
): Promise<{ message: string; count: number; pickups: AuthorizedPickupDto[] }> {
  const response = await post<{
    data: { message: string; count: number; pickups: AuthorizedPickupDto[] };
  }>(`/people/${childIdKey}/authorized-pickups/auto-populate`, {});
  return response.data;
}
