/**
 * Group membership request hooks using TanStack Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as membershipRequestsApi from '@/services/api/membershipRequests';
import type {
  SubmitMembershipRequestRequest,
  ProcessMembershipRequestRequest,
} from '@/services/api/types';

/**
 * Get pending membership requests for a group (leader only)
 */
export function usePendingRequests(groupIdKey?: string) {
  return useQuery({
    queryKey: ['groups', groupIdKey, 'membership-requests'],
    queryFn: () => membershipRequestsApi.getPendingRequests(groupIdKey!),
    enabled: !!groupIdKey,
    staleTime: 1 * 60 * 1000, // 1 minute - requests should be fresh
  });
}

/**
 * Submit a membership request to join a group
 */
export function useSubmitMembershipRequest(groupIdKey: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: SubmitMembershipRequestRequest) =>
      membershipRequestsApi.submitMembershipRequest(groupIdKey, request),
    onSuccess: () => {
      // Invalidate group details and requests
      queryClient.invalidateQueries({ queryKey: ['groups', groupIdKey] });
      queryClient.invalidateQueries({ queryKey: ['groups', groupIdKey, 'membership-requests'] });
    },
  });
}

/**
 * Process (approve or deny) a membership request
 */
export function useProcessRequest(groupIdKey: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ requestIdKey, request }: {
      requestIdKey: string;
      request: ProcessMembershipRequestRequest
    }) => membershipRequestsApi.processRequest(groupIdKey, requestIdKey, request),
    onSuccess: () => {
      // Invalidate requests list and group members
      queryClient.invalidateQueries({ queryKey: ['groups', groupIdKey, 'membership-requests'] });
      queryClient.invalidateQueries({ queryKey: ['groups', groupIdKey, 'members'] });
      queryClient.invalidateQueries({ queryKey: ['groups', groupIdKey] });
    },
  });
}
