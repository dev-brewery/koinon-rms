/**
 * Follow-up hooks using TanStack Query
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import * as followUpApi from './api';
import type { FollowUpStatus } from './api';
import type { IdKey } from '@/services/api/types';

/**
 * Query key factory for follow-ups
 */
const followUpKeys = {
  all: ['followups'] as const,
  pending: (assignedToIdKey?: IdKey) =>
    [...followUpKeys.all, 'pending', { assignedToIdKey }] as const,
  detail: (idKey: IdKey) => [...followUpKeys.all, 'detail', idKey] as const,
};

/**
 * Get pending follow-ups
 */
export function usePendingFollowUps(assignedToIdKey?: IdKey) {
  return useQuery({
    queryKey: followUpKeys.pending(assignedToIdKey),
    queryFn: () => followUpApi.getPendingFollowUps(assignedToIdKey),
    staleTime: 30 * 1000, // 30 seconds
  });
}

/**
 * Get a specific follow-up
 */
export function useFollowUp(idKey: IdKey) {
  return useQuery({
    queryKey: followUpKeys.detail(idKey),
    queryFn: () => followUpApi.getFollowUp(idKey),
    staleTime: 30 * 1000, // 30 seconds
  });
}

/**
 * Update follow-up status
 */
export function useUpdateFollowUpStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      idKey,
      status,
      notes,
    }: {
      idKey: IdKey;
      status: FollowUpStatus;
      notes?: string;
    }) => followUpApi.updateFollowUpStatus(idKey, status, notes),
    onSuccess: (updatedFollowUp) => {
      // Update the detail cache
      queryClient.setQueryData(
        followUpKeys.detail(updatedFollowUp.idKey),
        updatedFollowUp
      );

      // Invalidate pending list to refetch
      queryClient.invalidateQueries({
        queryKey: followUpKeys.all,
      });
    },
  });
}

/**
 * Assign follow-up to a user
 */
export function useAssignFollowUp() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      idKey,
      assignedToIdKey,
    }: {
      idKey: IdKey;
      assignedToIdKey: IdKey;
    }) => followUpApi.assignFollowUp(idKey, assignedToIdKey),
    onSuccess: () => {
      // Invalidate all follow-up queries to refetch
      queryClient.invalidateQueries({
        queryKey: followUpKeys.all,
      });
    },
  });
}
