/**
 * Public groups hooks using TanStack Query
 */

import { useQuery } from '@tanstack/react-query';
import { searchPublicGroups, PublicGroupSearchParams } from '@/services/api/publicGroups';

/**
 * Search for public groups with filters
 */
export function usePublicGroups(params: PublicGroupSearchParams = {}) {
  return useQuery({
    queryKey: ['publicGroups', params],
    queryFn: () => searchPublicGroups(params),
    staleTime: 30000, // 30 seconds - public data changes less frequently
  });
}
