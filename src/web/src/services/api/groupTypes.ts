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

  return get<GroupTypeAdminDto[]>(endpoint);
}

/**
 * Get group type details by IdKey
 */
export async function getGroupType(idKey: string): Promise<GroupTypeDetailAdminDto> {
  return get<GroupTypeDetailAdminDto>(`${BASE_URL}/${idKey}`);
}

/**
 * Create a new group type
 */
export async function createGroupType(request: CreateGroupTypeRequest): Promise<GroupTypeAdminDto> {
  return post<GroupTypeAdminDto>(BASE_URL, request);
}

/**
 * Update an existing group type
 */
export async function updateGroupType(
  idKey: string,
  request: UpdateGroupTypeRequest
): Promise<GroupTypeAdminDto> {
  return put<GroupTypeAdminDto>(`${BASE_URL}/${idKey}`, request);
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
  return get<GroupTypeGroupDto[]>(`${BASE_URL}/${idKey}/groups`);
}
