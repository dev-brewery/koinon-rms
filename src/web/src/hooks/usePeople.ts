/**
 * People management hooks using TanStack Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as peopleApi from '@/services/api/people';
import type {
  PersonSearchParams,
  CreatePersonRequest,
  UpdatePersonRequest,
} from '@/services/api/types';

/**
 * Search for people with filters
 */
export function usePeople(params: PersonSearchParams = {}) {
  return useQuery({
    queryKey: ['people', params],
    queryFn: () => peopleApi.searchPeople(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get a single person by IdKey
 */
export function usePerson(idKey?: string) {
  return useQuery({
    queryKey: ['people', idKey],
    queryFn: () => peopleApi.getPersonByIdKey(idKey!),
    enabled: !!idKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Create a new person
 */
export function useCreatePerson() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreatePersonRequest) => peopleApi.createPerson(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['people'] });
    },
  });
}

/**
 * Update an existing person
 */
export function useUpdatePerson() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ idKey, request }: { idKey: string; request: UpdatePersonRequest }) =>
      peopleApi.updatePerson(idKey, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['people', variables.idKey] });
      queryClient.invalidateQueries({ queryKey: ['people'] });
    },
  });
}

/**
 * Delete (soft-delete) a person
 */
export function useDeletePerson() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (idKey: string) => peopleApi.deletePerson(idKey),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['people'] });
    },
  });
}

/**
 * Get person's family members
 */
export function usePersonFamily(idKey?: string) {
  return useQuery({
    queryKey: ['people', idKey, 'family'],
    queryFn: () => peopleApi.getPersonFamily(idKey!),
    enabled: !!idKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get groups the person belongs to
 */
export function usePersonGroups(idKey?: string) {
  return useQuery({
    queryKey: ['people', idKey, 'groups'],
    queryFn: () => peopleApi.getPersonGroups(idKey!),
    enabled: !!idKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}
