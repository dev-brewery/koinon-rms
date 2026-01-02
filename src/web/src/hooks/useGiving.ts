/**
 * Giving management hooks using TanStack Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as givingApi from '@/services/api/giving';
import { getCampuses } from '@/services/api/reference';
import type { BatchFilterParams } from '@/services/api/giving';

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
 * Get campuses for filter dropdown
 */
export function useCampuses() {
  return useQuery({
    queryKey: ['campuses'],
    queryFn: () => getCampuses(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}
