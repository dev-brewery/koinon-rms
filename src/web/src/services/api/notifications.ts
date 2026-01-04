/**
 * Notifications API service
 */

import { get, put, del } from './client';
import type {
  NotificationDto,
  NotificationPreferenceDto,
  UpdateNotificationPreferenceDto,
  UnreadCountResponse,
  MarkAllAsReadResponse,
} from '@/types/notification';

// ============================================================================
// API Functions
// ============================================================================

/**
 * Get notifications for the current user
 * @param unreadOnly - If true, only return unread notifications
 * @param limit - Maximum number of notifications to return
 */
export async function getNotifications(
  unreadOnly?: boolean,
  limit?: number
): Promise<NotificationDto[]> {
  const params = new URLSearchParams();
  
  if (unreadOnly !== undefined) {
    params.append('unreadOnly', String(unreadOnly));
  }
  
  if (limit !== undefined) {
    params.append('limit', String(limit));
  }

  const queryString = params.toString();
  const endpoint = `/notifications${queryString ? `?${queryString}` : ''}`;
  
  const response = await get<{ data: NotificationDto[] }>(endpoint);
  return response.data;
}

/**
 * Get unread notification count for the current user
 */
export async function getUnreadCount(): Promise<number> {
  const response = await get<{ data: UnreadCountResponse }>('/notifications/unread-count');
  return response.data.count;
}

/**
 * Get a single notification by IdKey
 */
export async function getNotification(idKey: string): Promise<NotificationDto> {
  const response = await get<{ data: NotificationDto }>(`/notifications/${idKey}`);
  return response.data;
}

/**
 * Mark a notification as read
 */
export async function markAsRead(idKey: string): Promise<void> {
  await put<void>(`/notifications/${idKey}/read`);
}

/**
 * Mark all notifications as read for the current user
 */
export async function markAllAsRead(): Promise<number> {
  const response = await put<{ data: MarkAllAsReadResponse }>('/notifications/read-all');
  return response.data.markedCount;
}

/**
 * Delete a notification
 */
export async function deleteNotification(idKey: string): Promise<void> {
  await del<void>(`/notifications/${idKey}`);
}

/**
 * Get notification preferences for the current user
 */
export async function getPreferences(): Promise<NotificationPreferenceDto[]> {
  const response = await get<{ data: NotificationPreferenceDto[] }>('/notifications/preferences');
  return response.data;
}

/**
 * Update a notification preference
 */
export async function updatePreference(
  dto: UpdateNotificationPreferenceDto
): Promise<NotificationPreferenceDto> {
  const response = await put<{ data: NotificationPreferenceDto }>(
    '/notifications/preferences',
    dto
  );
  return response.data;
}
