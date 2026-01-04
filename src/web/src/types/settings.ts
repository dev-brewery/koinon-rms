/**
 * User Settings TypeScript Types
 * Maps C# DTOs from UserSettings domain
 */

import type { IdKey, DateTime } from '@/services/api/types';

// ============================================================================
// Enums
// ============================================================================

/**
 * Theme preference for the application
 */
export enum Theme {
  /** Use system theme (light/dark based on OS) */
  System = 0,
  /** Force light theme */
  Light = 1,
  /** Force dark theme */
  Dark = 2,
}

// ============================================================================
// User Preference Types
// ============================================================================

/**
 * User display and notification preferences
 */
export interface UserPreferenceDto {
  idKey: IdKey;
  theme: Theme;
  dateFormat: string; // 'MM/DD/YYYY' | 'DD/MM/YYYY' | 'YYYY-MM-DD'
  timeZone: string; // IANA timezone string
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

/**
 * Request to update user preferences
 */
export interface UpdateUserPreferenceRequest {
  theme: Theme;
  dateFormat: string;
  timeZone: string;
}

// ============================================================================
// Session Types
// ============================================================================

/**
 * User session information
 */
export interface UserSessionDto {
  idKey: IdKey;
  deviceInfo: string | null;
  ipAddress: string;
  location: string | null;
  lastActivityAt: DateTime;
  isActive: boolean;
  isCurrentSession: boolean;
  createdDateTime: DateTime;
}

// ============================================================================
// Security Types
// ============================================================================

/**
 * Request to change password
 */
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

/**
 * Two-factor authentication setup information
 */
export interface TwoFactorSetupDto {
  secretKey: string;
  qrCodeUri: string;
  recoveryCodes: string[];
}

/**
 * Two-factor authentication status
 */
export interface TwoFactorStatusDto {
  isEnabled: boolean;
  enabledAt?: DateTime;
}

/**
 * Request to verify two-factor authentication code
 */
export interface TwoFactorVerifyRequest {
  code: string;
}

/**
 * Request to disable two-factor authentication
 */
export interface TwoFactorDisableRequest {
  code: string;
}
