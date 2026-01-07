/**
 * React Query hooks for Defined Types
 */

import { useQuery } from '@tanstack/react-query';
import * as referenceApi from '@/services/api/reference';

/**
 * Get all defined types
 */
export function useDefinedTypes() {
  return useQuery({
    queryKey: ['defined-types'],
    queryFn: () => referenceApi.getDefinedTypes(),
    staleTime: 10 * 60 * 1000, // 10 minutes - reference data rarely changes
  });
}

/**
 * Get values for a specific defined type by IdKey or GUID
 */
export function useDefinedTypeValues(idKeyOrGuid?: string) {
  return useQuery({
    queryKey: ['defined-types', idKeyOrGuid, 'values'],
    queryFn: () => referenceApi.getDefinedTypeValues(idKeyOrGuid ?? ''),
    enabled: !!idKeyOrGuid,
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
}
