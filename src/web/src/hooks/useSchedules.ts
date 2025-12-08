/**
 * Schedules management hooks using TanStack Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as schedulesApi from '@/services/api/schedules';
import type {
  ScheduleSearchParams,
  CreateScheduleRequest,
  UpdateScheduleRequest,
} from '@/services/api/types';

/**
 * Search for schedules with filters
 */
export function useSchedules(params: ScheduleSearchParams = {}) {
  return useQuery({
    queryKey: ['schedules', params],
    queryFn: () => schedulesApi.searchSchedules(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get a single schedule by IdKey
 */
export function useSchedule(idKey?: string) {
  return useQuery({
    queryKey: ['schedules', idKey],
    queryFn: () => schedulesApi.getScheduleByIdKey(idKey!),
    enabled: !!idKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get upcoming occurrences for a schedule
 */
export function useScheduleOccurrences(idKey?: string, startDate?: string, count: number = 10) {
  return useQuery({
    queryKey: ['schedules', idKey, 'occurrences', startDate, count],
    queryFn: () => schedulesApi.getScheduleOccurrences(idKey!, startDate, count),
    enabled: !!idKey,
    staleTime: 1 * 60 * 1000, // 1 minute
  });
}

/**
 * Create a new schedule
 */
export function useCreateSchedule() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateScheduleRequest) => schedulesApi.createSchedule(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schedules'] });
    },
  });
}

/**
 * Update an existing schedule
 */
export function useUpdateSchedule() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ idKey, request }: { idKey: string; request: UpdateScheduleRequest }) =>
      schedulesApi.updateSchedule(idKey, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['schedules', variables.idKey] });
      queryClient.invalidateQueries({ queryKey: ['schedules'] });
    },
  });
}

/**
 * Delete (deactivate) a schedule
 */
export function useDeleteSchedule() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (idKey: string) => schedulesApi.deleteSchedule(idKey),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schedules'] });
    },
  });
}
