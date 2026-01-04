/**
 * useImportJobs hook
 * Fetches and filters import job history using TanStack Query
 */

import { useQuery } from '@tanstack/react-query';
import { useState, useMemo } from 'react';
import { getImportJobs } from '@/services/api/import';

export function useImportJobs() {
  const [typeFilter, setTypeFilter] = useState<string>('all');

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['import-jobs'],
    queryFn: () => getImportJobs(),
    staleTime: 30000, // 30 seconds
  });

  const filteredJobs = useMemo(() => {
    if (!data) return [];
    if (typeFilter === 'all') return data;
    return data.filter((job) => job.importType === typeFilter);
  }, [data, typeFilter]);

  return {
    jobs: filteredJobs,
    allJobs: data || [],
    isLoading,
    error,
    typeFilter,
    setTypeFilter,
    refetch,
  };
}
