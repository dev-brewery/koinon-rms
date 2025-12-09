/**
 * My Groups hooks using TanStack Query
 * For group leaders to manage their groups
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as myGroupsApi from '@/services/api/myGroups';
import type { RecordGroupAttendanceRequest } from '@/services/api/types';

/**
 * Get all groups where current user is a leader
 */
export function useMyGroups() {
  return useQuery({
    queryKey: ['my-groups'],
    queryFn: () => myGroupsApi.getMyGroups(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get members of a specific group
 */
export function useMyGroupMembers(groupIdKey?: string) {
  return useQuery({
    queryKey: ['my-groups', groupIdKey, 'members'],
    queryFn: () => myGroupsApi.getMyGroupMembers(groupIdKey!),
    enabled: !!groupIdKey,
    staleTime: 2 * 60 * 1000, // 2 minutes (fresher for member data)
  });
}

/**
 * Update a group member (role, status, note)
 */
export function useUpdateGroupMember(groupIdKey: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      memberIdKey,
      data,
    }: {
      memberIdKey: string;
      data: {
        roleId?: string;
        status?: string;
        note?: string;
      };
    }) => myGroupsApi.updateGroupMember(groupIdKey, memberIdKey, data),
    onSuccess: () => {
      // Invalidate members list to refetch
      queryClient.invalidateQueries({ queryKey: ['my-groups', groupIdKey, 'members'] });
      queryClient.invalidateQueries({ queryKey: ['my-groups'] });
    },
  });
}

/**
 * Remove a member from a group
 */
export function useRemoveGroupMember(groupIdKey: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (memberIdKey: string) =>
      myGroupsApi.removeGroupMember(groupIdKey, memberIdKey),
    onSuccess: () => {
      // Invalidate members list to refetch
      queryClient.invalidateQueries({ queryKey: ['my-groups', groupIdKey, 'members'] });
      queryClient.invalidateQueries({ queryKey: ['my-groups'] });
    },
  });
}

/**
 * Record attendance for a group meeting
 */
export function useRecordAttendance(groupIdKey: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: RecordGroupAttendanceRequest) =>
      myGroupsApi.recordAttendance(groupIdKey, request),
    onSuccess: () => {
      // Invalidate groups list to update last meeting date
      queryClient.invalidateQueries({ queryKey: ['my-groups'] });
    },
  });
}
