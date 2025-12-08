/**
 * Pager hooks using TanStack Query
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import * as pagerApi from './api';
import type { SendPageRequest } from './api';

/**
 * Query key factory for pagers
 */
const pagerKeys = {
  all: ['pagers'] as const,
  search: (searchTerm?: string, date?: string) =>
    [...pagerKeys.all, 'search', { searchTerm, date }] as const,
  history: (pagerNumber: number, date?: string) =>
    [...pagerKeys.all, 'history', pagerNumber, { date }] as const,
};

/**
 * Search for pager assignments
 */
export function usePagerSearch(searchTerm?: string, date?: string) {
  return useQuery({
    queryKey: pagerKeys.search(searchTerm, date),
    queryFn: () => pagerApi.searchPagers(searchTerm, date),
    staleTime: 10 * 1000, // 10 seconds - search results change frequently
    enabled: true, // Always enabled - will show all pagers when searchTerm is undefined
  });
}

/**
 * Get page history for a specific pager
 */
export function usePageHistory(pagerNumber: number | null, date?: string) {
  return useQuery({
    queryKey: pagerKeys.history(pagerNumber ?? 0, date),
    queryFn: () => pagerApi.getPageHistory(pagerNumber!, date),
    staleTime: 30 * 1000, // 30 seconds
    enabled: pagerNumber !== null, // Only fetch when pager number is provided
  });
}

/**
 * Send a page to a parent
 */
export function useSendPage() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: SendPageRequest) => pagerApi.sendPage(request),
    onSuccess: () => {
      // Invalidate all pager queries to refetch updated counts
      queryClient.invalidateQueries({
        queryKey: pagerKeys.all,
      });
    },
  });
}
