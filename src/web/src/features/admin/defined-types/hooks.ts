/**
 * Defined Types / Defined Values hooks using TanStack Query
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import * as definedTypesApi from './api';
import type {
  CreateDefinedValueRequest,
  UpdateDefinedValueRequest,
  ReorderDefinedValuesRequest,
} from './api';
import type { IdKey } from '@/services/api/types';

// ============================================================================
// Query Key Factory
// ============================================================================

const definedTypeKeys = {
  all: ['defined-types'] as const,
  lists: () => [...definedTypeKeys.all, 'list'] as const,
  list: () => [...definedTypeKeys.lists()] as const,
  details: () => [...definedTypeKeys.all, 'detail'] as const,
  detail: (idKey: IdKey) => [...definedTypeKeys.details(), idKey] as const,
};

// ============================================================================
// Query Hooks
// ============================================================================

/**
 * Get all defined types (summary list)
 */
export function useDefinedTypes() {
  return useQuery({
    queryKey: definedTypeKeys.list(),
    queryFn: () => definedTypesApi.getDefinedTypes(),
    staleTime: 60 * 1000, // 1 minute
  });
}

/**
 * Get a single defined type with its values
 */
export function useDefinedType(idKey: IdKey) {
  return useQuery({
    queryKey: definedTypeKeys.detail(idKey),
    queryFn: () => definedTypesApi.getDefinedType(idKey),
    staleTime: 30 * 1000, // 30 seconds
  });
}

// ============================================================================
// Mutation Hooks
// ============================================================================

/**
 * Create a new value for a defined type
 */
export function useCreateDefinedValue() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      typeIdKey,
      request,
    }: {
      typeIdKey: IdKey;
      request: CreateDefinedValueRequest;
    }) => definedTypesApi.createDefinedValue(typeIdKey, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: definedTypeKeys.detail(variables.typeIdKey),
      });
    },
  });
}

/**
 * Update an existing defined value
 */
export function useUpdateDefinedValue() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      valueIdKey,
      typeIdKey: _typeIdKey,
      request,
    }: {
      valueIdKey: IdKey;
      typeIdKey: IdKey;
      request: UpdateDefinedValueRequest;
    }) => definedTypesApi.updateDefinedValue(valueIdKey, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: definedTypeKeys.detail(variables.typeIdKey),
      });
    },
  });
}

/**
 * Delete (soft-deactivate) a defined value
 */
export function useDeleteDefinedValue() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      valueIdKey,
      // typeIdKey needed for cache invalidation
    }: {
      valueIdKey: IdKey;
      typeIdKey: IdKey;
    }) => definedTypesApi.deleteDefinedValue(valueIdKey),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: definedTypeKeys.detail(variables.typeIdKey),
      });
    },
  });
}

/**
 * Reorder values within a defined type
 */
export function useReorderDefinedValues() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      typeIdKey,
      request,
    }: {
      typeIdKey: IdKey;
      request: ReorderDefinedValuesRequest;
    }) => definedTypesApi.reorderDefinedValues(typeIdKey, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: definedTypeKeys.detail(variables.typeIdKey),
      });
    },
  });
}
