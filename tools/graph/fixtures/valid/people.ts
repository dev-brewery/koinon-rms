/**
 * People API service for testing graph generation.
 * Follows project conventions: uses client wrapper functions.
 */

import { get, post, put, del } from './client';
import type {
  PagedResult,
  PersonSearchParams,
  PersonSummaryDto,
  PersonDetailDto,
  CreatePersonRequest,
  UpdatePersonRequest,
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
  if (params.page) queryParams.set('page', String(params.page));
  if (params.pageSize) queryParams.set('pageSize', String(params.pageSize));

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
 * Soft-delete a person
 */
export async function deletePerson(idKey: string): Promise<void> {
  await del<void>(`/people/${idKey}`);
}
