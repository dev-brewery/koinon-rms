/**
 * Communication TypeScript Types
 * Maps C# DTOs from Communication domain
 */

import type { IdKey, DateTime } from '@/services/api/types';

// ============================================================================
// Enums
// ============================================================================

/**
 * Type of communication
 * Maps to CommunicationType enum in C#
 */
export enum CommunicationType {
  /** Email communication */
  Email = 0,
  /** SMS (text message) communication */
  Sms = 1,
}

/**
 * Overall status of a communication
 * Maps to CommunicationStatus enum in C#
 */
export enum CommunicationStatus {
  /** Communication is being drafted and has not been sent */
  Draft = 0,
  /** Communication is queued and waiting to be sent */
  Pending = 1,
  /** Communication has been sent to all recipients */
  Sent = 2,
  /** Communication failed to send */
  Failed = 3,
}

/**
 * Status of a communication for an individual recipient
 * Maps to CommunicationRecipientStatus enum in C#
 */
export enum CommunicationRecipientStatus {
  /** Communication is pending delivery to this recipient */
  Pending = 0,
  /** Communication has been delivered to this recipient */
  Delivered = 1,
  /** Communication failed to deliver to this recipient */
  Failed = 2,
  /** Recipient has opened the communication (email tracking) */
  Opened = 3,
}

// ============================================================================
// Recipient Types
// ============================================================================

/**
 * Communication recipient details
 */
export interface CommunicationRecipientDto {
  idKey: IdKey;
  personIdKey: string;
  address: string;
  recipientName?: string;
  status: string;
  deliveredDateTime?: DateTime;
  openedDateTime?: DateTime;
  errorMessage?: string;
  groupIdKey?: string;
}

// ============================================================================
// Communication Types
// ============================================================================

/**
 * Full communication details
 */
export interface CommunicationDto {
  idKey: IdKey;
  guid: string;
  communicationType: string;
  status: string;
  subject?: string;
  body: string;
  fromEmail?: string;
  fromName?: string;
  replyToEmail?: string;
  sentDateTime?: DateTime;
  recipientCount: number;
  deliveredCount: number;
  failedCount: number;
  openedCount: number;
  note?: string;
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
  recipients: CommunicationRecipientDto[];
}

/**
 * Summary communication for lists
 */
export interface CommunicationSummaryDto {
  idKey: IdKey;
  communicationType: string;
  status: string;
  subject?: string;
  recipientCount: number;
  deliveredCount: number;
  failedCount: number;
  createdDateTime: DateTime;
  sentDateTime?: DateTime;
}

// ============================================================================
// Request Types
// ============================================================================

/**
 * Request to create a new communication
 */
export interface CreateCommunicationDto {
  communicationType: string;
  subject?: string;
  body: string;
  fromEmail?: string;
  fromName?: string;
  replyToEmail?: string;
  note?: string;
  groupIdKeys: string[];
}

/**
 * Request to update a communication
 */
export interface UpdateCommunicationDto {
  subject?: string;
  body?: string;
  fromEmail?: string;
  fromName?: string;
  replyToEmail?: string;
  note?: string;
}

// ============================================================================
// Query Parameters
// ============================================================================

/**
 * Query parameters for listing communications
 */
export interface CommunicationsParams {
  page?: number;
  pageSize?: number;
  status?: string;
}
