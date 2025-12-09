/**
 * Volunteer Schedule hooks using TanStack Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as volunteerScheduleApi from '@/services/api/volunteerSchedule';
import type {
  CreateAssignmentsRequest,
  UpdateAssignmentStatusRequest,
  GetAssignmentsParams,
} from '@/types/volunteer';

/**
 * Get schedule assignments for a group
 */
export function useGroupAssignments(groupIdKey: string, params: GetAssignmentsParams = {}) {
  return useQuery({
    queryKey: ['group-assignments', groupIdKey, params],
    queryFn: () => volunteerScheduleApi.getAssignments(groupIdKey, params),
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

/**
 * Create schedule assignments
 */
export function useCreateAssignments(groupIdKey: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateAssignmentsRequest) =>
      volunteerScheduleApi.createAssignments(groupIdKey, request),
    onSuccess: () => {
      // Invalidate assignments to refetch
      queryClient.invalidateQueries({ queryKey: ['group-assignments', groupIdKey] });
    },
  });
}

/**
 * Get current user's schedule
 */
export function useMySchedule(params: GetAssignmentsParams = {}) {
  return useQuery({
    queryKey: ['my-schedule', params],
    queryFn: () => volunteerScheduleApi.getMySchedule(params),
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

/**
 * Update assignment status (confirm or decline)
 */
export function useUpdateAssignmentStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      assignmentIdKey,
      request,
    }: {
      assignmentIdKey: string;
      request: UpdateAssignmentStatusRequest;
    }) => volunteerScheduleApi.updateAssignmentStatus(assignmentIdKey, request),
    onSuccess: () => {
      // Invalidate my schedule to refetch
      queryClient.invalidateQueries({ queryKey: ['my-schedule'] });
    },
  });
}
