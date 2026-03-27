/**
 * Device management hooks using TanStack Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as devicesApi from '@/services/api/devices';
import type { GenerateKioskTokenResponse } from '@/services/api/devices';
import type {
  DevicesSearchParams,
  CreateDeviceRequest,
  UpdateDeviceRequest,
} from '@/services/api/types';

/**
 * Search for devices with optional filters
 */
export function useDevices(params: DevicesSearchParams = {}) {
  return useQuery({
    queryKey: ['devices', params],
    queryFn: () => devicesApi.getDevices(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get a single device by IdKey
 */
export function useDevice(idKey?: string) {
  return useQuery({
    queryKey: ['devices', idKey],
    queryFn: () => devicesApi.getDeviceByIdKey(idKey!),
    enabled: !!idKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Create a new device
 */
export function useCreateDevice() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateDeviceRequest) => devicesApi.createDevice(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices'] });
    },
  });
}

/**
 * Update an existing device
 */
export function useUpdateDevice() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ idKey, request }: { idKey: string; request: UpdateDeviceRequest }) =>
      devicesApi.updateDevice(idKey, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['devices', variables.idKey] });
      queryClient.invalidateQueries({ queryKey: ['devices'] });
    },
  });
}

/**
 * Delete a device
 */
export function useDeleteDevice() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (idKey: string) => devicesApi.deleteDevice(idKey),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices'] });
    },
  });
}

/**
 * Generate a kiosk token for a device
 */
export function useGenerateKioskToken() {
  const queryClient = useQueryClient();

  return useMutation<GenerateKioskTokenResponse, Error, string>({
    mutationFn: (idKey: string) => devicesApi.generateKioskToken(idKey),
    onSuccess: (_, idKey) => {
      queryClient.invalidateQueries({ queryKey: ['devices', idKey] });
    },
  });
}
