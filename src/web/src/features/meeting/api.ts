/**
 * Group meeting RSVP API service
 */

import { get, put, post } from '@/services/api/client';
import type { IdKey } from '@/services/api/types';

// ============================================================================
// Types
// ============================================================================

export type RsvpStatus = 'NoResponse' | 'Attending' | 'NotAttending' | 'Maybe';

export interface RsvpDto {
  idKey: IdKey;
  personIdKey: IdKey;
  personName: string;
  status: RsvpStatus;
  note?: string;
  respondedDateTime?: string;
}

export interface MeetingRsvpSummaryDto {
  meetingDate: string;
  attending: number;
  notAttending: number;
  maybe: number;
  noResponse: number;
  totalInvited: number;
  rsvps: RsvpDto[];
}

export interface UpdateRsvpRequest {
  status: RsvpStatus;
  note?: string;
}

export interface MyRsvpDto {
  groupIdKey: IdKey;
  groupName: string;
  meetingDate: string;
  status: RsvpStatus;
  note?: string;
}

// ============================================================================
// API Functions
// ============================================================================

export async function sendRsvpRequests(groupIdKey: IdKey, meetingDate: string): Promise<{ count: number; message: string }> {
  return post(`/api/v1/groups/${groupIdKey}/meetings/${meetingDate}/request-rsvp`, {});
}

export async function getMeetingRsvps(groupIdKey: IdKey, meetingDate: string): Promise<MeetingRsvpSummaryDto> {
  return get(`/api/v1/groups/${groupIdKey}/meetings/${meetingDate}/rsvps`);
}

export async function updateMyRsvp(groupIdKey: IdKey, meetingDate: string, request: UpdateRsvpRequest): Promise<void> {
  return put(`/api/v1/my-groups/${groupIdKey}/rsvp/${meetingDate}`, request);
}

export async function getMyRsvps(startDate?: string, endDate?: string): Promise<MyRsvpDto[]> {
  const params = new URLSearchParams();
  if (startDate) params.append('startDate', startDate);
  if (endDate) params.append('endDate', endDate);
  
  const queryString = params.toString();
  return get(`/api/v1/my-rsvps${queryString ? `?${queryString}` : ''}`);
}
