/**
 * Communications management hooks using TanStack Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as communicationsApi from '@/services/api/communications';
import type {
  CreateCommunicationRequest,
  CommunicationsParams,
  CommunicationPreviewRequest,
} from '@/services/api/communications';

/**
 * Get a single communication by IdKey
 */
export function useCommunication(idKey?: string) {
  return useQuery({
    queryKey: ['communications', idKey],
    queryFn: () => communicationsApi.getCommunication(idKey!),
    enabled: !!idKey,
    staleTime: 30 * 1000, // 30 seconds - communications status can change
  });
}

/**
 * List communications with filters
 */
export function useCommunications(params: CommunicationsParams = {}) {
  return useQuery({
    queryKey: ['communications', params],
    queryFn: () => communicationsApi.getCommunications(params),
    staleTime: 30 * 1000, // 30 seconds
  });
}

/**
 * Create a new communication
 */
export function useCreateCommunication() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateCommunicationRequest) =>
      communicationsApi.createCommunication(request),
    onSuccess: () => {
      // Invalidate communications list to refetch
      queryClient.invalidateQueries({ queryKey: ['communications'] });
    },
  });
}

/**
 * Send (queue) a communication for delivery
 */
export function useSendCommunication() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (idKey: string) => communicationsApi.sendCommunication(idKey),
    onSuccess: (_, idKey) => {
      // Invalidate specific communication and list
      queryClient.invalidateQueries({ queryKey: ['communications', idKey] });
      queryClient.invalidateQueries({ queryKey: ['communications'] });
    },
  });
}

/**
 * Schedule a communication for future delivery
 */
export function useScheduleCommunication() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ idKey, scheduledDateTime }: { idKey: string; scheduledDateTime: string }) =>
      communicationsApi.scheduleCommunication(idKey, scheduledDateTime),
    onSuccess: (_, { idKey }) => {
      queryClient.invalidateQueries({ queryKey: ['communications', idKey] });
      queryClient.invalidateQueries({ queryKey: ['communications'] });
    },
  });
}

/**
 * Cancel a scheduled communication
 */
export function useCancelSchedule() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (idKey: string) => communicationsApi.cancelSchedule(idKey),
    onSuccess: (_, idKey) => {
      queryClient.invalidateQueries({ queryKey: ['communications', idKey] });
      queryClient.invalidateQueries({ queryKey: ['communications'] });
    },
  });
}

/**
 * Hook to fetch available merge fields
 */
export function useMergeFields() {
  return useQuery({
    queryKey: ['merge-fields'],
    queryFn: communicationsApi.getMergeFields,
    staleTime: 1000 * 60 * 60, // 1 hour - merge fields rarely change
  });
}

/**
 * Hook to preview a communication with merge fields resolved
 */
export function usePreviewCommunication() {
  return useMutation({
    mutationFn: (request: CommunicationPreviewRequest) =>
      communicationsApi.previewCommunication(request),
  });
}
