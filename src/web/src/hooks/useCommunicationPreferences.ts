/**
 * Communication preferences hooks using TanStack Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as communicationsApi from '@/services/api/communications';
import type {
  UpdateCommunicationPreferenceDto,
  BulkUpdatePreferencesDto,
} from '@/types/communication';

/**
 * Get communication preferences for a person
 * No record = opted in (default behavior)
 */
export function useCommunicationPreferences(personIdKey?: string) {
  return useQuery({
    queryKey: ['communication-preferences', personIdKey],
    queryFn: () => communicationsApi.getCommunicationPreferences(personIdKey!),
    enabled: !!personIdKey,
    staleTime: 60 * 1000, // 1 minute - preferences don't change often
  });
}

/**
 * Update a single communication preference
 */
export function useUpdateCommunicationPreference() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      personIdKey,
      type,
      request,
    }: {
      personIdKey: string;
      type: 'Email' | 'Sms';
      request: UpdateCommunicationPreferenceDto;
    }) => communicationsApi.updateCommunicationPreference(personIdKey, type, request),
    onSuccess: (_, { personIdKey }) => {
      // Invalidate preferences for this person
      queryClient.invalidateQueries({
        queryKey: ['communication-preferences', personIdKey],
      });
    },
  });
}

/**
 * Bulk update multiple communication preferences
 */
export function useBulkUpdateCommunicationPreferences() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      personIdKey,
      request,
    }: {
      personIdKey: string;
      request: BulkUpdatePreferencesDto;
    }) => communicationsApi.bulkUpdateCommunicationPreferences(personIdKey, request),
    onSuccess: (_, { personIdKey }) => {
      // Invalidate preferences for this person
      queryClient.invalidateQueries({
        queryKey: ['communication-preferences', personIdKey],
      });
    },
  });
}
