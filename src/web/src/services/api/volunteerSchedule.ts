/**
 * Volunteer Schedule API service
 */

import { get, post, put } from './client';
import type {
  ScheduleAssignmentDto,
  MyScheduleDto,
  CreateAssignmentsRequest,
  UpdateAssignmentStatusRequest,
  GetAssignmentsParams,
} from '@/types/volunteer';

/**
 * Create schedule assignments for members
 */
export async function createAssignments(
  groupIdKey: string,
  request: CreateAssignmentsRequest
): Promise<ScheduleAssignmentDto[]> {
  return post<ScheduleAssignmentDto[]>(
    `/groups/${groupIdKey}/schedule-assignments`,
    request
  );
}

/**
 * Get schedule assignments for a group
 */
export async function getAssignments(
  groupIdKey: string,
  params: GetAssignmentsParams = {}
): Promise<ScheduleAssignmentDto[]> {
  const queryParams = new URLSearchParams();

  if (params.startDate) queryParams.set('startDate', params.startDate);
  if (params.endDate) queryParams.set('endDate', params.endDate);

  const query = queryParams.toString();
  const endpoint = `/groups/${groupIdKey}/schedule-assignments${query ? `?${query}` : ''}`;

  return get<ScheduleAssignmentDto[]>(endpoint);
}

/**
 * Update assignment status (confirm or decline)
 */
export async function updateAssignmentStatus(
  assignmentIdKey: string,
  request: UpdateAssignmentStatusRequest
): Promise<ScheduleAssignmentDto> {
  return put<ScheduleAssignmentDto>(
    `/my-schedule/${assignmentIdKey}`,
    request
  );
}

/**
 * Get current user's schedule
 */
export async function getMySchedule(
  params: GetAssignmentsParams = {}
): Promise<MyScheduleDto[]> {
  const queryParams = new URLSearchParams();

  if (params.startDate) queryParams.set('startDate', params.startDate);
  if (params.endDate) queryParams.set('endDate', params.endDate);

  const query = queryParams.toString();
  const endpoint = `/my-schedule${query ? `?${query}` : ''}`;

  return get<MyScheduleDto[]>(endpoint);
}
