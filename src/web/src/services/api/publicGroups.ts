/**
 * Public Groups API service
 * Handles public-facing group finder functionality
 */

import { get } from './client';
import type { PagedResult } from './types';

export interface PublicGroupDto {
  idKey: string;
  name: string;
  publicDescription?: string;
  groupTypeName?: string;
  campusIdKey?: string;
  campusName?: string;
  memberCount: number;
  capacity?: number;
  hasOpenings: boolean;
  meetingDay?: string;
  meetingTime?: string;
  meetingScheduleSummary?: string;
}

export interface PublicGroupSearchParams {
  searchTerm?: string;
  groupTypeIdKey?: string;
  campusIdKey?: string;
  dayOfWeek?: number;
  timeOfDay?: 0 | 1 | 2;
  hasOpenings?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

/**
 * Search for public groups with filters
 */
export async function searchPublicGroups(
  params: PublicGroupSearchParams = {}
): Promise<PagedResult<PublicGroupDto>> {
  const queryParams = new URLSearchParams();

  if (params.searchTerm) queryParams.set('searchTerm', params.searchTerm);
  if (params.groupTypeIdKey) queryParams.set('groupTypeIdKey', params.groupTypeIdKey);
  if (params.campusIdKey) queryParams.set('campusIdKey', params.campusIdKey);
  if (params.dayOfWeek !== undefined) queryParams.set('dayOfWeek', String(params.dayOfWeek));
  if (params.timeOfDay !== undefined) queryParams.set('timeOfDay', String(params.timeOfDay));
  if (params.hasOpenings !== undefined) queryParams.set('hasOpenings', String(params.hasOpenings));
  if (params.pageNumber) queryParams.set('pageNumber', String(params.pageNumber));
  if (params.pageSize) queryParams.set('pageSize', String(params.pageSize));

  const query = queryParams.toString();
  const endpoint = `/groups/public${query ? `?${query}` : ''}`;

  return get<PagedResult<PublicGroupDto>>(endpoint, { skipAuth: true });
}
