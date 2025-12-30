/**
 * Follow-up Types
 *
 * TypeScript definitions for the follow-up management system.
 */

import type { IdKey, DateTime } from '@/services/api/types';

/**
 * Status of a follow-up record
 */
export enum FollowUpStatus {
  /** Initial state - not yet contacted */
  Pending = 0,
  /** Contact has been attempted or made */
  Contacted = 1,
  /** Multiple contact attempts with no response */
  NoResponse = 2,
  /** Successfully connected with the person */
  Connected = 3,
  /** Person declined further follow-up */
  Declined = 4,
}

/**
 * Represents a follow-up task for a person
 */
export interface FollowUpDto {
  /** Unique identifier for this follow-up record */
  idKey: IdKey;
  /** IdKey of the person being followed up with */
  personIdKey: IdKey;
  /** Name of the person being followed up with */
  personName: string;
  /** IdKey of the attendance record that triggered this follow-up (if applicable) */
  attendanceIdKey?: IdKey;
  /** Current status of the follow-up */
  status: FollowUpStatus;
  /** Notes about the follow-up */
  notes?: string;
  /** IdKey of the person assigned to this follow-up */
  assignedToIdKey?: IdKey;
  /** Name of the person assigned to this follow-up */
  assignedToName?: string;
  /** When contact was made */
  contactedDateTime?: DateTime;
  /** When the follow-up was completed */
  completedDateTime?: DateTime;
  /** When this follow-up record was created */
  createdDateTime: DateTime;
}

/**
 * Request to update the status of a follow-up record
 */
export interface UpdateFollowUpStatusRequest {
  /** New status for the follow-up */
  status: FollowUpStatus;
  /** Additional notes about the status update */
  notes?: string;
}

/**
 * Request to assign a follow-up to a person
 */
export interface AssignFollowUpRequest {
  /** IdKey of the person to assign this follow-up to */
  assignedToIdKey: IdKey;
}
