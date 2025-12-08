import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as checkinApi from '@/services/api/checkin';

/**
 * Get room roster for a single location
 * Auto-refreshes every 30 seconds to show real-time check-ins/outs
 */
export function useRoomRoster(locationIdKey?: string, autoRefresh: boolean = true) {
  return useQuery({
    queryKey: ['checkin', 'roster', locationIdKey],
    queryFn: () => checkinApi.getRoomRoster(locationIdKey!),
    enabled: !!locationIdKey,
    staleTime: 30 * 1000, // 30 seconds
    refetchInterval: autoRefresh ? 30 * 1000 : false, // Auto-refresh every 30 seconds when enabled
  });
}

/**
 * Get rosters for multiple locations at once
 * Used by supervisors to view all room rosters
 */
export function useMultipleRoomRosters(locationIdKeys?: string[]) {
  return useQuery({
    queryKey: ['checkin', 'roster', 'multiple', locationIdKeys],
    queryFn: () => checkinApi.getMultipleRoomRosters(locationIdKeys!),
    enabled: !!locationIdKeys && locationIdKeys.length > 0,
    staleTime: 30 * 1000, // 30 seconds
    refetchInterval: 30 * 1000, // Auto-refresh every 30 seconds
  });
}

/**
 * Check out a child from the roster
 * Invalidates roster queries on success to refresh the view
 */
export function useCheckOutFromRoster() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (attendanceIdKey: string) => {
      await checkinApi.checkoutFromRoster(attendanceIdKey);
    },
    onSuccess: () => {
      // Invalidate all roster queries to refresh
      queryClient.invalidateQueries({ queryKey: ['checkin', 'roster'] });
    },
  });
}
