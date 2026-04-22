/**
 * React Query hooks for the live check-in operations dashboard (#482).
 *
 * Polls the dashboard endpoint every 5 seconds via React Query's
 * `refetchInterval`. No WebSockets/SignalR — defer to v1.0.
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  getCheckinOperationsDashboard,
  toggleCheckinOperationsRoom,
} from '@/services/api/checkinOperations';

export const CHECKIN_OPS_QUERY_KEY = ['checkin-operations', 'dashboard'] as const;
export const CHECKIN_OPS_POLL_INTERVAL_MS = 5000;

/**
 * Live dashboard query. Re-fetches every 5 seconds while mounted and the tab
 * is visible. Callers should render from `data` and render a loading skeleton
 * only on the initial load.
 */
export function useCheckinOperationsDashboard(enabled = true) {
  return useQuery({
    queryKey: CHECKIN_OPS_QUERY_KEY,
    queryFn: getCheckinOperationsDashboard,
    enabled,
    refetchInterval: CHECKIN_OPS_POLL_INTERVAL_MS,
    refetchIntervalInBackground: false,
    staleTime: 0,
  });
}

/**
 * Toggle a room's open/closed state. On success, invalidates the dashboard
 * query so the next poll (or immediate refetch) reflects the change.
 */
export function useToggleCheckinOperationsRoom() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (locationIdKey: string) => toggleCheckinOperationsRoom(locationIdKey),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CHECKIN_OPS_QUERY_KEY });
    },
  });
}
