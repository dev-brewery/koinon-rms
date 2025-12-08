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

export interface FirstTimeVisitorDto {
  personIdKey: string;
  personName: string;
  email?: string;
  phoneNumber?: string;
  checkInDateTime: string;
  groupName: string;
  groupTypeName: string;
  campusName?: string;
  hasFollowUp: boolean;
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
  if (!response.data) {
    throw new Error('Invalid response structure: missing data field');
  }
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
  if (!response.data) {
    throw new Error('Invalid response structure: missing data field');
  }
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
