/**
 * Analytics API service
 */

import { get } from './client';
import type {
  AttendanceAnalyticsDto,
  AttendanceAnalyticsParams,
  AttendanceByGroupDto,
  AttendanceTrendDto,
  FirstTimeVisitorDto,
} from '@/types';

// Re-export types for backwards compatibility
export type {
  AttendanceAnalyticsDto,
  AttendanceAnalyticsParams,
  AttendanceByGroupDto,
  AttendanceTrendDto,
  FirstTimeVisitorDto,
};

export async function getAttendanceAnalytics(
  params: AttendanceAnalyticsParams
): Promise<AttendanceAnalyticsDto> {
  const queryParams = new URLSearchParams({
    startDate: params.startDate,
    endDate: params.endDate,
  });

  if (params.campusIdKey) {
    queryParams.append('campusIdKey', params.campusIdKey);
  }

  if (params.groupTypeIdKey) {
    queryParams.append('groupTypeIdKey', params.groupTypeIdKey);
  }

  const response = await get<{ data: AttendanceAnalyticsDto }>(
    `/analytics/attendance?${queryParams.toString()}`
  );
  if (!response.data) {
    throw new Error('Invalid response structure: missing data field');
  }
  return response.data;
}

export async function getAttendanceTrends(
  params: AttendanceAnalyticsParams
): Promise<AttendanceTrendDto[]> {
  const queryParams = new URLSearchParams({
    startDate: params.startDate,
    endDate: params.endDate,
  });

  if (params.campusIdKey) {
    queryParams.append('campusIdKey', params.campusIdKey);
  }

  if (params.groupTypeIdKey) {
    queryParams.append('groupTypeIdKey', params.groupTypeIdKey);
  }

  const response = await get<{ data: AttendanceTrendDto[] }>(
    `/analytics/attendance/trends?${queryParams.toString()}`
  );
  if (!response.data) {
    throw new Error('Invalid response structure: missing data field');
  }
  return response.data;
}

export async function getAttendanceByGroup(
  params: AttendanceAnalyticsParams
): Promise<AttendanceByGroupDto[]> {
  const queryParams = new URLSearchParams({
    startDate: params.startDate,
    endDate: params.endDate,
  });

  if (params.campusIdKey) {
    queryParams.append('campusIdKey', params.campusIdKey);
  }

  if (params.groupTypeIdKey) {
    queryParams.append('groupTypeIdKey', params.groupTypeIdKey);
  }

  const response = await get<{ data: AttendanceByGroupDto[] }>(
    `/analytics/attendance/by-group?${queryParams.toString()}`
  );
  if (!response.data) {
    throw new Error('Invalid response structure: missing data field');
  }
  return response.data;
}

export async function getTodaysFirstTimeVisitors(
  campusIdKey?: string
): Promise<FirstTimeVisitorDto[]> {
  const queryParams = new URLSearchParams();

  if (campusIdKey) {
    queryParams.append('campusIdKey', campusIdKey);
  }

  const queryString = queryParams.toString();
  const url = queryString
    ? `/analytics/first-time-visitors/today?${queryString}`
    : '/analytics/first-time-visitors/today';

  const response = await get<{ data: FirstTimeVisitorDto[] }>(url);
  if (!response.data) {
    throw new Error('Invalid response structure: missing data field');
  }
  return response.data;
}

export async function getFirstTimeVisitorsByDateRange(
  startDate: string,
  endDate: string,
  campusIdKey?: string
): Promise<FirstTimeVisitorDto[]> {
  const queryParams = new URLSearchParams({
    startDate,
    endDate,
  });

  if (campusIdKey) {
    queryParams.append('campusIdKey', campusIdKey);
  }

  const response = await get<{ data: FirstTimeVisitorDto[] }>(
    `/analytics/first-time-visitors?${queryParams.toString()}`
  );
  if (!response.data) {
    throw new Error('Invalid response structure: missing data field');
  }
  return response.data;
}
