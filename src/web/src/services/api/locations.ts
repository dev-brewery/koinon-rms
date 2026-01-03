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

  return get<LocationSummaryDto[]>(endpoint);
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

  return get<LocationDto[]>(endpoint);
}

/**
 * Get location details by IdKey
 */
export async function getLocation(idKey: string): Promise<LocationDto> {
  return get<LocationDto>(`${BASE_URL}/${idKey}`);
}

/**
 * Create a new location
 */
export async function createLocation(request: CreateLocationRequest): Promise<LocationDto> {
  return post<LocationDto>(BASE_URL, request);
}

/**
 * Update an existing location
 */
export async function updateLocation(
  idKey: string,
  request: UpdateLocationRequest
): Promise<LocationDto> {
  return put<LocationDto>(`${BASE_URL}/${idKey}`, request);
}

/**
 * Delete a location
 */
export async function deleteLocation(idKey: string): Promise<void> {
  await del<void>(`${BASE_URL}/${idKey}`);
}
