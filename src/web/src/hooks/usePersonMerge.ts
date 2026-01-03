/**
 * Person Merge hooks using TanStack Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as personMergeApi from '@/services/api/personMerge';
import type {
  PersonMergeRequestDto,
  IgnoreDuplicateRequestDto,
} from '@/types/personMerge';

/**
 * Get list of potential duplicate people
 */
export function useDuplicates(page: number = 1, pageSize: number = 25) {
  return useQuery({
    queryKey: ['duplicates', page, pageSize],
    queryFn: () => personMergeApi.getDuplicates(page, pageSize),
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

/**
 * Get potential duplicates for a specific person
 */
export function useDuplicatesForPerson(idKey?: string) {
  return useQuery({
    queryKey: ['duplicates', 'person', idKey],
    queryFn: () => personMergeApi.getDuplicatesForPerson(idKey!),
    enabled: !!idKey,
    staleTime: 2 * 60 * 1000,
  });
}

/**
 * Compare two people for merge preview
 */
export function usePersonComparison(person1IdKey?: string, person2IdKey?: string) {
  return useQuery({
    queryKey: ['personComparison', person1IdKey, person2IdKey],
    queryFn: () => personMergeApi.comparePeople(person1IdKey!, person2IdKey!),
    enabled: !!person1IdKey && !!person2IdKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Merge two people
 */
export function useMergePeople() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: PersonMergeRequestDto) => personMergeApi.mergePeople(request),
    onSuccess: () => {
      // Invalidate duplicates and people queries
      queryClient.invalidateQueries({ queryKey: ['duplicates'] });
      queryClient.invalidateQueries({ queryKey: ['people'] });
      queryClient.invalidateQueries({ queryKey: ['mergeHistory'] });
    },
  });
}

/**
 * Get merge history
 */
export function useMergeHistory(page: number = 1, pageSize: number = 25) {
  return useQuery({
    queryKey: ['mergeHistory', page, pageSize],
    queryFn: () => personMergeApi.getMergeHistory(page, pageSize),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Mark two people as not duplicates
 */
export function useIgnoreDuplicate() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: IgnoreDuplicateRequestDto) => personMergeApi.ignoreDuplicate(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['duplicates'] });
    },
  });
}

/**
 * Remove ignore flag from duplicate pair
 */
export function useUnignoreDuplicate() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ person1IdKey, person2IdKey }: { person1IdKey: string; person2IdKey: string }) =>
      personMergeApi.unignoreDuplicate(person1IdKey, person2IdKey),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['duplicates'] });
    },
  });
}
