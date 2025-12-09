/**
 * React Query hooks for Group Types Admin
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as groupTypesApi from '@/services/api/groupTypes';
import type { CreateGroupTypeRequest, UpdateGroupTypeRequest } from '@/services/api/types';

/**
 * Get all group types
 */
export function useGroupTypes(includeArchived = false) {
  return useQuery({
    queryKey: ['group-types', { includeArchived }],
    queryFn: () => groupTypesApi.getGroupTypes(includeArchived),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get single group type details
 */
export function useGroupType(idKey?: string) {
  return useQuery({
    queryKey: ['group-types', idKey],
    queryFn: () => groupTypesApi.getGroupType(idKey!),
    enabled: !!idKey,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

/**
 * Create a new group type
 */
export function useCreateGroupType() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateGroupTypeRequest) => groupTypesApi.createGroupType(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['group-types'] });
    },
  });
}

/**
 * Update an existing group type
 */
export function useUpdateGroupType() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ idKey, request }: { idKey: string; request: UpdateGroupTypeRequest }) =>
      groupTypesApi.updateGroupType(idKey, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['group-types'] });
    },
  });
}

/**
 * Archive a group type
 */
export function useArchiveGroupType() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (idKey: string) => groupTypesApi.archiveGroupType(idKey),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['group-types'] });
    },
  });
}

/**
 * Get groups for a specific group type
 */
export function useGroupsByType(idKey?: string) {
  return useQuery({
    queryKey: ['group-types', idKey, 'groups'],
    queryFn: () => groupTypesApi.getGroupsByType(idKey!),
    enabled: !!idKey,
  });
}
