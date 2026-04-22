/**
 * My Groups API service
 * Endpoints for group leaders to manage their groups
 */

import { get, put, del, post } from './client';
import type {
  MyGroupDto,
  MyGroupMemberDetailDto,
  UpdateGroupMemberRequest,
  RecordGroupAttendanceRequest,
} from './types';

/**
 * Get all groups where current user is a leader
 */
export async function getMyGroups(): Promise<MyGroupDto[]> {
  const response = await get<{ data: MyGroupDto[] }>('/my-groups');
  return response.data;
}

/**
 * Get members of a group where current user is a leader
 */
export async function getMyGroupMembers(
  groupIdKey: string
): Promise<MyGroupMemberDetailDto[]> {
  const response = await get<{ data: MyGroupMemberDetailDto[] }>(
    `/my-groups/${groupIdKey}/members`
  );
  return response.data;
}

/**
 * Update a group member (role, status, note)
 */
export async function updateGroupMember(
  groupIdKey: string,
  memberIdKey: string,
  request: UpdateGroupMemberRequest
): Promise<MyGroupMemberDetailDto> {
  const response = await put<{ data: MyGroupMemberDetailDto }>(
    `/my-groups/${groupIdKey}/members/${memberIdKey}`,
    request
  );
  return response.data;
}

/**
 * Remove a member from a group
 */
export async function removeGroupMember(
  groupIdKey: string,
  memberIdKey: string
): Promise<void> {
  await del<void>(`/my-groups/${groupIdKey}/members/${memberIdKey}`);
}

/**
 * Record attendance for a group meeting
 */
export async function recordAttendance(
  groupIdKey: string,
  request: RecordGroupAttendanceRequest
): Promise<void> {
  await post<void>(`/my-groups/${groupIdKey}/attendance`, request);
}
