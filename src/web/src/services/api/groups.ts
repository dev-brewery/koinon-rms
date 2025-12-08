/**
 * Groups API service
 */

import { get, post, put, del } from './client';
import type {
  PagedResult,
  GroupsSearchParams,
  GroupSummaryDto,
  GroupDetailDto,
  GroupMembersParams,
  GroupMemberDetailDto,
  AddGroupMemberRequest,
  CreateGroupRequest,
  UpdateGroupRequest,
} from './types';

/**
 * List groups with optional search and filters
 */
export async function searchGroups(
  params: GroupsSearchParams = {}
): Promise<PagedResult<GroupSummaryDto>> {
  const queryParams = new URLSearchParams();

  if (params.q) queryParams.set('q', params.q);
  if (params.groupTypeId) queryParams.set('groupTypeId', params.groupTypeId);
  if (params.parentGroupId) queryParams.set('parentGroupId', params.parentGroupId);
  if (params.campusId) queryParams.set('campusId', params.campusId);
  if (params.includeInactive) queryParams.set('includeInactive', String(params.includeInactive));
  if (params.page) queryParams.set('page', String(params.page));
  if (params.pageSize) queryParams.set('pageSize', String(params.pageSize));

  const query = queryParams.toString();
  const endpoint = `/groups${query ? `?${query}` : ''}`;

  return get<PagedResult<GroupSummaryDto>>(endpoint);
}

/**
 * Get group details by IdKey
 */
export async function getGroupByIdKey(idKey: string): Promise<GroupDetailDto> {
  const response = await get<{ data: GroupDetailDto }>(`/groups/${idKey}`);
  return response.data;
}

/**
 * Get group members
 */
export async function getGroupMembers(
  groupIdKey: string,
  params: GroupMembersParams = {}
): Promise<PagedResult<GroupMemberDetailDto>> {
  const queryParams = new URLSearchParams();

  if (params.status) queryParams.set('status', params.status);
  if (params.roleId) queryParams.set('roleId', params.roleId);
  if (params.page) queryParams.set('page', String(params.page));
  if (params.pageSize) queryParams.set('pageSize', String(params.pageSize));

  const query = queryParams.toString();
  const endpoint = `/groups/${groupIdKey}/members${query ? `?${query}` : ''}`;

  return get<PagedResult<GroupMemberDetailDto>>(endpoint);
}

/**
 * Add a member to a group
 */
export async function addGroupMember(
  groupIdKey: string,
  request: AddGroupMemberRequest
): Promise<GroupMemberDetailDto> {
  const response = await post<{ data: GroupMemberDetailDto }>(
    `/groups/${groupIdKey}/members`,
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
  await del<void>(`/groups/${groupIdKey}/members/${memberIdKey}`);
}

/**
 * Create a new group
 */
export async function createGroup(
  request: CreateGroupRequest
): Promise<GroupDetailDto> {
  const response = await post<{ data: GroupDetailDto }>('/groups', request);
  return response.data;
}

/**
 * Update a group
 */
export async function updateGroup(
  idKey: string,
  request: UpdateGroupRequest
): Promise<GroupDetailDto> {
  const response = await put<{ data: GroupDetailDto }>(`/groups/${idKey}`, request);
  return response.data;
}

/**
 * Delete (archive) a group
 */
export async function deleteGroup(idKey: string): Promise<void> {
  await del<void>(`/groups/${idKey}`);
}

/**
 * Get child groups of a group
 */
export async function getChildGroups(idKey: string): Promise<PagedResult<GroupSummaryDto>> {
  return get<PagedResult<GroupSummaryDto>>(`/groups/${idKey}/children`);
}
