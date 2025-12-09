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
  return await get<MyGroupDto[]>('/my-groups');
}

/**
 * Get members of a group where current user is a leader
 */
export async function getMyGroupMembers(
  groupIdKey: string
): Promise<MyGroupMemberDetailDto[]> {
  return await get<MyGroupMemberDetailDto[]>(
    `/my-groups/${groupIdKey}/members`
  );
}

/**
 * Update a group member (role, status, note)
 */
export async function updateGroupMember(
  groupIdKey: string,
  memberIdKey: string,
  request: UpdateGroupMemberRequest
): Promise<MyGroupMemberDetailDto> {
  return await put<MyGroupMemberDetailDto>(
    `/my-groups/${groupIdKey}/members/${memberIdKey}`,
    request
  );
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
