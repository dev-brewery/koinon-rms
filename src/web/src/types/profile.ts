/**
 * Profile-related TypeScript types
 */

import type { IdKey, DateTime, DateOnly } from '@/services/api/types';
import type { CampusSummaryDto } from './campus';

// Re-export shared types for convenience
export type { CampusSummaryDto };

// ============================================================================
// Phone Number Types
// ============================================================================

export interface PhoneNumberDto {
  idKey: IdKey;
  number: string;
  numberFormatted: string;
  extension?: string;
  phoneType?: {
    idKey: IdKey;
    value: string;
  };
  isMessagingEnabled: boolean;
  isUnlisted: boolean;
}

export interface PhoneNumberRequestDto {
  idKey?: IdKey;  // IdKey of existing phone (null/undefined for new)
  number: string;
  extension?: string;
  phoneTypeIdKey?: IdKey;
  isMessagingEnabled?: boolean;
  isUnlisted?: boolean;
}

// ============================================================================
// Profile Types
// ============================================================================

export interface FamilySummaryDto {
  idKey: IdKey;
  name: string;
  memberCount: number;
}

export interface MyProfileDto {
  idKey: IdKey;
  guid: string;
  firstName: string;
  middleName?: string;
  nickName?: string;
  lastName: string;
  fullName: string;
  email?: string;
  isEmailActive: boolean;
  emailPreference: string;
  phoneNumbers: PhoneNumberDto[];
  birthDate?: DateOnly;
  age?: number;
  gender: string;
  photoUrl?: string;
  primaryFamily?: FamilySummaryDto;
  primaryCampus?: CampusSummaryDto;
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

export interface UpdateMyProfileRequest {
  nickName?: string;
  email?: string;
  emailPreference?: string;
  phoneNumbers?: PhoneNumberRequestDto[];
}

// ============================================================================
// Family Member Types
// ============================================================================

export interface FamilyMemberDto {
  idKey: IdKey;
  firstName: string;
  nickName?: string;
  lastName: string;
  fullName: string;
  birthDate?: DateOnly;
  age?: number;
  gender: string;
  email?: string;
  phoneNumbers: PhoneNumberDto[];
  photoUrl?: string;
  familyRole: string; // "Adult" | "Child"
  canEdit: boolean;
  allergies?: string;
  hasCriticalAllergies: boolean;
  specialNeeds?: string;
}

export interface UpdatePhoneNumberRequest {
  idKey?: IdKey;  // IdKey of existing phone (null/undefined for new)
  number: string;
  extension?: string;
  phoneTypeIdKey?: IdKey;
  isMessagingEnabled: boolean;
  isUnlisted: boolean;
}

export interface UpdateFamilyMemberRequest {
  nickName?: string;
  phoneNumbers?: UpdatePhoneNumberRequest[];
  allergies?: string;
  hasCriticalAllergies?: boolean;
  specialNeeds?: string;
}

// ============================================================================
// Involvement Types
// ============================================================================

export interface GroupMembershipDto {
  idKey: IdKey;
  groupName: string;
  description?: string;
  groupTypeName: string;
  role: string;
  isLeader: boolean;
  joinedDate: DateTime;
  lastAttendanceDate?: DateTime;
  campus?: CampusSummaryDto;
}

export interface MyInvolvementDto {
  groups: GroupMembershipDto[];
  recentAttendanceCount: number;
  totalGroupsCount: number;
}
