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
