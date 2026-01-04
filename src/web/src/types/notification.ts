/**
 * Notification TypeScript Types
 * Maps C# DTOs from Notification domain
 */

import type { IdKey, DateTime } from '@/services/api/types';

// ============================================================================
// Enums
// ============================================================================

/**
 * Type of notification
 * Maps to NotificationType enum in C#
 */
export enum NotificationType {
  /** Check-in related alerts (e.g., parent pickup) */
  CheckinAlert = 0,
  /** Communication delivery status updates */
  CommunicationStatus = 1,
  /** System-level alerts and announcements */
  SystemAlert = 2,
  /** Membership request notifications */
  MembershipRequest = 3,
  /** Follow-up task reminders */
  FollowUp = 4,
}

// ============================================================================
// Notification Types
// ============================================================================

/**
 * Full notification details
 */
export interface NotificationDto {
  idKey: IdKey;
  guid: string;
  notificationType: NotificationType;
  title: string;
  message: string;
  isRead: boolean;
  readDateTime: DateTime | null;
  actionUrl: string | null;
  metadataJson: string | null;
  createdDateTime: DateTime;
}

/**
 * Notification preference for a user
 */
export interface NotificationPreferenceDto {
  idKey: IdKey;
  notificationType: NotificationType;
  isEnabled: boolean;
}

// ============================================================================
// Request Types
// ============================================================================

/**
 * Request to update a notification preference
 */
export interface UpdateNotificationPreferenceDto {
  notificationType: NotificationType;
  isEnabled: boolean;
}

// ============================================================================
// Response Types
// ============================================================================

/**
 * Unread count response
 */
export interface UnreadCountResponse {
  count: number;
}

/**
 * Mark all as read response
 */
export interface MarkAllAsReadResponse {
  markedCount: number;
}
