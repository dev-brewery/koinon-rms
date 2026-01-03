/**
 * React Query hooks for Locations
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as locationsApi from '@/services/api/locations';
import type { CreateLocationRequest, UpdateLocationRequest } from '@/types/location';

interface UseLocationsOptions {
  campusIdKey?: string;
  includeInactive?: boolean;
}

/**
 * Get all locations (flat list)
 */
export function useLocations(options?: UseLocationsOptions) {
  return useQuery({
    queryKey: ['locations', options],
    queryFn: () => locationsApi.getLocations(options),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get location tree (hierarchical structure)
 */
export function useLocationTree(options?: UseLocationsOptions) {
  return useQuery({
    queryKey: ['locations', 'tree', options],
    queryFn: () => locationsApi.getLocationTree(options),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get single location details
 */
export function useLocation(idKey?: string) {
  return useQuery({
    queryKey: ['locations', idKey],
    queryFn: () => locationsApi.getLocation(idKey!),
    enabled: !!idKey,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

/**
 * Create a new location
 */
export function useCreateLocation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateLocationRequest) => locationsApi.createLocation(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['locations'] });
    },
  });
}

/**
 * Update an existing location
 */
export function useUpdateLocation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ idKey, request }: { idKey: string; request: UpdateLocationRequest }) =>
      locationsApi.updateLocation(idKey, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['locations'] });
    },
  });
}

/**
 * Delete a location
 */
export function useDeleteLocation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (idKey: string) => locationsApi.deleteLocation(idKey),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['locations'] });
    },
  });
}
