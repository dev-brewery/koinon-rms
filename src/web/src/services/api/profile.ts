/**
 * Profile API service
 */

import { get, put } from './client';
import type {
  MyProfileDto,
  UpdateMyProfileRequest,
  FamilyMemberDto,
  UpdateFamilyMemberRequest,
  MyInvolvementDto,
} from '@/types/profile';

// ============================================================================
// API Functions
// ============================================================================

/**
 * Get the current user's profile
 */
export async function getMyProfile(): Promise<MyProfileDto> {
  const response = await get<{ data: MyProfileDto }>('/api/v1/my-profile');
  return response.data;
}

/**
 * Update the current user's profile
 */
export async function updateMyProfile(
  data: UpdateMyProfileRequest
): Promise<MyProfileDto> {
  const response = await put<{ data: MyProfileDto }>('/api/v1/my-profile', data);
  return response.data;
}

/**
 * Get the current user's family members
 */
export async function getMyFamily(): Promise<FamilyMemberDto[]> {
  const response = await get<{ data: FamilyMemberDto[] }>('/api/v1/my-family');
  return response.data;
}

/**
 * Update a family member's editable fields
 */
export async function updateFamilyMember(
  personIdKey: string,
  data: UpdateFamilyMemberRequest
): Promise<FamilyMemberDto> {
  const response = await put<{ data: FamilyMemberDto }>(
    `/api/v1/my-family/members/${personIdKey}`,
    data
  );
  return response.data;
}

/**
 * Get the current user's group involvement and attendance summary
 */
export async function getMyInvolvement(): Promise<MyInvolvementDto> {
  const response = await get<{ data: MyInvolvementDto }>('/api/v1/my-involvement');
  return response.data;
}
