/**
 * Communication Templates API service
 */

import { get, post, put, del } from './client';
import type { PagedResult } from './types';
import type {
  CommunicationTemplateDto,
  CommunicationTemplateSummaryDto,
  CreateCommunicationTemplateDto,
  UpdateCommunicationTemplateDto,
  CommunicationTemplatesParams,
} from '@/types/communication';

// Re-export types from central module for backward compatibility
export type {
  CommunicationTemplateDto,
  CommunicationTemplateSummaryDto,
  CreateCommunicationTemplateDto,
  UpdateCommunicationTemplateDto,
  CommunicationTemplatesParams,
};

// Type aliases for API compatibility
export type CreateCommunicationTemplateRequest = CreateCommunicationTemplateDto;
export type UpdateCommunicationTemplateRequest = UpdateCommunicationTemplateDto;

// ============================================================================
// API Functions
// ============================================================================

/**
 * List communication templates with optional filters
 */
export async function getCommunicationTemplates(
  params: CommunicationTemplatesParams = {}
): Promise<PagedResult<CommunicationTemplateSummaryDto>> {
  const queryParams = new URLSearchParams();

  if (params.page) queryParams.set('page', String(params.page));
  if (params.pageSize) queryParams.set('pageSize', String(params.pageSize));
  if (params.type) queryParams.set('type', params.type);
  if (params.isActive !== undefined) queryParams.set('isActive', String(params.isActive));

  const query = queryParams.toString();
  const endpoint = `/communication-templates${query ? `?${query}` : ''}`;

  return get<PagedResult<CommunicationTemplateSummaryDto>>(endpoint);
}

/**
 * Get a single communication template by IdKey
 */
export async function getCommunicationTemplate(idKey: string): Promise<CommunicationTemplateDto> {
  const response = await get<{ data: CommunicationTemplateDto }>(`/communication-templates/${idKey}`);
  return response.data;
}

/**
 * Create a new communication template
 */
export async function createCommunicationTemplate(
  request: CreateCommunicationTemplateRequest
): Promise<CommunicationTemplateDto> {
  const response = await post<{ data: CommunicationTemplateDto }>('/communication-templates', request);
  return response.data;
}

/**
 * Update an existing communication template
 */
export async function updateCommunicationTemplate(
  idKey: string,
  request: UpdateCommunicationTemplateRequest
): Promise<CommunicationTemplateDto> {
  const response = await put<{ data: CommunicationTemplateDto }>(`/communication-templates/${idKey}`, request);
  return response.data;
}

/**
 * Delete a communication template
 */
export async function deleteCommunicationTemplate(idKey: string): Promise<void> {
  await del<void>(`/communication-templates/${idKey}`);
}
