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
  /** Communication is scheduled to be sent at a future time */
  Scheduled = 4,
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
  scheduledDateTime?: DateTime;
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
  scheduledDateTime?: DateTime;
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
  scheduledDateTime?: string;
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

/**
 * Request to schedule a communication for future delivery
 */
export interface ScheduleCommunicationRequest {
  scheduledDateTime: string;
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

// ============================================================================
// Communication Template Types
// ============================================================================

/**
 * Full communication template details
 */
export interface CommunicationTemplateDto {
  idKey: IdKey;
  name: string;
  communicationType: string; // 'Email' | 'Sms'
  subject?: string;
  body: string;
  description?: string;
  isActive: boolean;
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

/**
 * Summary template for lists
 */
export interface CommunicationTemplateSummaryDto {
  idKey: IdKey;
  name: string;
  communicationType: string; // 'Email' | 'Sms'
  isActive: boolean;
}

/**
 * Request to create a new communication template
 */
export interface CreateCommunicationTemplateDto {
  name: string;
  communicationType: string; // 'Email' | 'Sms'
  subject?: string;
  body: string;
  description?: string;
  isActive?: boolean;
}

/**
 * Request to update a communication template
 */
export interface UpdateCommunicationTemplateDto {
  name?: string;
  subject?: string;
  body?: string;
  description?: string;
  isActive?: boolean;
}

/**
 * Query parameters for listing communication templates
 */
export interface CommunicationTemplatesParams {
  page?: number;
  pageSize?: number;
  type?: string; // Filter by communication type
  isActive?: boolean; // Filter by active status
}

// ============================================================================
// Merge Field Types
// ============================================================================

/**
 * Merge field definition for dynamic content in communications
 */
export interface MergeFieldDto {
  /** Field name (e.g., "FirstName") */
  name: string;
  /** Token to use in templates (e.g., "{{FirstName}}") */
  token: string;
  /** Human-readable description of the field */
  description: string;
}

/**
 * Request to preview a communication with merge fields resolved
 */
export interface CommunicationPreviewRequest {
  /** Email subject (optional for SMS) */
  subject?: string;
  /** Email or SMS body with merge field tokens */
  body: string;
  /** IdKey of person to use for preview (uses sample data if not provided) */
  personIdKey?: string;
}

/**
 * Response containing rendered preview with merge fields resolved
 */
export interface CommunicationPreviewResponse {
  /** Rendered subject with merge fields resolved */
  subject?: string;
  /** Rendered body with merge fields resolved */
  body: string;
  /** Name of person used for preview data */
  personName: string;
}
