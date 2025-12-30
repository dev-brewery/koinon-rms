/**
 * Extended TypeScript types for Koinon RMS Check-in API
 * Provides types for attendance analytics, roster management, and extended check-in operations
 */

import type { IdKey, DateTime, DateOnly, Guid, CapacityStatus } from '@/services/api/types';

// ============================================================================
// Attendance Analytics Types
// ============================================================================

/**
 * Aggregate attendance statistics for a date range
 */
export interface AttendanceAnalyticsDto {
  totalAttendance: number;
  uniqueAttendees: number;
  firstTimeVisitors: number;
  returningVisitors: number;
  averageAttendance: number;
  startDate: DateOnly;
  endDate: DateOnly;
}

/**
 * Daily attendance trend data point
 */
export interface AttendanceTrendDto {
  date: DateOnly;
  count: number;
  firstTime: number;
  returning: number;
}

/**
 * Attendance statistics grouped by group
 */
export interface AttendanceByGroupDto {
  groupIdKey: IdKey;
  groupName: string;
  groupTypeName: string;
  totalAttendance: number;
  uniqueAttendees: number;
}

// ============================================================================
// Attendance Taker / Roster Types
// ============================================================================

/**
 * Result of marking a single person's attendance
 */
export interface MarkAttendanceResultDto {
  success: boolean;
  errorMessage?: string;
  attendanceIdKey?: IdKey;
  isFirstTime: boolean;
  presentDateTime?: DateTime;
}

/**
 * Result of marking attendance for multiple people
 */
export interface BulkMarkAttendanceResultDto {
  results: MarkAttendanceResultDto[];
  successCount: number;
  failureCount: number;
  allSucceeded: boolean;
}

/**
 * A person's roster entry for a specific occurrence
 */
export interface OccurrenceRosterEntryDto {
  personIdKey: IdKey;
  fullName: string;
  firstName: string;
  lastName: string;
  nickName?: string;
  age?: number;
  photoUrl?: string;
  isAttending: boolean;
  attendanceIdKey?: IdKey;
  presentDateTime?: DateTime;
  isFirstTime: boolean;
  note?: string;
}

/**
 * Family grouping for roster display
 */
export interface FamilyRosterGroupDto {
  familyIdKey: IdKey;
  familyName: string;
  members: OccurrenceRosterEntryDto[];
  attendingCount: number;
  totalCount: number;
}

// ============================================================================
// Extended Check-in Search Types
// ============================================================================

/**
 * Family search result with recent check-in context
 */
export interface CheckinFamilySearchResultDto {
  familyIdKey: IdKey;
  familyName: string;
  addressSummary?: string;
  campusName?: string;
  members: CheckinFamilyMemberDto[];
  recentCheckInCount: number;
}

/**
 * Family member details for check-in context
 */
export interface CheckinFamilyMemberDto {
  personIdKey: IdKey;
  fullName: string;
  firstName: string;
  lastName: string;
  nickName?: string;
  age?: number;
  gender: string;
  photoUrl?: string;
  roleName: string;
  isChild: boolean;
  hasRecentCheckIn: boolean;
  lastCheckIn?: DateTime;
  grade?: string;
  allergies?: string;
  hasCriticalAllergies: boolean;
  specialNeeds?: string;
}

// ============================================================================
// Extended Check-in Request/Result Types
// ============================================================================

/**
 * Request to check in a person to a location
 */
export interface ExtendedCheckinRequestDto {
  personIdKey: IdKey;
  locationIdKey: IdKey;
  scheduleIdKey?: IdKey;
  occurrenceDate?: DateOnly;
  deviceIdKey?: IdKey;
  generateSecurityCode: boolean;
  note?: string;
}

/**
 * Request to check in multiple people
 */
export interface BatchCheckinRequestDto {
  checkIns: ExtendedCheckinRequestDto[];
  deviceIdKey?: IdKey;
}

/**
 * Result of a single check-in operation
 */
