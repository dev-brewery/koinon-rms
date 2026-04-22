/**
 * Check-in Operations API client (#482).
 *
 * Thin wrapper around GET /checkin-operations/dashboard and POST
 * /checkin-operations/rooms/:idKey/toggle. Types are colocated here because
 * the feature is self-contained and nothing else in the codebase consumes
 * these DTOs yet.
 */

import type { IdKey, DateTime } from './types';
import { get, post } from './client';

export type CapacityPillColor = 'green' | 'yellow' | 'red' | 'grey';

export interface CheckinOperationsRoomDto {
  locationIdKey: IdKey;
  locationName: string;
  checkedInCount: number;
  capacity?: number;
  percentFull: number;
  capacityPillColor: CapacityPillColor;
  isOpen: boolean;
}

export interface CheckinOperationsAttendeeDto {
  attendanceIdKey: IdKey;
  personIdKey: IdKey;
  fullName: string;
  locationIdKey: IdKey;
  locationName: string;
  checkInTime: DateTime;
  checkOutTime?: DateTime | null;
  isPresent: boolean;
}

export interface CheckinOperationsSummaryDto {
  totalCheckedIn: number;
  currentlyPresent: number;
  checkedOut: number;
}

export interface CheckinOperationsDashboardDto {
  rooms: CheckinOperationsRoomDto[];
  attendees: CheckinOperationsAttendeeDto[];
  summary: CheckinOperationsSummaryDto;
  generatedAt: DateTime;
}

export interface ToggleRoomResponseDto {
  locationIdKey: IdKey;
  isOpen: boolean;
}

/**
 * Fetch the full operations dashboard snapshot. Intended to be called on a
 * 5-second polling interval from {@link useCheckinOperations}.
 */
export async function getCheckinOperationsDashboard(): Promise<CheckinOperationsDashboardDto> {
  const response = await get<{ data: CheckinOperationsDashboardDto }>(
    '/checkin-operations/dashboard'
  );
  return response.data;
}

/**
 * Toggle a room's open/closed state for check-ins. Returns the new state.
 */
export async function toggleCheckinOperationsRoom(
  locationIdKey: string
): Promise<ToggleRoomResponseDto> {
  const response = await post<{ data: ToggleRoomResponseDto }>(
    `/checkin-operations/rooms/${encodeURIComponent(locationIdKey)}/toggle`,
    undefined
  );
  return response.data;
}
