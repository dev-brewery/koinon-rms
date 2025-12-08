/**
 * Analytics hooks using TanStack Query
 */

import { useQuery } from '@tanstack/react-query';
import * as analyticsApi from '@/services/api/analytics';
import type { AttendanceAnalyticsParams } from '@/services/api/analytics';

export function useAttendanceAnalytics(params: AttendanceAnalyticsParams) {
  return useQuery({
    queryKey: ['analytics', 'attendance', params],
    queryFn: () => analyticsApi.getAttendanceAnalytics(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

export function useAttendanceTrends(params: AttendanceAnalyticsParams) {
  return useQuery({
    queryKey: ['analytics', 'attendance', 'trends', params],
    queryFn: () => analyticsApi.getAttendanceTrends(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

export function useAttendanceByGroup(params: AttendanceAnalyticsParams) {
  return useQuery({
    queryKey: ['analytics', 'attendance', 'by-group', params],
    queryFn: () => analyticsApi.getAttendanceByGroup(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

export function useTodaysFirstTimeVisitors(campusIdKey?: string) {
  return useQuery({
    queryKey: ['analytics', 'first-time-visitors', 'today', campusIdKey],
    queryFn: () => analyticsApi.getTodaysFirstTimeVisitors(campusIdKey),
    staleTime: 2 * 60 * 1000, // 2 minutes (more frequent updates for "today" data)
  });
}

export function useFirstTimeVisitorsByDateRange(
  startDate: string,
  endDate: string,
  campusIdKey?: string
) {
  return useQuery({
    queryKey: ['analytics', 'first-time-visitors', startDate, endDate, campusIdKey],
    queryFn: () =>
      analyticsApi.getFirstTimeVisitorsByDateRange(startDate, endDate, campusIdKey),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}
