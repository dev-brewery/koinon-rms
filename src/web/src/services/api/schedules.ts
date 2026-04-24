/**
 * Schedules API service
 */

import { get, post, put, del } from './client';
import type {
  PagedResult,
  ScheduleSearchParams,
  ScheduleSummaryDto,
  ScheduleDetailDto,
  ScheduleOccurrenceDto,
  CreateScheduleRequest,
  UpdateScheduleRequest,
} from './types';

/**
 * Search schedules with optional filters
 */
export async function searchSchedules(
  params: ScheduleSearchParams = {}
): Promise<PagedResult<ScheduleSummaryDto>> {
  const queryParams = new URLSearchParams();

  if (params.query) queryParams.set('query', params.query);
  if (params.dayOfWeek !== undefined) queryParams.set('dayOfWeek', String(params.dayOfWeek));
  if (params.includeInactive) queryParams.set('includeInactive', String(params.includeInactive));
  if (params.page) queryParams.set('page', String(params.page));
  if (params.pageSize) queryParams.set('pageSize', String(params.pageSize));

  const query = queryParams.toString();
  const endpoint = `/schedules${query ? `?${query}` : ''}`;

  // Backend: Ok(new { data = items, meta = {...} }) — matches PagedResult<T> shape exactly.
  return get<PagedResult<ScheduleSummaryDto>>(endpoint);
}

/**
 * Get schedule details by IdKey
 */
export async function getScheduleByIdKey(idKey: string): Promise<ScheduleDetailDto> {
  // Backend: Ok(new { data = schedule }) — unwrap envelope (see #688).
  const response = await get<{ data: ScheduleDetailDto }>(`/schedules/${idKey}`);
  return response.data;
}

/**
 * Get upcoming occurrences for a schedule
 */
export async function getScheduleOccurrences(
  idKey: string,
  startDate?: string,
  count: number = 10
): Promise<ScheduleOccurrenceDto[]> {
  const queryParams = new URLSearchParams();

  if (startDate) queryParams.set('startDate', startDate);
  queryParams.set('count', String(count));

  const query = queryParams.toString();
  const endpoint = `/schedules/${idKey}/occurrences${query ? `?${query}` : ''}`;

  // Backend: Ok(new { data = occurrences }) — unwrap envelope (see #688).
  const response = await get<{ data: ScheduleOccurrenceDto[] }>(endpoint);
  return response.data;
}

/**
 * Create a new schedule
 */
export async function createSchedule(
  request: CreateScheduleRequest
): Promise<ScheduleDetailDto> {
  // Backend: CreatedAtAction(..., schedule) — returns flat (NOT enveloped).
  // Do NOT unwrap, or we'll read .data on the schedule itself (see #688).
  return post<ScheduleDetailDto>('/schedules', request);
}

/**
 * Update an existing schedule
 */
export async function updateSchedule(
  idKey: string,
  request: UpdateScheduleRequest
): Promise<ScheduleDetailDto> {
  // Backend: Ok(new { data = schedule }) — unwrap envelope (see #688).
  const response = await put<{ data: ScheduleDetailDto }>(`/schedules/${idKey}`, request);
  return response.data;
}

/**
 * Delete (deactivate) a schedule
 */
export async function deleteSchedule(idKey: string): Promise<void> {
  await del<void>(`/schedules/${idKey}`);
}
