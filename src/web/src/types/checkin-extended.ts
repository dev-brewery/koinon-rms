/**
 * Extended TypeScript types for Koinon RMS Check-in API
 * Provides types for roster management and extended check-in operations
 *
 * Note: Core check-in configuration types are now in './checkin.ts'
 * Note: Attendance analytics types are now in './analytics.ts'
 */

import type { IdKey, DateTime } from '@/services/api/types';

// Re-export core checkin types for backwards compatibility
export type {
  CapacityStatusDto,
  CheckinCampusSummaryDto,
  CheckinGroupTypeSummaryDto,
  CheckinGroupTypeRoleDto,
  CheckinScheduleDto,
  CheckinLocationDto,
  CheckinAreaDto,
  CheckinConfigurationDto,
  CheckinRequestDto,
  BatchCheckinRequestDto,
  CheckinResultDto,
  BatchCheckinResultDto,
  CheckinPersonSummaryDto,
  CheckinLocationSummaryDto,
  CheckinValidationResultDto,
} from './checkin';

// Re-export analytics types for backwards compatibility
export type {
  AttendanceAnalyticsDto,
  AttendanceTrendDto,
  AttendanceByGroupDto,
} from './analytics';

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
// Extended Check-in Request/Result Types (Legacy Aliases)
// ============================================================================

// Legacy aliases for backwards compatibility
// These match the original names in this file
import type {
  CheckinRequestDto,
  BatchCheckinRequestDto as BatchCheckinRequestDtoCore,
  CheckinResultDto,
  BatchCheckinResultDto as BatchCheckinResultDtoCore,
  CheckinPersonSummaryDto as CheckinPersonSummaryDtoCore,
  CheckinLocationSummaryDto as CheckinLocationSummaryDtoCore,
  CheckinValidationResultDto,
} from './checkin';

/**
 * @deprecated Use CheckinRequestDto from './checkin' instead
 */
export type ExtendedCheckinRequestDto = CheckinRequestDto;

/**
 * @deprecated Use BatchCheckinRequestDto from './checkin' instead
 */
export type ExtendedBatchCheckinRequestDto = BatchCheckinRequestDtoCore;

/**
 * @deprecated Use CheckinResultDto from './checkin' instead
 */
export type ExtendedCheckinResultDto = CheckinResultDto;

/**
 * @deprecated Use BatchCheckinResultDto from './checkin' instead
 */
export type ExtendedBatchCheckinResultDto = BatchCheckinResultDtoCore;

/**
 * Summary of an attendance record
 */
export interface AttendanceSummaryDto {
  idKey: IdKey;
  person: CheckinPersonSummaryDtoCore;
  location: CheckinLocationSummaryDtoCore;
  startDateTime: DateTime;
  endDateTime?: DateTime;
  securityCode?: string;
  isFirstTime: boolean;
  note?: string;
}

/**
 * @deprecated Use CheckinValidationResultDto from './checkin' instead
 */
export type CheckinValidationResult = CheckinValidationResultDto;

// ============================================================================
// Extended Configuration Types (Legacy Aliases)
// ============================================================================

import type {
  CheckinConfigurationDto,
  CheckinCampusSummaryDto,
  CheckinAreaDto as CheckinAreaDtoCore,
  CheckinLocationDto as CheckinLocationDtoCore,
  CheckinScheduleDto,
  CheckinGroupTypeSummaryDto,
} from './checkin';

/**
 * @deprecated Use CheckinConfigurationDto from './checkin' instead
 */
export type ExtendedCheckinConfigurationDto = CheckinConfigurationDto;

/**
 * @deprecated Use CheckinCampusSummaryDto from './checkin' instead
 */
export type ExtendedCampusSummaryDto = CheckinCampusSummaryDto;

/**
 * @deprecated Use CheckinAreaDto from './checkin' instead
 */
export type ExtendedCheckinAreaDto = CheckinAreaDtoCore;

/**
 * @deprecated Use CheckinLocationDto from './checkin' instead
 */
export type ExtendedCheckinLocationDto = CheckinLocationDtoCore;

/**
 * @deprecated Use CheckinScheduleDto from './checkin' instead
 */
export type ExtendedScheduleDto = CheckinScheduleDto;

/**
 * @deprecated Use CheckinGroupTypeSummaryDto from './checkin' instead
 */
export type ExtendedGroupTypeSummaryDto = CheckinGroupTypeSummaryDto;

// Note: GroupTypeRoleDto is now exported from './group'
// For check-in specific role types, use CheckinGroupTypeRoleDto from './checkin'
