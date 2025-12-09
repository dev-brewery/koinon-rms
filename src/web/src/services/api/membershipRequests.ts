/**
 * Group Membership Requests API service
 */

import { get, post, put } from './client';
import type {
  GroupMemberRequestDto,
  SubmitMembershipRequestRequest,
  ProcessMembershipRequestRequest,
} from './types';

/**
 * Submit a membership request to join a group
 */
export async function submitMembershipRequest(
  groupIdKey: string,
  request: SubmitMembershipRequestRequest
): Promise<GroupMemberRequestDto> {
  return await post<GroupMemberRequestDto>(
    `/groups/${groupIdKey}/membership-requests`,
    request
  );
}

/**
 * Get pending membership requests for a group (leader only)
 */
export async function getPendingRequests(
  groupIdKey: string
): Promise<GroupMemberRequestDto[]> {
  return await get<GroupMemberRequestDto[]>(
    `/groups/${groupIdKey}/membership-requests`
  );
}

/**
 * Process (approve or deny) a membership request
 */
export async function processRequest(
  groupIdKey: string,
  requestIdKey: string,
  request: ProcessMembershipRequestRequest
): Promise<GroupMemberRequestDto> {
  return await put<GroupMemberRequestDto>(
    `/groups/${groupIdKey}/membership-requests/${requestIdKey}`,
    request
  );
}
