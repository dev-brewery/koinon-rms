/**
 * Analytics domain types
 *
 * Types for attendance analytics and visitor tracking.
 * Aligned with C# DTOs in src/Koinon.Application/DTOs/
 *
 * Note: TypeScript uses camelCase for properties while C# uses PascalCase.
 * JSON serialization handles the casing transformation automatically.
 * Date fields use ISO 8601 string format for C# DateOnly/DateTime types.
 */

/**
 * Aggregated attendance statistics for a date range
 */
export interface AttendanceAnalyticsDto {
  totalAttendance: number;
  uniqueAttendees: number;
  firstTimeVisitors: number;
  returningVisitors: number;
  averageAttendance: number;
  startDate: string;
  endDate: string;
}

/**
 * Daily attendance trend data point
 */
export interface AttendanceTrendDto {
  date: string;
  count: number;
  firstTime: number;
  returning: number;
}

/**
 * Attendance breakdown by group
 */
export interface AttendanceByGroupDto {
  groupIdKey: string;
  groupName: string;
  groupTypeName: string;
  totalAttendance: number;
  uniqueAttendees: number;
}

/**
 * First-time visitor record with contact information
 */
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

/**
 * Parameters for querying attendance analytics
 */
export interface AttendanceAnalyticsParams {
  startDate: string;
  endDate: string;
  campusIdKey?: string;
  groupTypeIdKey?: string;
}
