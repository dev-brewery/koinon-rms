/**
 * TypeScript types for Pager System
 * Maps to backend DTOs in Koinon.Application.DTOs.Pager
 */

import type { IdKey, DateTime } from '@/services/api/types';

// ============================================================================
// Enums
// ============================================================================

/**
 * Type of pager message
 */
export enum PagerMessageType {
  /** Parent needs to pick up child */
  PickupNeeded = 0,
  /** Child needs attention (bathroom, etc.) */
  NeedsAttention = 1,
  /** Service is ending */
  ServiceEnding = 2,
  /** Custom message */
  Custom = 3,
}

/**
 * Status of a pager message
 */
export enum PagerMessageStatus {
  /** Message queued for sending */
  Pending = 0,
  /** Message sent to pager system */
  Sent = 1,
  /** Message delivered to pager device */
  Delivered = 2,
  /** Message failed to deliver */
  Failed = 3,
}

// ============================================================================
// DTOs
// ============================================================================

/**
 * Active pager assignment for current service
 */
export interface PagerAssignmentDto {
  /** Encoded ID of the pager assignment */
  idKey: IdKey;
  /** Physical pager number given to parent */
  pagerNumber: number;
  /** Encoded ID of the attendance record */
  attendanceIdKey: IdKey;
  /** Name of the child being cared for */
  childName: string;
  /** Name of the group (classroom/service) */
  groupName: string;
  /** Physical location name */
  locationName: string;
  /** Parent's phone number (optional) */
  parentPhoneNumber?: string;
  /** When the child was checked in */
  checkedInAt: DateTime;
  /** Number of messages sent to this pager */
  messagesSentCount: number;
}

/**
 * Individual pager message
 */
export interface PagerMessageDto {
  /** Encoded ID of the message */
  idKey: IdKey;
  /** Type of message */
  messageType: PagerMessageType;
  /** Full message text sent to pager */
  messageText: string;
  /** Current delivery status */
  status: PagerMessageStatus;
  /** When message was sent */
  sentDateTime: DateTime;
  /** When message was delivered (if applicable) */
  deliveredDateTime?: DateTime;
  /** Name of person who sent the message */
  sentByPersonName: string;
}

/**
 * Historical pager activity for a specific assignment
 */
export interface PageHistoryDto {
  /** Encoded ID of the pager assignment */
  idKey: IdKey;
  /** Physical pager number */
  pagerNumber: number;
  /** Name of the child */
  childName: string;
  /** Parent's phone number */
  parentPhoneNumber: string;
  /** All messages sent to this pager */
  messages: PagerMessageDto[];
}

// ============================================================================
// Request DTOs
// ============================================================================

/**
 * Request to send a page
 */
export interface SendPageRequest {
  /** Pager number to send message to (as string for form compatibility) */
  pagerNumber: string;
  /** Type of message to send */
  messageType: PagerMessageType;
  /** Custom message text (required if messageType is Custom) */
  customMessage?: string;
}

/**
 * Request to search pager assignments
 */
export interface PageSearchRequest {
  /** Search by child name, pager number, or phone */
  searchTerm?: string;
  /** Filter by campus ID */
  campusId?: number;
  /** Filter by check-in date */
  date?: DateTime;
}
