/**
 * Group Domain Types
 *
 * TypeScript interfaces matching C# DTOs from:
 * - src/Koinon.Application/DTOs/GroupDto.cs
 * - src/Koinon.Application/DTOs/GroupTypeDto.cs
 * - src/Koinon.Application/DTOs/GroupMemberDetailDto.cs
 *
 * Note on date/time fields:
 * - C# DateTime -> TypeScript string (ISO 8601: "2024-01-15T10:30:00Z")
 *
 * Note on naming:
 * - All types use Dto suffix to align with C# naming conventions
 * - camelCase for TypeScript (matches JSON serialization from C#)
 */

import type { IdKey, DateTime, Guid } from '@/services/api/types';
import type { CampusSummaryDto } from './profile';
import type { PersonSummaryDto } from './person';

// ============================================================================
// Group Type Role
// ============================================================================

/**
 * Group type role DTO.
 * Represents a role within a group type (e.g., Leader, Member).
 * Matches C# GroupTypeRoleDto.
 */
export interface GroupTypeRoleDto {
  idKey: IdKey;
  name: string;
  isLeader: boolean;
}

// ============================================================================
// Group Type Types
// ============================================================================

/**
 * Summary of a group type used in group DTOs.
 * Matches C# GroupTypeSummaryDto.
 */
export interface GroupTypeSummaryDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  /** @deprecated Families are now separate from group types */
  isFamilyGroupType?: boolean;
  allowMultipleLocations: boolean;
  roles: GroupTypeRoleDto[];
}

/**
 * Group type DTO for list views.
 * Matches C# GroupTypeDto.
 */
export interface GroupTypeDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  iconCssClass?: string;
  color?: string;
  groupTerm: string;
  groupMemberTerm: string;
  takesAttendance: boolean;
  allowSelfRegistration: boolean;
  requiresMemberApproval: boolean;
  defaultIsPublic: boolean;
  defaultGroupCapacity?: number;
  isSystem: boolean;
  isArchived: boolean;
  order: number;
  groupCount: number;
}

/**
 * Detailed group type DTO with all configuration fields.
 * Matches C# GroupTypeDetailDto.
 */
export interface GroupTypeDetailDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  iconCssClass?: string;
  color?: string;
  groupTerm: string;
  groupMemberTerm: string;
  takesAttendance: boolean;
  allowSelfRegistration: boolean;
  requiresMemberApproval: boolean;
  defaultIsPublic: boolean;
  defaultGroupCapacity?: number;
  showInGroupList: boolean;
  showInNavigation: boolean;
  attendanceCountsAsWeekendService: boolean;
  sendAttendanceReminder: boolean;
  allowMultipleLocations: boolean;
  enableSpecificGroupRequirements: boolean;
  allowGroupSync: boolean;
  allowSpecificGroupMemberAttributes: boolean;
  showConnectionStatus: boolean;
  ignorePersonInactivated: boolean;
  isSystem: boolean;
  isArchived: boolean;
  order: number;
  groupCount: number;
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

// ============================================================================
// Group Summary Types
// ============================================================================

/**
 * Summary group DTO for lists and references.
 * Matches C# GroupSummaryDto.
 */
export interface GroupSummaryDto {
  idKey: IdKey;
  name: string;
  description?: string;
  isActive: boolean;
  isArchived?: boolean;
  memberCount: number;
  groupTypeName: string;
}

// ============================================================================
// Group Detail Types
// ============================================================================

/**
 * Full group details DTO.
 * Matches C# GroupDto.
 */
export interface GroupDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  isActive: boolean;
  isArchived: boolean;
  isSecurityRole: boolean;
  isPublic: boolean;
  allowGuests: boolean;
  groupCapacity?: number;
  order: number;
  groupType: GroupTypeSummaryDto;
  campus?: CampusSummaryDto;
  parentGroup?: GroupSummaryDto;
  members: GroupMemberDto[];
  childGroups: GroupSummaryDto[];
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
  archivedDateTime?: DateTime;
}

// ============================================================================
// Group Member Types
// ============================================================================

/**
 * Group member DTO representing a person's membership in a group.
 * Matches C# GroupMemberDto.
 */
export interface GroupMemberDto {
  idKey: IdKey;
  person: PersonSummaryDto;
  role: GroupTypeRoleDto;
  status: string;
  dateTimeAdded?: DateTime;
  inactiveDateTime?: DateTime;
  note?: string;
}

/**
 * Enhanced group member DTO with contact information.
 * Contact details (email, phone) are only populated for group leaders.
 * Matches C# GroupMemberDetailDto.
 */
export interface GroupMemberDetailDto {
  idKey: IdKey;
  personIdKey: IdKey;
  firstName: string;
  lastName: string;
  fullName: string;
  email?: string;
  phone?: string;
  photoUrl?: string;
  age?: number;
  gender: string;
  role: GroupTypeRoleDto;
  status: string;
  dateTimeAdded?: DateTime;
  inactiveDateTime?: DateTime;
  note?: string;
}
