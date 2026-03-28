/**
 * Campuses API service
 */

import { get, post, put, del } from './client';
import type { CampusDto } from './types';
import type { CreateCampusRequest, UpdateCampusRequest } from '@/types/campus';

const BASE_URL = '/campuses';

/**
 * Get all campuses
 */
export async function getCampuses(includeInactive = false): Promise<CampusDto[]> {
  const queryParams = new URLSearchParams();
  if (includeInactive) {
    queryParams.set('includeInactive', 'true');
  }

  const query = queryParams.toString();
  const endpoint = `${BASE_URL}${query ? `?${query}` : ''}`;

  const response = await get<{ data: CampusDto[] }>(endpoint);
  return response.data;
}

/**
 * Get campus details by IdKey
 */
export async function getCampus(idKey: string): Promise<CampusDto> {
  const response = await get<{ data: CampusDto }>(`${BASE_URL}/${idKey}`);
  return response.data;
}

/**
 * Create a new campus
 */
export async function createCampus(request: CreateCampusRequest): Promise<CampusDto> {
  const response = await post<{ data: CampusDto }>(BASE_URL, request);
  return response.data;
}

/**
 * Update an existing campus
 */
export async function updateCampus(
  idKey: string,
  request: UpdateCampusRequest
): Promise<CampusDto> {
  const response = await put<{ data: CampusDto }>(`${BASE_URL}/${idKey}`, request);
  return response.data;
}

/**
 * Delete a campus
 */
export async function deleteCampus(idKey: string): Promise<void> {
  await del<void>(`${BASE_URL}/${idKey}`);
}
