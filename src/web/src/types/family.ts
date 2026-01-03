/**
 * Family Domain Types
 *
 * TypeScript interfaces matching C# DTOs from:
 * - src/Koinon.Application/DTOs/FamilyDto.cs
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
import type { GroupTypeRoleDto } from './group';

// ============================================================================
// Location/Address Types
// ============================================================================

/**
 * Address DTO for postal addresses.
 * Matches C# AddressDto.
 */
export interface AddressDto {
  idKey: IdKey;
  street1?: string;
  street2?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  country?: string;
  formattedAddress: string;
}

// ============================================================================
// Family Member Types
// ============================================================================

/**
 * Family membership DTO representing a person's membership in a family.
 * Matches C# FamilyMemberDto (for admin views with nested PersonSummaryDto).
 *
 * Note: This differs from the FamilyMemberDto in profile.ts which has
 * flattened person fields for self-service "My Family" pages.
 */
export interface FamilyMembershipDto {
  idKey: IdKey;
  person: PersonSummaryDto;
  role: GroupTypeRoleDto;
  status: string;
  dateTimeAdded?: DateTime;
}

// ============================================================================
// Family Types
// ============================================================================

/**
 * Full family details DTO with members.
 * Matches C# FamilyDto.
 */
export interface FamilyDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  isActive: boolean;
  campus?: CampusSummaryDto;
  address?: AddressDto;
  members: FamilyMembershipDto[];
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}
