/**
 * Giving management hooks using TanStack Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as givingApi from '@/services/api/giving';
import { getCampuses } from '@/services/api/reference';
import type { BatchFilterParams } from '@/services/api/giving';
import type { AddContributionRequest } from '@/types/giving';

/**
 * Get financial batches with filters
 */
export function useBatches(params: BatchFilterParams = {}) {
  return useQuery({
    queryKey: ['batches', params],
    queryFn: () => givingApi.getBatches(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Open a financial batch
 */
export function useOpenBatch() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (idKey: string) => givingApi.openBatch(idKey),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['batches'] });
    },
  });
}

/**
 * Close a financial batch
 */
export function useCloseBatch() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (idKey: string) => givingApi.closeBatch(idKey),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['batches'] });
    },
  });
}

/**
 * Get a single financial batch by IdKey
 */
export function useBatch(idKey?: string) {
  return useQuery({
    queryKey: ['batches', idKey],
    queryFn: () => givingApi.getBatch(idKey!),
    enabled: !!idKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get batch summary with totals
 */
export function useBatchSummary(idKey?: string) {
  return useQuery({
    queryKey: ['batches', idKey, 'summary'],
    queryFn: () => givingApi.getBatchSummary(idKey!),
    enabled: !!idKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get contributions for a batch
 */
export function useBatchContributions(batchIdKey?: string) {
  return useQuery({
    queryKey: ['batches', batchIdKey, 'contributions'],
    queryFn: () => givingApi.getBatchContributions(batchIdKey!),
    enabled: !!batchIdKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get active funds for contribution entry
 */
export function useActiveFunds() {
  return useQuery({
    queryKey: ['funds', 'active'],
    queryFn: () => givingApi.getActiveFunds(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Add contribution to batch
 */
export function useAddContribution() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ batchIdKey, request }: { batchIdKey: string; request: AddContributionRequest }) =>
      givingApi.addContribution(batchIdKey, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['batches', variables.batchIdKey, 'contributions'] });
      queryClient.invalidateQueries({ queryKey: ['batches', variables.batchIdKey, 'summary'] });
    },
  });
}

/**
 * Update existing contribution
 */
export function useUpdateContribution() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ idKey, data }: { idKey: string; data: Parameters<typeof givingApi.updateContribution>[1] }) =>
      givingApi.updateContribution(idKey, data),
    onSuccess: (data) => {
      if (data?.batchIdKey) {
        queryClient.invalidateQueries({ queryKey: ['batches', data.batchIdKey, 'contributions'] });
        queryClient.invalidateQueries({ queryKey: ['batches', data.batchIdKey, 'summary'] });
      }
      queryClient.invalidateQueries({ queryKey: ['contributions'] });
    },
  });
}

/**
 * Delete contribution from batch
 */
export function useDeleteContribution() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ idKey }: { idKey: string; batchIdKey: string }) =>
      givingApi.deleteContribution(idKey),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['batches', variables.batchIdKey, 'contributions'] });
      queryClient.invalidateQueries({ queryKey: ['batches', variables.batchIdKey, 'summary'] });
      queryClient.invalidateQueries({ queryKey: ['contributions'] });
    },
  });
}

/**
 * Get campuses for filter dropdown
 */
export function useCampuses() {
  return useQuery({
    queryKey: ['campuses'],
    queryFn: () => getCampuses(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}
