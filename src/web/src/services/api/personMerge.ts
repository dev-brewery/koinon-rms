/**
 * Person Merge API Service
 * API functions for person merge and deduplication operations
 */

import { get, post, del } from './client';
import type { PagedResult } from './types';
import type {
  DuplicateMatchDto,
  PersonComparisonDto,
  PersonMergeRequestDto,
  PersonMergeResultDto,
  PersonMergeHistoryDto,
  IgnoreDuplicateRequestDto,
} from '@/types/personMerge';

/**
 * Get list of potential duplicate people
 */
export async function getDuplicates(
  page: number = 1,
  pageSize: number = 25
): Promise<PagedResult<DuplicateMatchDto>> {
  const queryParams = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });

  return get<PagedResult<DuplicateMatchDto>>(`/people/duplicates?${queryParams.toString()}`);
}

/**
 * Get potential duplicates for a specific person
 */
export async function getDuplicatesForPerson(idKey: string): Promise<DuplicateMatchDto[]> {
  const response = await get<{ data: DuplicateMatchDto[] }>(`/people/${idKey}/duplicates`);
  return response.data;
}

/**
 * Compare two people for merge preview
 */
export async function comparePeople(
  person1IdKey: string,
  person2IdKey: string
): Promise<PersonComparisonDto> {
  const response = await get<{ data: PersonComparisonDto }>(
    `/people/compare?person1IdKey=${person1IdKey}&person2IdKey=${person2IdKey}`
  );
  return response.data;
}

/**
 * Merge two people
 */
export async function mergePeople(
  request: PersonMergeRequestDto
): Promise<PersonMergeResultDto> {
  const response = await post<{ data: PersonMergeResultDto }>('/people/merge', request);
  return response.data;
}

/**
 * Get merge history
 */
export async function getMergeHistory(
  page: number = 1,
  pageSize: number = 25
): Promise<PagedResult<PersonMergeHistoryDto>> {
  const queryParams = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });

  return get<PagedResult<PersonMergeHistoryDto>>(`/people/merge-history?${queryParams.toString()}`);
}

/**
 * Mark two people as not duplicates (ignore)
 */
export async function ignoreDuplicate(
  request: IgnoreDuplicateRequestDto
): Promise<void> {
  await post<void>('/people/duplicates/ignore', request);
}

/**
 * Remove ignore flag from duplicate pair
 */
export async function unignoreDuplicate(
  person1IdKey: string,
  person2IdKey: string
): Promise<void> {
  await del<void>(`/people/duplicates/ignore/${person1IdKey}/${person2IdKey}`);
}
