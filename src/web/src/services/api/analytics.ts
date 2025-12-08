/**
 * Analytics API service
 */

import { get } from './client';

export interface AttendanceAnalytics {
  totalAttendance: number;
  uniqueAttendees: number;
  firstTimeVisitors: number;
  returningVisitors: number;
  averageAttendance: number;
  startDate: string;
  endDate: string;
}

export interface AttendanceTrend {
  date: string;
  count: number;
  firstTime: number;
  returning: number;
}

export interface AttendanceByGroup {
  groupIdKey: string;
  groupName: string;
  groupTypeName: string;
  totalAttendance: number;
  uniqueAttendees: number;
}

export interface AttendanceAnalyticsParams {
  startDate: string;
  endDate: string;
  campusIdKey?: string;
  groupTypeIdKey?: string;
}

export async function getAttendanceAnalytics(
  params: AttendanceAnalyticsParams
): Promise<AttendanceAnalytics> {
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

  const response = await get<{ data: AttendanceAnalytics }>(
    `/analytics/attendance?${queryParams.toString()}`
  );
  return response.data;
}

export async function getAttendanceTrends(
  params: AttendanceAnalyticsParams
): Promise<AttendanceTrend[]> {
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

  const response = await get<{ data: AttendanceTrend[] }>(
    `/analytics/attendance/trends?${queryParams.toString()}`
  );
  return response.data;
}

export async function getAttendanceByGroup(
  params: AttendanceAnalyticsParams
): Promise<AttendanceByGroup[]> {
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

  const response = await get<{ data: AttendanceByGroup[] }>(
    `/analytics/attendance/by-group?${queryParams.toString()}`
  );
  return response.data;
}
