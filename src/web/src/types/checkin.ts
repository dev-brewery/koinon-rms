/**
 * Core Check-in Domain Types
 *
 * TypeScript interfaces matching C# DTOs from:
 * - src/Koinon.Application/DTOs/CheckinConfigurationDto.cs
 *
 * Note on date/time fields:
 * - C# DateTime -> TypeScript string (ISO 8601: "2024-01-15T10:30:00Z")
 * - C# DateOnly -> TypeScript string (ISO 8601: "2024-01-15")
 * - C# TimeSpan -> TypeScript string (e.g., "09:00:00")
 * - C# DayOfWeek enum -> TypeScript number (0=Sunday, 6=Saturday)
 *
 * Note on naming:
 * - All types use Dto suffix to align with C# naming conventions
 * - camelCase for TypeScript (matches JSON serialization from C#)
 */

import type { IdKey, DateTime, DateOnly, Guid } from '@/services/api/types';

// ============================================================================
// Capacity Status Enum
// ============================================================================

/**
 * Capacity status for locations and areas.
 * Matches C# CapacityStatus enum values.
 */
export enum CapacityStatusDto {
  /** Below soft capacity threshold */
  Available = 0,
  /** At or above soft capacity threshold */
  Warning = 1,
  /** At or above hard capacity threshold */
  Full = 2,
}

// ============================================================================
// Campus Summary
// ============================================================================

/**
 * Campus summary for check-in configuration.
 * Matches C# CampusSummaryDto (from GroupTypeSummaryDto.cs or inline).
 */
export interface CheckinCampusSummaryDto {
  idKey: IdKey;
  name: string;
  shortCode?: string;
}

// ============================================================================
// Group Type Summary
// ============================================================================

/**
 * Group type summary with roles for check-in.
 * Matches C# GroupTypeSummaryDto.
 */
export interface CheckinGroupTypeSummaryDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  isFamilyGroupType: boolean;
  allowMultipleLocations: boolean;
  roles: CheckinGroupTypeRoleDto[];
}

/**
 * Group type role definition.
 * Matches C# GroupTypeRoleDto.
 */
export interface CheckinGroupTypeRoleDto {
  idKey: IdKey;
  name: string;
  isLeader: boolean;
}

// ============================================================================
// Schedule DTO
// ============================================================================

/**
 * Schedule information for check-in.
 * Matches C# ScheduleDto from CheckinConfigurationDto.cs.
 */
export interface CheckinScheduleDto {
  /** IdKey of the schedule */
  idKey: IdKey;
  /** Globally unique identifier */
  guid: Guid;
  /** Name of the schedule */
  name: string;
  /** Optional description */
  description?: string;
  /** Day of week (0=Sunday, 6=Saturday) for simple weekly schedules */
  weeklyDayOfWeek?: number;
  /** Time of day (e.g., "09:00:00") for simple weekly schedules */
  weeklyTimeOfDay?: string;
  /** Minutes before scheduled time when check-in opens */
  checkInStartOffsetMinutes?: number;
  /** Minutes after scheduled time when check-in closes */
  checkInEndOffsetMinutes?: number;
  /** Whether this schedule is currently active */
  isActive: boolean;
  /** Whether check-in is currently open for this schedule */
  isCheckinActive: boolean;
  /** Date and time when check-in opens */
  checkinStartTime?: DateTime;
  /** Date and time when check-in closes */
  checkinEndTime?: DateTime;
  /** Whether this schedule is visible in public calendars */
  isPublic: boolean;
  /** Display order for sorting schedules */
  order: number;
  /** The date when this schedule becomes effective */
  effectiveStartDate?: DateOnly;
  /** The date when this schedule is no longer effective */
  effectiveEndDate?: DateOnly;
  /** iCalendar content string (RRULE) for complex recurrence patterns */
  iCalendarContent?: string;
  /** Whether this schedule should be automatically deactivated when complete */
  autoInactivateWhenComplete: boolean;
  /** Date and time when this schedule was created */
  createdDateTime: DateTime;
  /** Date and time when this schedule was last modified */
  modifiedDateTime?: DateTime;
}

// ============================================================================
// Check-in Location DTO
// ============================================================================

/**
 * Represents a location within a check-in area.
 * Matches C# CheckinLocationDto.
 */
