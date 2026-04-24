/**
 * Locations API service
 */

import { get, post, put, del } from './client';
import type { LocationDto, LocationSummaryDto, CreateLocationRequest, UpdateLocationRequest } from '@/types/location';

const BASE_URL = '/locations';

interface GetLocationsParams {
  campusIdKey?: string;
  includeInactive?: boolean;
}

/**
 * Get all locations (flat list)
 */
export async function getLocations(params?: GetLocationsParams): Promise<LocationSummaryDto[]> {
  const searchParams = new URLSearchParams();
  if (params?.campusIdKey) {
    searchParams.set('campusIdKey', params.campusIdKey);
  }
  if (params?.includeInactive) {
    searchParams.set('includeInactive', 'true');
  }

  const query = searchParams.toString();
  const endpoint = `${BASE_URL}${query ? `?${query}` : ''}`;

  // Backend: Ok(new { data = result }) — unwrap envelope (see #693).
  const response = await get<{ data: LocationSummaryDto[] }>(endpoint);
  return response.data;
}

/**
 * Get location tree (hierarchical structure)
 */
export async function getLocationTree(params?: GetLocationsParams): Promise<LocationDto[]> {
  const searchParams = new URLSearchParams();
  if (params?.campusIdKey) {
    searchParams.set('campusIdKey', params.campusIdKey);
  }
  if (params?.includeInactive) {
    searchParams.set('includeInactive', 'true');
  }

  const query = searchParams.toString();
  const endpoint = `${BASE_URL}/tree${query ? `?${query}` : ''}`;

  // Backend: Ok(new { data = result.Value }) — unwrap envelope (see #693).
  const response = await get<{ data: LocationDto[] }>(endpoint);
  return response.data;
}

/**
 * Get location details by IdKey
 */
export async function getLocation(idKey: string): Promise<LocationDto> {
  // Backend: Ok(new { data = result.Value }) — unwrap envelope (see #693).
  const response = await get<{ data: LocationDto }>(`${BASE_URL}/${idKey}`);
  return response.data;
}

/**
 * Create a new location
 */
export async function createLocation(request: CreateLocationRequest): Promise<LocationDto> {
  // Backend: CreatedAtAction(..., new { data = result.Value }) — unwrap envelope (see #693).
  const response = await post<{ data: LocationDto }>(BASE_URL, request);
  return response.data;
}

/**
 * Update an existing location
 */
export async function updateLocation(
  idKey: string,
  request: UpdateLocationRequest
): Promise<LocationDto> {
  // Backend: Ok(new { data = result.Value }) — unwrap envelope (see #693).
  const response = await put<{ data: LocationDto }>(`${BASE_URL}/${idKey}`, request);
  return response.data;
}

/**
 * Delete a location
 */
export async function deleteLocation(idKey: string): Promise<void> {
  await del<void>(`${BASE_URL}/${idKey}`);
}
