/**
 * Defined Types / Defined Values API service
 */

import { get, post, put, del } from '@/services/api/client';
import type { ApiResponse } from '@/services/api/types';
import type { IdKey } from '@/services/api/types';

// ============================================================================
// Types
// ============================================================================

export interface DefinedTypeSummary {
  idKey: IdKey;
  name: string;
  category: string;
  isSystem: boolean;
  order: number;
  valueCount: number;
}

export interface DefinedValueDto {
  idKey: IdKey;
  definedTypeIdKey: IdKey;
  value: string;
  description: string | null;
  order: number;
  isActive: boolean;
}

export interface DefinedTypeDto {
  idKey: IdKey;
  name: string;
  description: string | null;
  category: string;
  helpText: string | null;
  isSystem: boolean;
  order: number;
  definedValues: DefinedValueDto[];
}

export interface CreateDefinedValueRequest {
  value: string;
  description?: string;
  order: number;
}

export interface UpdateDefinedValueRequest {
  value: string;
  description?: string;
  order: number;
  isActive: boolean;
}

export interface ReorderItem {
  idKey: IdKey;
  order: number;
}

export interface ReorderDefinedValuesRequest {
  items: ReorderItem[];
}

// ============================================================================
// API Functions
// ============================================================================

/**
 * Get all defined types (summary list)
 */
export async function getDefinedTypes(): Promise<DefinedTypeSummary[]> {
  const response = await get<ApiResponse<DefinedTypeSummary[]>>('/defined-types');
  return response.data;
}

/**
 * Get a single defined type with its values
 */
export async function getDefinedType(idKey: IdKey): Promise<DefinedTypeDto> {
  const response = await get<ApiResponse<DefinedTypeDto>>(`/defined-types/${idKey}`);
  return response.data;
}

/**
 * Create a new value for a defined type
 */
export async function createDefinedValue(
  typeIdKey: IdKey,
  request: CreateDefinedValueRequest
): Promise<DefinedValueDto> {
  const response = await post<ApiResponse<DefinedValueDto>>(
    `/defined-types/${typeIdKey}/values`,
    request
  );
  return response.data;
}

/**
 * Update an existing defined value
 */
export async function updateDefinedValue(
  valueIdKey: IdKey,
  request: UpdateDefinedValueRequest
): Promise<DefinedValueDto> {
  const response = await put<ApiResponse<DefinedValueDto>>(
    `/defined-types/values/${valueIdKey}`,
    request
  );
  return response.data;
}

/**
 * Delete (soft-deactivate) a defined value
 */
export async function deleteDefinedValue(valueIdKey: IdKey): Promise<void> {
  await del(`/defined-types/values/${valueIdKey}`);
}

/**
 * Reorder the values of a defined type
 */
export async function reorderDefinedValues(
  typeIdKey: IdKey,
  request: ReorderDefinedValuesRequest
): Promise<void> {
  await post(`/defined-types/${typeIdKey}/values/reorder`, request);
}
