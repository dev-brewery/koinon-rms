/**
 * Settings API service
 */

import { get, put, post, del } from './client';
import type {
  UserPreferenceDto,
  UpdateUserPreferenceRequest,
  UserSessionDto,
  ChangePasswordRequest,
  TwoFactorSetupDto,
  TwoFactorStatusDto,
  TwoFactorVerifyRequest,
  TwoFactorDisableRequest,
} from '@/types/settings';

// ============================================================================
// Preferences
// ============================================================================

/**
 * Get the current user's preferences
 */
export async function getPreferences(): Promise<UserPreferenceDto> {
  const response = await get<{ data: UserPreferenceDto }>('/my-settings/preferences');
  return response.data;
}

/**
 * Update the current user's preferences
 */
export async function updatePreferences(
  data: UpdateUserPreferenceRequest
): Promise<UserPreferenceDto> {
  const response = await put<{ data: UserPreferenceDto }>('/my-settings/preferences', data);
  return response.data;
}

// ============================================================================
// Sessions
// ============================================================================

/**
 * Get all active sessions for the current user
 */
export async function getSessions(): Promise<UserSessionDto[]> {
  const response = await get<{ data: UserSessionDto[] }>('/my-settings/sessions');
  return response.data;
}

/**
 * Revoke a session by IdKey
 */
export async function revokeSession(idKey: string): Promise<void> {
  await del(`/my-settings/sessions/${idKey}`);
}

// ============================================================================
// Security
// ============================================================================

/**
 * Change the current user's password
 */
export async function changePassword(data: ChangePasswordRequest): Promise<void> {
  await post('/my-settings/change-password', data);
}

/**
 * Get two-factor authentication status
 */
export async function getTwoFactorStatus(): Promise<TwoFactorStatusDto> {
  const response = await get<{ data: TwoFactorStatusDto }>('/my-settings/two-factor');
  return response.data;
}

/**
 * Setup two-factor authentication (generates QR code)
 */
export async function setupTwoFactor(): Promise<TwoFactorSetupDto> {
  const response = await post<{ data: TwoFactorSetupDto }>('/my-settings/two-factor/setup');
  return response.data;
}

/**
 * Verify and enable two-factor authentication
 */
export async function verifyTwoFactor(code: string): Promise<void> {
  const request: TwoFactorVerifyRequest = { code };
  await post('/my-settings/two-factor/verify', request);
}

/**
 * Disable two-factor authentication
 */
export async function disableTwoFactor(code: string): Promise<void> {
  const request: TwoFactorDisableRequest = { code };
  await del('/my-settings/two-factor', {
    body: JSON.stringify(request),
  });
}
