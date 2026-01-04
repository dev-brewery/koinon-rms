/**
 * Notifications management hooks using TanStack Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as notificationsApi from '@/services/api/notifications';
import type { UpdateNotificationPreferenceDto } from '@/types/notification';

// ============================================================================
// Query Keys
// ============================================================================

const QUERY_KEYS = {
  all: ['notifications'] as const,
  list: (unreadOnly?: boolean, limit?: number) =>
    ['notifications', 'list', { unreadOnly, limit }] as const,
  detail: (idKey: string) => ['notifications', 'detail', idKey] as const,
  unreadCount: ['notifications', 'unread-count'] as const,
  preferences: ['notifications', 'preferences'] as const,
};

// ============================================================================
// Query Hooks
// ============================================================================

/**
 * Get notifications for the current user
 * @param unreadOnly - If true, only return unread notifications
 * @param limit - Maximum number of notifications to return
 */
export function useNotifications(unreadOnly?: boolean, limit?: number) {
  return useQuery({
    queryKey: QUERY_KEYS.list(unreadOnly, limit),
    queryFn: () => notificationsApi.getNotifications(unreadOnly, limit),
    staleTime: 30 * 1000, // 30 seconds - notifications can update frequently
    refetchInterval: 60 * 1000, // Refetch every minute to stay up to date
  });
}

/**
 * Get a single notification by IdKey
 */
export function useNotification(idKey?: string) {
  return useQuery({
    queryKey: QUERY_KEYS.detail(idKey!),
    queryFn: () => notificationsApi.getNotification(idKey!),
    enabled: !!idKey,
    staleTime: 30 * 1000,
  });
}

/**
 * Get unread notification count
 * This will be automatically updated via SignalR when useNotificationHub is active
 */
export function useUnreadCount() {
  return useQuery({
    queryKey: QUERY_KEYS.unreadCount,
    queryFn: notificationsApi.getUnreadCount,
    staleTime: 30 * 1000,
    refetchInterval: 60 * 1000, // Refetch every minute as fallback if SignalR is not connected
  });
}

/**
 * Get notification preferences for the current user
 */
export function useNotificationPreferences() {
  return useQuery({
    queryKey: QUERY_KEYS.preferences,
    queryFn: notificationsApi.getPreferences,
    staleTime: 5 * 60 * 1000, // 5 minutes - preferences change infrequently
  });
}

// ============================================================================
// Mutation Hooks
// ============================================================================

/**
 * Mark a notification as read
 */
export function useMarkAsRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (idKey: string) => notificationsApi.markAsRead(idKey),
    onSuccess: (_, idKey) => {
      // Invalidate all notification queries to refetch updated data
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.detail(idKey) });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.unreadCount });
    },
  });
}

/**
 * Mark all notifications as read
 */
export function useMarkAllAsRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => notificationsApi.markAllAsRead(),
    onSuccess: () => {
      // Invalidate all notification queries
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.unreadCount });
    },
  });
}

/**
 * Delete a notification
 */
export function useDeleteNotification() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (idKey: string) => notificationsApi.deleteNotification(idKey),
    onSuccess: () => {
      // Invalidate all notification queries to refetch updated lists
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.unreadCount });
    },
  });
}

/**
 * Update a notification preference
 */
export function useUpdatePreference() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: UpdateNotificationPreferenceDto) =>
      notificationsApi.updatePreference(dto),
    onSuccess: () => {
      // Invalidate preferences query to refetch
      queryClient.invalidateQueries({ queryKey: QUERY_KEYS.preferences });
    },
  });
}
