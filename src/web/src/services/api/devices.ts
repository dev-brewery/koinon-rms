/**
 * Devices API service
 */

import { get, post, put, del } from './client';
import type {
  DeviceSummaryDto,
  DeviceDetailDto,
  DevicesSearchParams,
  CreateDeviceRequest,
  UpdateDeviceRequest,
} from './types';

/**
 * List devices with optional filters
 */
export async function getDevices(
  params: DevicesSearchParams = {}
): Promise<DeviceSummaryDto[]> {
  const queryParams = new URLSearchParams();

  if (params.q) queryParams.set('q', params.q);
  if (params.campusId) queryParams.set('campusIdKey', params.campusId);
  if (params.includeInactive) queryParams.set('includeInactive', String(params.includeInactive));
  if (params.page) queryParams.set('page', String(params.page));
  if (params.pageSize) queryParams.set('pageSize', String(params.pageSize));

  const query = queryParams.toString();
  const endpoint = `/devices${query ? `?${query}` : ''}`;

  const response = await get<{ data: DeviceSummaryDto[] }>(endpoint);
  return response.data;
}

/**
 * Get device details by IdKey
 */
export async function getDeviceByIdKey(idKey: string): Promise<DeviceDetailDto> {
  const response = await get<{ data: DeviceDetailDto }>(`/devices/${idKey}`);
  return response.data;
}

/**
 * Create a new device
 */
export async function createDevice(request: CreateDeviceRequest): Promise<DeviceDetailDto> {
  const response = await post<{ data: DeviceDetailDto }>('/devices', request);
  return response.data;
}

/**
 * Update an existing device
 */
export async function updateDevice(
  idKey: string,
  request: UpdateDeviceRequest
): Promise<DeviceDetailDto> {
  const response = await put<{ data: DeviceDetailDto }>(`/devices/${idKey}`, request);
  return response.data;
}

/**
 * Delete a device
 */
export async function deleteDevice(idKey: string): Promise<void> {
  await del<void>(`/devices/${idKey}`);
}

export interface GenerateKioskTokenResponse {
  token: string;
  deviceIdKey: string;
  deviceName: string;
  expiresAt?: string;
}

/**
 * Generate a kiosk token for a device
 */
export async function generateKioskToken(idKey: string): Promise<GenerateKioskTokenResponse> {
  const response = await post<{ data: GenerateKioskTokenResponse }>(`/devices/${idKey}/token`, {});
  return response.data;
}