export interface ExtendedCheckinResultDto {
  success: boolean;
  errorMessage?: string;
  attendanceIdKey?: IdKey;
  securityCode?: string;
  checkInTime?: DateTime;
  person?: CheckinPersonSummaryDto;
  location?: CheckinLocationSummaryDto;
}

/**
 * Result of batch check-in operation
 */
export interface BatchCheckinResultDto {
  results: ExtendedCheckinResultDto[];
  successCount: number;
  failureCount: number;
  allSucceeded: boolean;
}

/**
 * Summary of an attendance record
 */
export interface AttendanceSummaryDto {
  idKey: IdKey;
  person: CheckinPersonSummaryDto;
  location: CheckinLocationSummaryDto;
  startDateTime: DateTime;
  endDateTime?: DateTime;
  securityCode?: string;
  isFirstTime: boolean;
  note?: string;
}

/**
 * Minimal person info for check-in results
 */
export interface CheckinPersonSummaryDto {
  idKey: IdKey;
  fullName: string;
  firstName: string;
  lastName: string;
  nickName?: string;
  age?: number;
  photoUrl?: string;
}

/**
 * Minimal location info for check-in results
 */
export interface CheckinLocationSummaryDto {
  idKey: IdKey;
  name: string;
  fullPath: string;
}

/**
 * Validation result for check-in eligibility
 */
export interface CheckinValidationResult {
  isAllowed: boolean;
  reason?: string;
  isAlreadyCheckedIn: boolean;
  isAtCapacity: boolean;
  isOutsideSchedule: boolean;
}

// ============================================================================
// Extended Configuration Types
// ============================================================================

/**
 * Extended check-in configuration with full detail
 */
export interface ExtendedCheckinConfigurationDto {
  campus: ExtendedCampusSummaryDto;
  areas: ExtendedCheckinAreaDto[];
  activeSchedules: ExtendedScheduleDto[];
  serverTime: DateTime;
}

/**
 * Campus summary for extended check-in
 */
export interface ExtendedCampusSummaryDto {
  idKey: IdKey;
  name: string;
  shortCode?: string;
}

/**
 * Check-in area with full configuration
 */
export interface ExtendedCheckinAreaDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  groupType: ExtendedGroupTypeSummaryDto;
  locations: ExtendedCheckinLocationDto[];
  schedule?: ExtendedScheduleDto;
  isActive: boolean;
  capacityStatus: CapacityStatus;
  minAgeMonths?: number;
  maxAgeMonths?: number;
  minGrade?: number;
  maxGrade?: number;
}

/**
 * Check-in location with capacity and overflow
 */
export interface ExtendedCheckinLocationDto {
  idKey: IdKey;
  name: string;
  fullPath: string;
  softCapacity?: number;
  hardCapacity?: number;
  currentCount: number;
  capacityStatus: CapacityStatus;
  isActive: boolean;
  printerDeviceIdKey?: string;
  percentageFull: number;
  overflowLocationIdKey?: string;
  overflowLocationName?: string;
  autoAssignOverflow: boolean;
}

/**
 * Schedule with full check-in window details
 */
export interface ExtendedScheduleDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  weeklyDayOfWeek?: number;
  weeklyTimeOfDay?: string;
  checkInStartOffsetMinutes?: number;
  checkInEndOffsetMinutes?: number;
  isActive: boolean;
  isCheckinActive: boolean;
  checkinStartTime?: DateTime;
  checkinEndTime?: DateTime;
  isPublic: boolean;
  order: number;
  effectiveStartDate?: DateOnly;
  effectiveEndDate?: DateOnly;
  iCalendarContent?: string;
  autoInactivateWhenComplete: boolean;
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

/**
 * Group type summary with roles
 */
export interface ExtendedGroupTypeSummaryDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  isFamilyGroupType: boolean;
  allowMultipleLocations: boolean;
  roles: GroupTypeRoleDto[];
}

/**
 * Group type role definition
 */
export interface GroupTypeRoleDto {
  idKey: IdKey;
  name: string;
  isLeader: boolean;
}
