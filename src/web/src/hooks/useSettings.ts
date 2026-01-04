/**
 * Settings management hooks using TanStack Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as settingsApi from '@/services/api/settings';
import type {
  UpdateUserPreferenceRequest,
  ChangePasswordRequest,
} from '@/types/settings';

// ============================================================================
// Preferences
// ============================================================================

/**
 * Get the current user's preferences
 */
export function usePreferences() {
  return useQuery({
    queryKey: ['settings', 'preferences'],
    queryFn: () => settingsApi.getPreferences(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Update the current user's preferences
 */
export function useUpdatePreferences() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateUserPreferenceRequest) => settingsApi.updatePreferences(data),
    onSuccess: () => {
      // Invalidate preferences to refetch
      queryClient.invalidateQueries({ queryKey: ['settings', 'preferences'] });
    },
  });
}

// ============================================================================
// Sessions
// ============================================================================

/**
 * Get all active sessions for the current user
 */
export function useSessions() {
  return useQuery({
    queryKey: ['settings', 'sessions'],
    queryFn: () => settingsApi.getSessions(),
    staleTime: 1 * 60 * 1000, // 1 minute
  });
}

/**
 * Revoke a session
 */
export function useRevokeSession() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (idKey: string) => settingsApi.revokeSession(idKey),
    onSuccess: () => {
      // Invalidate sessions to refetch
      queryClient.invalidateQueries({ queryKey: ['settings', 'sessions'] });
    },
  });
}

// ============================================================================
// Security
// ============================================================================

/**
 * Change the current user's password
 */
export function useChangePassword() {
  return useMutation({
    mutationFn: (data: ChangePasswordRequest) => settingsApi.changePassword(data),
  });
}

/**
 * Get two-factor authentication status
 */
export function useTwoFactorStatus() {
  return useQuery({
    queryKey: ['settings', 'two-factor', 'status'],
    queryFn: () => settingsApi.getTwoFactorStatus(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Setup two-factor authentication (generates QR code)
 */
export function useSetupTwoFactor() {
  return useMutation({
    mutationFn: () => settingsApi.setupTwoFactor(),
  });
}

/**
 * Verify and enable two-factor authentication
 */
export function useVerifyTwoFactor() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (code: string) => settingsApi.verifyTwoFactor(code),
    onSuccess: () => {
      // Invalidate 2FA status to refetch
      queryClient.invalidateQueries({ queryKey: ['settings', 'two-factor', 'status'] });
    },
  });
}

/**
 * Disable two-factor authentication
 */
export function useDisableTwoFactor() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (code: string) => settingsApi.disableTwoFactor(code),
    onSuccess: () => {
      // Invalidate 2FA status to refetch
      queryClient.invalidateQueries({ queryKey: ['settings', 'two-factor', 'status'] });
    },
  });
}
