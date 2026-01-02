/**
 * Person Domain Types
 *
 * TypeScript interfaces matching C# DTOs from:
 * - src/Koinon.Application/DTOs/PersonDto.cs
 * - src/Koinon.Application/DTOs/PersonSearchParameters.cs
 *
 * Note on date/time fields:
 * - C# DateTime -> TypeScript string (ISO 8601: "2024-01-15T10:30:00Z")
 * - C# DateOnly -> TypeScript string (ISO 8601: "2024-01-15")
 *
 * Note on naming:
 * - All types use Dto suffix to align with C# naming conventions
 * - camelCase for TypeScript (matches JSON serialization from C#)
 */

import type { IdKey, DateTime, DateOnly, Guid } from '@/services/api/types';

// ============================================================================
// Shared Types (re-exported from profile for domain consistency)
// ============================================================================

// Re-export shared types for backwards compatibility
export type {
  PhoneNumberDto,
  PhoneNumberRequestDto,
  CampusSummaryDto,
  FamilySummaryDto,
} from './profile';

// ============================================================================
// Defined Value Types
// ============================================================================

/**
 * Defined value DTO for reference data.
 * Represents values from configurable lookup tables.
 */
export interface DefinedValueDto {
  idKey: IdKey;
  guid: Guid;
  value: string;
  description?: string;
  isActive: boolean;
  order: number;
}

// ============================================================================
// Person Summary Types
// ============================================================================

/**
 * Summary person DTO for lists and search results.
 * Matches C# PersonSummaryDto.
 */
export interface PersonSummaryDto {
  idKey: IdKey;
  firstName: string;
  nickName?: string;
  lastName: string;
  fullName: string;
  email?: string;
  photoUrl?: string;
  age?: number;
  gender: string;
  connectionStatus?: DefinedValueDto;
  recordStatus?: DefinedValueDto;
}

// ============================================================================
// Person Detail Types
// ============================================================================

/**
 * Full person details DTO.
 * Matches C# PersonDto.
 */
export interface PersonDto {
  idKey: IdKey;
  guid: Guid;
  firstName: string;
  nickName?: string;
  middleName?: string;
  lastName: string;
  fullName: string;
  birthDate?: DateOnly;
  age?: number;
  gender: string;
  email?: string;
  isEmailActive: boolean;
  emailPreference: string;
  phoneNumbers: import('./profile').PhoneNumberDto[];
  recordStatus?: DefinedValueDto;
  connectionStatus?: DefinedValueDto;
  primaryFamily?: import('./profile').FamilySummaryDto;
  primaryCampus?: import('./profile').CampusSummaryDto;
  photoUrl?: string;
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

// ============================================================================
// Person Search Types
// ============================================================================

/**
 * Parameters for searching people.
 * Matches C# PersonSearchParameters.
 */
export interface PersonSearchParams {
  /** Full-text search query (searches first name, last name, nick name, email) */
  query?: string;
  /** Filter by primary campus ID */
  campusId?: IdKey;
  /** Filter by record status ID */
  recordStatusId?: IdKey;
  /** Filter by connection status ID */
  connectionStatusId?: IdKey;
  /** Include inactive records (default: false) */
  includeInactive?: boolean;
  /** Page number (1-based, default: 1) */
  page?: number;
  /** Number of items per page (default: 25, max: 100) */
  pageSize?: number;
}
