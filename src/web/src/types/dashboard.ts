/**
 * Dashboard domain types
 *
 * Types for dashboard statistics and overview data.
 * Aligned with C# DTOs in src/Koinon.Application/DTOs/
 *
 * Note: TypeScript uses camelCase for properties while C# uses PascalCase.
 * JSON serialization handles the casing transformation automatically.
 * Date fields use ISO 8601 string format for C# DateOnly/DateTime types.
 */

/**
 * Upcoming schedule summary for dashboard display
 */
export interface UpcomingScheduleDto {
  idKey: string;
  name: string;
  nextOccurrence: string;
  minutesUntilCheckIn: number;
}

/**
 * Batch summary for dashboard display
 */
export interface BatchSummary {
  idKey: string;
  name: string;
  batchDate: string;
  status: 'Open' | 'Pending' | 'Closed';
  total: number;
}

/**
 * Giving statistics for dashboard
 */
export interface GivingStats {
  monthToDateTotal: number;
  yearToDateTotal: number;
  recentBatches: BatchSummary[];
}

/**
 * Communication summary for dashboard display
 */
export interface CommunicationSummary {
  idKey: string;
  subject: string;
  type: 'Email' | 'SMS' | 'Push';
  status: 'Draft' | 'Pending' | 'Sent' | 'Failed';
  createdDateTime: string;
}

/**
 * Communications statistics for dashboard
 */
export interface CommunicationsStats {
  pendingCount: number;
  sentThisWeekCount: number;
  recentCommunications: CommunicationSummary[];
}

/**
 * Dashboard statistics overview
 */
export interface DashboardStatsDto {
  totalPeople: number;
  totalFamilies: number;
  activeGroups: number;
  todayCheckIns: number;
  lastWeekCheckIns: number;
  activeSchedules: number;
  upcomingSchedules: UpcomingScheduleDto[];
  givingStats?: GivingStats;
  communicationsStats?: CommunicationsStats;
}
