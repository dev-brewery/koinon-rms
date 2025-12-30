/**
 * Room Capacity TypeScript Types
 * Maps C# DTOs from Room Capacity domain
 */

import type { CapacityStatus } from '@/services/api/types';

// ============================================================================
// Capacity Types
// ============================================================================

/**
 * Detailed capacity information for a room/location
 * Maps to RoomCapacityDto in C#
 */
export interface RoomCapacityDto {
  /** IdKey of the location */
  idKey: string;
  /** Name of the location */
  name: string;
  /** Soft capacity threshold (warning level) */
  softCapacity?: number;
  /** Hard capacity limit (cannot exceed without override) */
  hardCapacity?: number;
  /** Current attendance count */
  currentCount: number;
  /** Current capacity status */
  capacityStatus: CapacityStatus;
  /** Percentage of soft capacity used (0-100+) */
  percentageFull: number;
  /** Staff-to-child ratio requirement (e.g., 1 staff per 5 children = 5) */
  staffToChildRatio?: number;
  /** Current number of staff checked in */
  currentStaffCount: number;
  /** Number of staff required based on current attendance and ratio */
  requiredStaffCount?: number;
  /** Indicates whether the location meets staff ratio requirements */
  meetsStaffRatio: boolean;
  /** Overflow location IdKey */
  overflowLocationIdKey?: string;
  /** Overflow location name */
  overflowLocationName?: string;
  /** Indicates whether overflow assignment should be automatic */
  autoAssignOverflow: boolean;
  /** Indicates whether this location is active */
  isActive: boolean;
}

// ============================================================================
// Request Types
// ============================================================================

/**
 * Request to update capacity settings for a location
 * Maps to UpdateCapacitySettingsDto in C#
 */
export interface UpdateCapacitySettingsDto {
  /** Soft capacity threshold (warning level) */
  softCapacity?: number;
  /** Hard capacity limit (cannot exceed without override) */
  hardCapacity?: number;
  /** Staff-to-child ratio requirement */
  staffToChildRatio?: number;
  /** Overflow location IdKey */
  overflowLocationIdKey?: string;
  /** Indicates whether overflow assignment should be automatic */
  autoAssignOverflow: boolean;
}

/**
 * Request to override capacity for a specific check-in
 * Maps to CapacityOverrideRequestDto in C#
 */
export interface CapacityOverrideRequestDto {
  /** Location IdKey to override capacity for */
  locationIdKey: string;
  /** Supervisor PIN for authorization */
  supervisorPin: string;
  /** Reason for the override */
  reason: string;
}
