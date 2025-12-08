/**
 * Families management hooks using TanStack Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as familiesApi from '@/services/api/families';
import type {
  FamiliesSearchParams,
  CreateFamilyRequest,
  UpdateFamilyRequest,
  AddFamilyMemberRequest,
  RemoveFamilyMemberParams,
} from '@/services/api/types';

/**
 * Search for families with filters
 */
export function useFamilies(params: FamiliesSearchParams = {}) {
  return useQuery({
    queryKey: ['families', params],
    queryFn: () => familiesApi.searchFamilies(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get a single family by IdKey
 */
export function useFamily(idKey?: string) {
  return useQuery({
    queryKey: ['families', idKey],
    queryFn: () => familiesApi.getFamilyByIdKey(idKey!),
    enabled: !!idKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Create a new family
 */
export function useCreateFamily() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateFamilyRequest) => familiesApi.createFamily(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['families'] });
    },
  });
}

/**
 * Update an existing family
 */
export function useUpdateFamily() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ idKey, request }: { idKey: string; request: UpdateFamilyRequest }) =>
      familiesApi.updateFamily(idKey, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['families', variables.idKey] });
      queryClient.invalidateQueries({ queryKey: ['families'] });
    },
  });
}

/**
 * Add a member to a family
 */
export function useAddFamilyMember() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ familyIdKey, request }: { familyIdKey: string; request: AddFamilyMemberRequest }) =>
      familiesApi.addFamilyMember(familyIdKey, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['families', variables.familyIdKey] });
      queryClient.invalidateQueries({ queryKey: ['families'] });
      queryClient.invalidateQueries({ queryKey: ['people'] });
    },
  });
}

/**
 * Remove a member from a family
 */
export function useRemoveFamilyMember() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      familyIdKey,
      personIdKey,
      params
    }: {
      familyIdKey: string;
      personIdKey: string;
      params?: RemoveFamilyMemberParams;
    }) =>
      familiesApi.removeFamilyMember(familyIdKey, personIdKey, params),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['families', variables.familyIdKey] });
      queryClient.invalidateQueries({ queryKey: ['families'] });
      queryClient.invalidateQueries({ queryKey: ['people'] });
    },
  });
}
