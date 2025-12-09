/**
 * Volunteer Schedule Types
 * TypeScript types for volunteer serving schedule management
 */

import type { IdKey, DateOnly, DateTime } from '@/services/api/types';

export enum VolunteerScheduleStatus {
  Scheduled = 0,
  Confirmed = 1,
  Declined = 2,
  NoResponse = 3,
}

export interface ScheduleAssignmentDto {
  idKey: IdKey;
  memberIdKey: IdKey;
  memberName: string;
  scheduleIdKey: IdKey;
  scheduleName: string;
  assignedDate: DateOnly;
  status: VolunteerScheduleStatus;
  declineReason?: string;
  respondedDateTime?: DateTime;
  note?: string;
}

export interface MyScheduleDto {
  date: DateOnly;
  assignments: ScheduleAssignmentDto[];
}

export interface CreateAssignmentsRequest {
  memberIdKeys: IdKey[];
  scheduleIdKey: IdKey;
  dates: DateOnly[];
}

export interface UpdateAssignmentStatusRequest {
  status: VolunteerScheduleStatus;
  declineReason?: string;
}

export interface GetAssignmentsParams {
  startDate?: DateOnly;
  endDate?: DateOnly;
}
