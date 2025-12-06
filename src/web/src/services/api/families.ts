/**
 * Families API service
 */

import { get, post, put, del } from './client';
import type {
  PagedResult,
  FamiliesSearchParams,
  FamilySummaryDto,
  FamilyDetailDto,
  CreateFamilyRequest,
  UpdateFamilyRequest,
  AddFamilyMemberRequest,
  FamilyMemberDto,
  UpdateFamilyAddressRequest,
  FamilyAddressDto,
  RemoveFamilyMemberParams,
} from './types';

/**
 * List families with optional search and filters
 */
export async function searchFamilies(
  params: FamiliesSearchParams = {}
): Promise<PagedResult<FamilySummaryDto>> {
  const queryParams = new URLSearchParams();

  if (params.q) queryParams.set('q', params.q);
  if (params.campusId) queryParams.set('campusId', params.campusId);
  if (params.includeInactive) queryParams.set('includeInactive', String(params.includeInactive));
  if (params.page) queryParams.set('page', String(params.page));
  if (params.pageSize) queryParams.set('pageSize', String(params.pageSize));

  const query = queryParams.toString();
  const endpoint = `/families${query ? `?${query}` : ''}`;

  return get<PagedResult<FamilySummaryDto>>(endpoint);
}

/**
 * Get family details with members by IdKey
 */
export async function getFamilyByIdKey(idKey: string): Promise<FamilyDetailDto> {
  const response = await get<{ data: FamilyDetailDto }>(`/families/${idKey}`);
  return response.data;
}

/**
 * Create a new family
 */
export async function createFamily(request: CreateFamilyRequest): Promise<FamilyDetailDto> {
  const response = await post<{ data: FamilyDetailDto }>('/families', request);
  return response.data;
}

/**
 * Update family details
 */
export async function updateFamily(
  idKey: string,
  request: UpdateFamilyRequest
): Promise<FamilyDetailDto> {
  const response = await put<{ data: FamilyDetailDto }>(`/families/${idKey}`, request);
  return response.data;
}

/**
 * Add a member to a family
 */
export async function addFamilyMember(
  familyIdKey: string,
  request: AddFamilyMemberRequest
): Promise<FamilyMemberDto> {
  const response = await post<{ data: FamilyMemberDto }>(
    `/families/${familyIdKey}/members`,
    request
  );
  return response.data;
}

/**
 * Remove a member from a family
 */
export async function removeFamilyMember(
  familyIdKey: string,
  personIdKey: string,
  params: RemoveFamilyMemberParams = {}
): Promise<void> {
  const queryParams = new URLSearchParams();

  if (params.removeFromAllGroups) {
    queryParams.set('removeFromAllGroups', String(params.removeFromAllGroups));
  }

  const query = queryParams.toString();
  const endpoint = `/families/${familyIdKey}/members/${personIdKey}${query ? `?${query}` : ''}`;

  await del<void>(endpoint);
}

/**
 * Update family address (affects all members)
 */
export async function updateFamilyAddress(
  familyIdKey: string,
  request: UpdateFamilyAddressRequest
): Promise<FamilyAddressDto> {
  const response = await put<{ data: FamilyAddressDto }>(
    `/families/${familyIdKey}/address`,
    request
  );
  return response.data;
}
