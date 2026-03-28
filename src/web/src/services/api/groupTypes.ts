/**
 * Group Types Admin API service
 */

import { get, post, put, del } from './client';
import type {
  GroupTypeAdminDto,
  GroupTypeDetailAdminDto,
  CreateGroupTypeRequest,
  UpdateGroupTypeRequest,
  GroupTypeGroupDto,
} from './types';

const BASE_URL = '/admin/group-types';

/**
 * Get all group types
 */
export async function getGroupTypes(includeArchived = false): Promise<GroupTypeAdminDto[]> {
  const queryParams = new URLSearchParams();
  if (includeArchived) {
    queryParams.set('includeArchived', 'true');
  }

  const query = queryParams.toString();
  const endpoint = `${BASE_URL}${query ? `?${query}` : ''}`;

  const response = await get<{ data: GroupTypeAdminDto[] }>(endpoint);
  return response.data;
}

/**
 * Get group type details by IdKey
 */
export async function getGroupType(idKey: string): Promise<GroupTypeDetailAdminDto> {
  const response = await get<{ data: GroupTypeDetailAdminDto }>(`${BASE_URL}/${idKey}`);
  return response.data;
}

/**
 * Create a new group type
 */
export async function createGroupType(request: CreateGroupTypeRequest): Promise<GroupTypeAdminDto> {
  const response = await post<{ data: GroupTypeAdminDto }>(BASE_URL, request);
  return response.data;
}

/**
 * Update an existing group type
 */
export async function updateGroupType(
  idKey: string,
  request: UpdateGroupTypeRequest
): Promise<GroupTypeAdminDto> {
  const response = await put<{ data: GroupTypeAdminDto }>(`${BASE_URL}/${idKey}`, request);
  return response.data;
}

/**
 * Archive a group type
 */
export async function archiveGroupType(idKey: string): Promise<void> {
  await del<void>(`${BASE_URL}/${idKey}`);
}

/**
 * Get groups of a specific type
 */
export async function getGroupsByType(idKey: string): Promise<GroupTypeGroupDto[]> {
  const response = await get<{ data: GroupTypeGroupDto[] }>(`${BASE_URL}/${idKey}/groups`);
  return response.data;
}
