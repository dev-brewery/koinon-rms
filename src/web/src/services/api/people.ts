/**
 * People API service
 */

import { get, post, put, del } from './client';
import type {
  PagedResult,
  PersonSearchParams,
  PersonSummaryDto,
  PersonDetailDto,
  CreatePersonRequest,
  UpdatePersonRequest,
  PersonFamilyResponse,
  GroupMembershipDto,
  PersonGroupsParams,
} from './types';

/**
 * Search and list people with optional filters
 */
export async function searchPeople(
  params: PersonSearchParams = {}
): Promise<PagedResult<PersonSummaryDto>> {
  const queryParams = new URLSearchParams();

  if (params.q) queryParams.set('q', params.q);
  if (params.firstName) queryParams.set('firstName', params.firstName);
  if (params.lastName) queryParams.set('lastName', params.lastName);
  if (params.email) queryParams.set('email', params.email);
  if (params.phone) queryParams.set('phone', params.phone);
  if (params.recordStatusId) queryParams.set('recordStatusId', params.recordStatusId);
  if (params.connectionStatusId) queryParams.set('connectionStatusId', params.connectionStatusId);
  if (params.campusId) queryParams.set('campusId', params.campusId);
  if (params.includeInactive) queryParams.set('includeInactive', String(params.includeInactive));
  if (params.page) queryParams.set('page', String(params.page));
  if (params.pageSize) queryParams.set('pageSize', String(params.pageSize));
  if (params.sortBy) queryParams.set('sortBy', params.sortBy);
  if (params.sortDir) queryParams.set('sortDir', params.sortDir);

  const query = queryParams.toString();
  const endpoint = `/people${query ? `?${query}` : ''}`;

  return get<PagedResult<PersonSummaryDto>>(endpoint);
}

/**
 * Get a single person by IdKey with full details
 */
export async function getPersonByIdKey(idKey: string): Promise<PersonDetailDto> {
  const response = await get<{ data: PersonDetailDto }>(`/people/${idKey}`);
  return response.data;
}

/**
 * Create a new person
 */
export async function createPerson(request: CreatePersonRequest): Promise<PersonDetailDto> {
  const response = await post<{ data: PersonDetailDto }>('/people', request);
  return response.data;
}

/**
 * Update an existing person
 */
export async function updatePerson(
  idKey: string,
  request: UpdatePersonRequest
): Promise<PersonDetailDto> {
  const response = await put<{ data: PersonDetailDto }>(`/people/${idKey}`, request);
  return response.data;
}

/**
 * Soft-delete a person (set record status to Inactive)
 */
export async function deletePerson(idKey: string): Promise<void> {
  await del<void>(`/people/${idKey}`);
}

/**
 * Get the person's family members
 */
export async function getPersonFamily(idKey: string): Promise<PersonFamilyResponse> {
  const response = await get<{ data: PersonFamilyResponse }>(`/people/${idKey}/family`);
  return response.data;
}

/**
 * Get groups the person belongs to (excluding family)
 */
export async function getPersonGroups(
  idKey: string,
  params: PersonGroupsParams = {}
): Promise<PagedResult<GroupMembershipDto>> {
  const queryParams = new URLSearchParams();

  if (params.groupTypeId) queryParams.set('groupTypeId', params.groupTypeId);
  if (params.includeInactive) queryParams.set('includeInactive', String(params.includeInactive));
  if (params.page) queryParams.set('page', String(params.page));
  if (params.pageSize) queryParams.set('pageSize', String(params.pageSize));

  const query = queryParams.toString();
  const endpoint = `/people/${idKey}/groups${query ? `?${query}` : ''}`;

  return get<PagedResult<GroupMembershipDto>>(endpoint);
}
