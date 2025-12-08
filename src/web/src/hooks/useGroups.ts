/**
 * Groups management hooks using TanStack Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as groupsApi from '@/services/api/groups';
import type {
  GroupsSearchParams,
  CreateGroupRequest,
  UpdateGroupRequest,
  AddGroupScheduleRequest,
} from '@/services/api/types';

/**
 * Search for groups with filters
 */
export function useGroups(params: GroupsSearchParams = {}) {
  return useQuery({
    queryKey: ['groups', params],
    queryFn: () => groupsApi.searchGroups(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get a single group by IdKey
 */
export function useGroup(idKey?: string) {
  return useQuery({
    queryKey: ['groups', idKey],
    queryFn: () => groupsApi.getGroupByIdKey(idKey!),
    enabled: !!idKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get child groups of a parent group
 */
export function useChildGroups(parentIdKey?: string) {
  return useQuery({
    queryKey: ['groups', parentIdKey, 'children'],
    queryFn: () => groupsApi.getChildGroups(parentIdKey!),
    enabled: !!parentIdKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Create a new group
 */
export function useCreateGroup() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateGroupRequest) => groupsApi.createGroup(request),
    onSuccess: () => {
      // Invalidate groups list to refetch
      queryClient.invalidateQueries({ queryKey: ['groups'] });
    },
  });
}

/**
 * Update an existing group
 */
export function useUpdateGroup() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ idKey, request }: { idKey: string; request: UpdateGroupRequest }) =>
      groupsApi.updateGroup(idKey, request),
    onSuccess: (_, variables) => {
      // Invalidate specific group and groups list
      queryClient.invalidateQueries({ queryKey: ['groups', variables.idKey] });
      queryClient.invalidateQueries({ queryKey: ['groups'] });
    },
  });
}

/**
 * Delete (archive) a group
 */
export function useDeleteGroup() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (idKey: string) => groupsApi.deleteGroup(idKey),
    onSuccess: () => {
      // Invalidate groups list to refetch
      queryClient.invalidateQueries({ queryKey: ['groups'] });
    },
  });
}

/**
 * Get schedules for a group
 */
export function useGroupSchedules(groupIdKey?: string) {
  return useQuery({
    queryKey: ['groups', groupIdKey, 'schedules'],
    queryFn: () => groupsApi.getGroupSchedules(groupIdKey!),
    enabled: !!groupIdKey,
    staleTime: 5 * 60 * 1000,
  });
}

/**
 * Add a schedule to a group
 */
export function useAddGroupSchedule() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ groupIdKey, request }: { groupIdKey: string; request: AddGroupScheduleRequest }) =>
      groupsApi.addGroupSchedule(groupIdKey, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['groups', variables.groupIdKey, 'schedules'] });
    },
  });
}

/**
 * Remove a schedule from a group
 */
export function useRemoveGroupSchedule() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ groupIdKey, scheduleIdKey }: { groupIdKey: string; scheduleIdKey: string }) =>
      groupsApi.removeGroupSchedule(groupIdKey, scheduleIdKey),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['groups', variables.groupIdKey, 'schedules'] });
    },
  });
}
