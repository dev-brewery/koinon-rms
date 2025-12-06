/**
 * Reference Data API service
 * Handles defined types, defined values, campuses, and group types
 */

import { get } from './client';
import type {
  DefinedTypeDto,
  DefinedValueDto,
  CampusDto,
  CampusesParams,
  GroupTypeDto,
} from './types';

/**
 * Get all defined types with their values
 */
export async function getDefinedTypes(): Promise<DefinedTypeDto[]> {
  const response = await get<{ data: DefinedTypeDto[] }>('/defined-types');
  return response.data;
}

/**
 * Get values for a specific defined type by IdKey or GUID
 */
export async function getDefinedTypeValues(idKeyOrGuid: string): Promise<DefinedValueDto[]> {
  const response = await get<{ data: DefinedValueDto[] }>(
    `/defined-types/${idKeyOrGuid}/values`
  );
  return response.data;
}

/**
 * List all campuses
 */
export async function getCampuses(params: CampusesParams = {}): Promise<CampusDto[]> {
  const queryParams = new URLSearchParams();

  if (params.includeInactive) {
    queryParams.set('includeInactive', String(params.includeInactive));
  }

  const query = queryParams.toString();
  const endpoint = `/campuses${query ? `?${query}` : ''}`;

  const response = await get<{ data: CampusDto[] }>(endpoint);
  return response.data;
}

/**
 * List all group types with their roles
 */
export async function getGroupTypes(): Promise<GroupTypeDto[]> {
  const response = await get<{ data: GroupTypeDto[] }>('/group-types');
  return response.data;
}
