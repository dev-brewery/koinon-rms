/**
 * React Query hooks for Campuses
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as campusesApi from '@/services/api/campuses';
import type { CreateCampusRequest, UpdateCampusRequest } from '@/types/campus';

/**
 * Get all campuses
 */
export function useCampuses(includeInactive = false) {
  return useQuery({
    queryKey: ['campuses', { includeInactive }],
    queryFn: () => campusesApi.getCampuses(includeInactive),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get single campus details
 */
export function useCampus(idKey?: string) {
  return useQuery({
    queryKey: ['campuses', idKey],
    queryFn: () => campusesApi.getCampus(idKey!),
    enabled: !!idKey,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

/**
 * Create a new campus
 */
export function useCreateCampus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateCampusRequest) => campusesApi.createCampus(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['campuses'] });
    },
  });
}

/**
 * Update an existing campus
 */
export function useUpdateCampus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ idKey, request }: { idKey: string; request: UpdateCampusRequest }) =>
      campusesApi.updateCampus(idKey, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['campuses'] });
    },
  });
}

/**
 * Delete a campus
 */
export function useDeleteCampus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (idKey: string) => campusesApi.deleteCampus(idKey),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['campuses'] });
    },
  });
}
