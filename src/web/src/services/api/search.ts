/**
 * Global search API service
 */

import { get } from './client';
import type { GlobalSearchResponse, SearchParams } from '@/types/search';

/**
 * Perform a global search across People, Families, and Groups
 */
export async function globalSearch(params: SearchParams): Promise<GlobalSearchResponse> {
  const queryParams = new URLSearchParams();

  // Query is required
  queryParams.set('q', params.query);

  // Optional filters
  if (params.category) {
    queryParams.set('category', params.category);
  }
  if (params.pageNumber !== undefined) {
    queryParams.set('pageNumber', String(params.pageNumber));
  }
  if (params.pageSize !== undefined) {
    queryParams.set('pageSize', String(params.pageSize));
  }

  const endpoint = `/search?${queryParams.toString()}`;
  return get<GlobalSearchResponse>(endpoint);
}
