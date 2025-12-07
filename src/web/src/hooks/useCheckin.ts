import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as checkinApi from '@/services/api/checkin';
import type {
  CheckinConfigParams,
  CheckinOpportunitiesParams,
  RecordAttendanceRequest,
  LabelParams,
} from '@/services/api/types';

/**
 * Get check-in configuration for kiosk
 */
export function useCheckinConfiguration(params: CheckinConfigParams = {}) {
  return useQuery({
    queryKey: ['checkin', 'configuration', params],
    queryFn: () => checkinApi.getCheckinConfiguration(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Search for families to check in
 */
export function useCheckinSearch(searchValue?: string, searchType?: 'Phone' | 'Name' | 'Auto') {
  return useQuery({
    queryKey: ['checkin', 'search', searchValue, searchType],
    queryFn: () =>
      checkinApi.searchFamiliesForCheckin({
        searchValue: searchValue!,
        searchType,
      }),
    enabled: !!searchValue && searchValue.length >= 2,
    staleTime: 30 * 1000, // 30 seconds
  });
}

/**
 * Get check-in opportunities for a family
 */
export function useCheckinOpportunities(
  familyIdKey?: string,
  params: CheckinOpportunitiesParams = {}
) {
  return useQuery({
    queryKey: ['checkin', 'opportunities', familyIdKey, params],
    queryFn: () => checkinApi.getCheckinOpportunities(familyIdKey!, params),
    enabled: !!familyIdKey,
    staleTime: 1 * 60 * 1000, // 1 minute
  });
}

/**
 * Record attendance (check-in)
 */
export function useRecordAttendance() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: RecordAttendanceRequest) =>
      checkinApi.recordAttendance(request),
    onSuccess: () => {
      // Invalidate opportunities to refresh current attendance
      queryClient.invalidateQueries({ queryKey: ['checkin', 'opportunities'] });
    },
  });
}

/**
 * Check out a person
 * Note: Error handling is performed at the component level for better UX control
 */
export function useCheckout() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (attendanceIdKey: string) => {
      await checkinApi.checkout(attendanceIdKey);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['checkin', 'opportunities'] });
    },
  });
}

/**
 * Get labels for an attendance record
 */
export function useLabels(attendanceIdKey?: string, params: LabelParams = {}) {
  return useQuery({
    queryKey: ['checkin', 'labels', attendanceIdKey, params],
    queryFn: () => checkinApi.getLabels(attendanceIdKey!, params),
    enabled: !!attendanceIdKey,
  });
}
