/**
 * Dashboard hooks using TanStack Query
 */

import { useQuery } from '@tanstack/react-query';
import * as dashboardApi from '@/services/api/dashboard';

export function useDashboardStats() {
  return useQuery({
    queryKey: ['dashboard', 'stats'],
    queryFn: () => dashboardApi.getDashboardStats(),
    staleTime: 60 * 1000, // 1 minute
    refetchInterval: 60 * 1000, // Auto refresh every minute
  });
}
