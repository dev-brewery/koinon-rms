/**
 * Person Merge/Deduplication Types
 *
 * TypeScript interfaces for person merge operations
 * Matches backend DTOs from person merge feature
 */

import type { IdKey, DateTime } from '@/services/api/types';
import type { PersonDto } from './person';

/**
 * Duplicate match entry showing two potentially duplicate people
 */
export interface DuplicateMatchDto {
  person1IdKey: IdKey;
  person1Name: string;
  person1Email?: string;
  person1Phone?: string;
  person1PhotoUrl?: string;

  person2IdKey: IdKey;
  person2Name: string;
  person2Email?: string;
  person2Phone?: string;
  person2PhotoUrl?: string;

  matchScore: number;
  matchReasons: string[];
}

/**
 * Detailed comparison of two people for merge preview
 * Matches C# PersonComparisonDto
 */
export interface PersonComparisonDto {
  person1: PersonDto;
  person2: PersonDto;
  person1AttendanceCount: number;
  person2AttendanceCount: number;
  person1GroupMembershipCount: number;
  person2GroupMembershipCount: number;
  person1ContributionTotal: number;
  person2ContributionTotal: number;
}

/**
 * Request to merge two people
 */
export interface PersonMergeRequestDto {
  survivorIdKey: IdKey;
  mergedIdKey: IdKey;
  fieldSelections: Record<string, 'survivor' | 'merged'>;
  notes?: string;
}

/**
 * Result of merge operation
 * Matches C# PersonMergeResultDto
 */
export interface PersonMergeResultDto {
  survivorIdKey: IdKey;
  mergedIdKey: IdKey;
  aliasesUpdated: number;
  groupMembershipsUpdated: number;
  familyMembershipsUpdated: number;
  phoneNumbersUpdated: number;
  authorizedPickupsUpdated: number;
  communicationPreferencesUpdated: number;
  refreshTokensUpdated: number;
  securityRolesUpdated: number;
  supervisorSessionsUpdated: number;
  followUpsUpdated: number;
  totalRecordsUpdated: number;
  mergedDateTime: DateTime;
}

/**
 * Historical record of completed merge
 * Matches C# PersonMergeHistoryDto
 */
export interface PersonMergeHistoryDto {
  idKey: IdKey;
  survivorIdKey: IdKey;
  survivorName: string;
  mergedIdKey: IdKey;
  mergedName: string;
  mergedByIdKey?: IdKey;
  mergedByName?: string;
  mergedDateTime: DateTime;
  notes?: string;
}

/**
 * Request to mark two people as not duplicates
 */
export interface IgnoreDuplicateRequestDto {
  person1IdKey: IdKey;
  person2IdKey: IdKey;
  reason?: string;
}