export interface CheckinLocationDto {
  /** IdKey of the location */
  idKey: IdKey;
  /** Name of the location */
  name: string;
  /** Full path of location (Building > Floor > Room) */
  fullPath: string;
  /** Soft capacity threshold (warning level) */
  softCapacity?: number;
  /** Hard capacity threshold (cannot exceed) */
  hardCapacity?: number;
  /** Current attendance count */
  currentCount: number;
  /** Current capacity status */
  capacityStatus: CapacityStatusDto;
  /** Whether this location is active */
  isActive: boolean;
  /** IdKey of the printer device for this location */
  printerDeviceIdKey?: string;
  /** Percentage of soft capacity used (0-100+) */
  percentageFull: number;
  /** Overflow location IdKey when this room is full */
  overflowLocationIdKey?: string;
  /** Overflow location name */
  overflowLocationName?: string;
  /** Whether overflow assignment should be automatic */
  autoAssignOverflow: boolean;
}

// ============================================================================
// Check-in Area DTO
// ============================================================================

/**
 * Represents a check-in area (special group type for children's ministry, volunteers, etc.).
 * Matches C# CheckinAreaDto.
 */
export interface CheckinAreaDto {
  /** IdKey of the group representing this check-in area */
  idKey: IdKey;
  /** Globally unique identifier */
  guid: Guid;
  /** Name of the check-in area */
  name: string;
  /** Optional description */
  description?: string;
  /** Group type information */
  groupType: CheckinGroupTypeSummaryDto;
  /** Available locations within this check-in area */
  locations: CheckinLocationDto[];
  /** Schedule for when this area is open */
  schedule?: CheckinScheduleDto;
  /** Whether this area is currently active */
  isActive: boolean;
  /** Current capacity status */
  capacityStatus: CapacityStatusDto;
  /** Minimum age in months for eligibility in this area (null = no restriction) */
  minAgeMonths?: number;
  /** Maximum age in months for eligibility in this area (null = no restriction) */
  maxAgeMonths?: number;
  /** Minimum grade for eligibility (-1 = Pre-K, 0 = K, 1+ = grades). Null = no restriction */
  minGrade?: number;
  /** Maximum grade for eligibility (-1 = Pre-K, 0 = K, 1+ = grades). Null = no restriction */
  maxGrade?: number;
}

// ============================================================================
// Check-in Configuration DTO
// ============================================================================

/**
 * Check-in configuration for a kiosk or campus.
 * Contains all settings needed to run check-in operations.
 * Matches C# CheckinConfigurationDto.
 */
export interface CheckinConfigurationDto {
  /** Campus information for this check-in configuration */
  campus: CheckinCampusSummaryDto;
  /** Available check-in areas (groups) at this location */
  areas: CheckinAreaDto[];
  /** Active schedules for today */
  activeSchedules: CheckinScheduleDto[];
  /** Current server time (for clock synchronization) */
  serverTime: DateTime;
}

// ============================================================================
// Check-in Request/Result Types
// ============================================================================

/**
 * Request to check in a person to a location.
 */
export interface CheckinRequestDto {
  personIdKey: IdKey;
  locationIdKey: IdKey;
  scheduleIdKey?: IdKey;
  occurrenceDate?: DateOnly;
  deviceIdKey?: IdKey;
  generateSecurityCode: boolean;
  note?: string;
}

/**
 * Request to check in multiple people.
 */
export interface BatchCheckinRequestDto {
  checkIns: CheckinRequestDto[];
  deviceIdKey?: IdKey;
}

/**
 * Result of a single check-in operation.
 */
export interface CheckinResultDto {
  success: boolean;
  errorMessage?: string;
  attendanceIdKey?: IdKey;
  securityCode?: string;
  checkInTime?: DateTime;
  person?: CheckinPersonSummaryDto;
  location?: CheckinLocationSummaryDto;
}

/**
 * Result of batch check-in operation.
 */
export interface BatchCheckinResultDto {
  results: CheckinResultDto[];
  successCount: number;
  failureCount: number;
  allSucceeded: boolean;
}

/**
 * Minimal person info for check-in results.
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
 * Minimal location info for check-in results.
 */
export interface CheckinLocationSummaryDto {
  idKey: IdKey;
  name: string;
  fullPath: string;
}

/**
 * Validation result for check-in eligibility.
 */
export interface CheckinValidationResultDto {
  isAllowed: boolean;
  reason?: string;
  isAlreadyCheckedIn: boolean;
  isAtCapacity: boolean;
  isOutsideSchedule: boolean;
}
