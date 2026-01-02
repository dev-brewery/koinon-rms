/**
 * Communication Templates management hooks using TanStack Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as communicationTemplatesApi from '@/services/api/communicationTemplates';
import type {
  CreateCommunicationTemplateRequest,
  UpdateCommunicationTemplateRequest,
  CommunicationTemplatesParams,
} from '@/services/api/communicationTemplates';

/**
 * List communication templates with filters
 */
export function useCommunicationTemplates(params: CommunicationTemplatesParams = {}) {
  return useQuery({
    queryKey: ['communication-templates', params],
    queryFn: () => communicationTemplatesApi.getCommunicationTemplates(params),
    staleTime: 5 * 60 * 1000, // 5 minutes - templates don't change often
  });
}

/**
 * Get a single communication template by IdKey
 */
export function useCommunicationTemplate(idKey?: string) {
  return useQuery({
    queryKey: ['communication-templates', idKey],
    queryFn: () => communicationTemplatesApi.getCommunicationTemplate(idKey!),
    enabled: !!idKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Create a new communication template
 */
export function useCreateCommunicationTemplate() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateCommunicationTemplateRequest) =>
      communicationTemplatesApi.createCommunicationTemplate(request),
    onSuccess: () => {
      // Invalidate templates list to refetch
      queryClient.invalidateQueries({ queryKey: ['communication-templates'] });
    },
    onError: () => {
      // Invalidate on error to ensure consistent state
      queryClient.invalidateQueries({ queryKey: ['communication-templates'] });
    },
  });
}

/**
 * Update an existing communication template
 */
export function useUpdateCommunicationTemplate() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ idKey, request }: { idKey: string; request: UpdateCommunicationTemplateRequest }) =>
      communicationTemplatesApi.updateCommunicationTemplate(idKey, request),
    onSuccess: (_, variables) => {
      // Invalidate specific template and templates list
      queryClient.invalidateQueries({ queryKey: ['communication-templates', variables.idKey] });
      queryClient.invalidateQueries({ queryKey: ['communication-templates'] });
    },
    onError: () => {
      // Invalidate on error to ensure consistent state
      queryClient.invalidateQueries({ queryKey: ['communication-templates'] });
    },
  });
}

/**
 * Delete a communication template
 */
export function useDeleteCommunicationTemplate() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (idKey: string) => communicationTemplatesApi.deleteCommunicationTemplate(idKey),
    onSuccess: () => {
      // Invalidate templates list to refetch
      queryClient.invalidateQueries({ queryKey: ['communication-templates'] });
    },
    onError: () => {
      // Invalidate on error to ensure consistent state
      queryClient.invalidateQueries({ queryKey: ['communication-templates'] });
    },
  });
}
