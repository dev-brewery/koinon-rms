import { useQuery } from '@tanstack/react-query';
import * as checkinApi from '@/services/api/checkin';
import type {
  AttendanceResultDto,
  RosterChildDto,
  RoomRosterDto,
  CheckinAreaDto
} from '@/services/api/types';

/**
 * Maps RosterChildDto to AttendanceResultDto for supervisor mode display.
 * The roster data contains the same information but with different field names.
 *
 * NOTE: This mapper is intentionally kept local to this hook. If similar mapping
 * logic is needed elsewhere, extract to src/utils/mappers/attendanceMapper.ts
 */
function mapRosterChildToAttendance(
  child: RosterChildDto,
  locationName: string
): AttendanceResultDto {
  return {
    attendanceIdKey: child.attendanceIdKey,
    personIdKey: child.personIdKey,
    personName: child.fullName,
    // Group name is not available in roster data - use location as context
    groupName: '',
    locationName,
    // Schedule name is not available in roster data
    scheduleName: '',
    securityCode: child.securityCode ?? '',
    checkInTime: child.checkInTime,
    isFirstTime: child.isFirstTime,
  };
}

/**
 * Aggregates multiple room rosters into a single attendance list.
 */
function aggregateRostersToAttendance(rosters: RoomRosterDto[]): AttendanceResultDto[] {
  const attendances: AttendanceResultDto[] = [];

  for (const roster of rosters) {
    if (!roster.children || roster.children.length === 0) {
      continue;
    }

    for (const child of roster.children) {
      attendances.push(mapRosterChildToAttendance(child, roster.locationName));
    }
  }

  // Sort by check-in time, most recent first
  return attendances.sort(
    (a, b) => new Date(b.checkInTime).getTime() - new Date(a.checkInTime).getTime()
  );
}

/**
 * Hook to fetch current attendance for supervisor mode.
 * Fetches rosters for all provided locations and aggregates them.
 *
 * @param locationIdKeys - Array of location IdKeys to fetch attendance for
 * @param enabled - Whether the query should be enabled (typically when supervisor mode is active)
 */
export function useSupervisorAttendance(
  locationIdKeys: string[] | undefined,
  enabled: boolean = true
) {
  const isEnabled = enabled && !!locationIdKeys && locationIdKeys.length > 0;

  return useQuery({
    queryKey: ['supervisor', 'attendance', locationIdKeys],
    queryFn: async () => {
      if (!locationIdKeys || locationIdKeys.length === 0) {
        return [];
      }

      try {
        const rosters = await checkinApi.getMultipleRoomRosters(locationIdKeys);
        return aggregateRostersToAttendance(rosters);
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Unknown error';
        if (import.meta.env.DEV) {
          console.error('Failed to fetch supervisor attendance:', errorMessage, error);
        }
        // Re-throw to let React Query handle error state
        throw new Error(`Failed to fetch supervisor attendance: ${errorMessage}`);
      }
    },
    enabled: isEnabled,
    staleTime: 15 * 1000, // 15 seconds - shorter stale time for real-time display
    refetchInterval: isEnabled ? 30 * 1000 : false, // Auto-refresh every 30 seconds when active
  });
}

/**
 * Extracts all unique location IdKeys from the check-in configuration.
 * Used to determine which locations to fetch attendance for.
 */
export function extractLocationIdKeys(
  areas: CheckinAreaDto[] | undefined
): string[] {
  if (!areas) return [];

  const locationIdKeys = new Set<string>();

  for (const area of areas) {
    for (const group of area.groups) {
      for (const location of group.locations) {
        locationIdKeys.add(location.idKey);
      }
    }
  }

  return Array.from(locationIdKeys);
}
