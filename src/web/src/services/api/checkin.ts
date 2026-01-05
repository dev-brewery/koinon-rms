/**
 * Check-in API service
 */

import { get, post } from './client';
import type {
  CheckinConfigDto,
  CheckinConfigParams,
  CheckinSearchRequest,
  CheckinFamilyDto,
  CheckinOpportunitiesParams,
  CheckinOpportunitiesResponse,
  BatchCheckinRequest,
  BatchCheckinResultDto,
  CheckinRequestItem,
  LabelDto,
  LabelParams,
  SupervisorLoginRequest,
  SupervisorLoginResponse,
  RoomRosterDto,
} from './types';

/**
 * Get check-in configuration for current kiosk/campus
 */
export async function getCheckinConfiguration(
  params: CheckinConfigParams = {}
): Promise<CheckinConfigDto> {
  const queryParams = new URLSearchParams();

  if (params.kioskId) queryParams.set('kioskId', params.kioskId);
  if (params.campusId) queryParams.set('campusId', params.campusId);

  const query = queryParams.toString();
  const endpoint = `/checkin/configuration${query ? `?${query}` : ''}`;

  const response = await get<{ data: CheckinConfigDto }>(endpoint);
  return response.data;
}

/**
 * Search for families to check in
 */
export async function searchFamiliesForCheckin(
  request: CheckinSearchRequest
): Promise<CheckinFamilyDto[]> {
  const response = await post<{ data: CheckinFamilyDto[] }>('/checkin/search', request);
  return response.data;
}

/**
 * Get available check-in opportunities for a family
 */
export async function getCheckinOpportunities(
  familyIdKey: string,
  params: CheckinOpportunitiesParams = {}
): Promise<CheckinOpportunitiesResponse> {
  const queryParams = new URLSearchParams();

  if (params.scheduleId) queryParams.set('scheduleId', params.scheduleId);

  const query = queryParams.toString();
  const endpoint = `/checkin/opportunities/${familyIdKey}${query ? `?${query}` : ''}`;

  const response = await get<{ data: CheckinOpportunitiesResponse }>(endpoint);
  return response.data;
}

/**
 * Record check-in attendance for one or more people.
 *
 * Accepts UI-friendly CheckinRequestItem array and transforms to
 * backend BatchCheckinRequest format.
 *
 * @param items - Array of check-in items from UI selection
 * @param deviceIdKey - Optional kiosk device ID
 * @returns Batch check-in result with individual results and labels
 */
export async function recordAttendance(
  items: CheckinRequestItem[],
  deviceIdKey?: string
): Promise<BatchCheckinResultDto> {
  // Transform UI items to backend BatchCheckinRequest format
  const request: BatchCheckinRequest = {
    checkIns: items.map(item => ({
      personIdKey: item.personIdKey,
      locationIdKey: item.locationIdKey,
      scheduleIdKey: item.scheduleIdKey,
      // Default to generating security codes for children's ministry
      generateSecurityCode: true,
    })),
    deviceIdKey,
  };

  // Backend returns result directly (not wrapped in data envelope for this endpoint)
  const response = await post<BatchCheckinResultDto>(
    '/checkin/attendance',
    request
  );
  return response;
}

/**
 * Record check-out for an attendance record
 */
export async function checkout(attendanceIdKey: string): Promise<void> {
  // Backend returns 204 No Content
  await post(`/checkin/checkout/${attendanceIdKey}`, undefined);
}

/**
 * Get printable labels for an attendance record
 */
export async function getLabels(
  attendanceIdKey: string,
  params: LabelParams = {}
): Promise<LabelDto[]> {
  const queryParams = new URLSearchParams();

  if (params.labelType) queryParams.set('labelType', params.labelType);
  if (params.format) queryParams.set('format', params.format);

  const query = queryParams.toString();
  const endpoint = `/checkin/labels/${attendanceIdKey}${query ? `?${query}` : ''}`;

  const response = await get<{ data: LabelDto[] }>(endpoint);
  return response.data;
}

/**
 * Supervisor Mode API
 */

/**
 * Authenticate supervisor with PIN
 */
export async function supervisorLogin(
  request: SupervisorLoginRequest
): Promise<SupervisorLoginResponse> {
  const response = await post<SupervisorLoginResponse>(
    '/checkin/supervisor/login',
    request
  );
  return response;
}

/**
 * End supervisor session
 */
export async function supervisorLogout(sessionToken: string): Promise<void> {
  await post('/checkin/supervisor/logout', undefined, {
    headers: {
      'X-Supervisor-Session': sessionToken,
    },
  });
}

/**
 * Reprint label for an attendance record (supervisor mode)
 */
export async function supervisorReprint(
  attendanceIdKey: string,
  sessionToken: string
): Promise<LabelDto[]> {
  const response = await post<{ labels: LabelDto[] }>(
    `/checkin/supervisor/reprint/${attendanceIdKey}`,
    undefined,
    {
      headers: {
        'X-Supervisor-Session': sessionToken,
      },
    }
  );
  return response.labels;
}

/**
 * Room Roster API
 */

/**
 * Get room roster for a single location
 */
export async function getRoomRoster(locationIdKey: string): Promise<RoomRosterDto> {
  const response = await get<{ data: RoomRosterDto }>(`/checkin/roster/${locationIdKey}`);
  return response.data;
}

/**
 * Get rosters for multiple locations at once
 */
export async function getMultipleRoomRosters(
  locationIdKeys: string[]
): Promise<RoomRosterDto[]> {
  const queryParams = new URLSearchParams();
  queryParams.set('locationIdKeys', locationIdKeys.join(','));

  const response = await get<{ data: RoomRosterDto[] }>(
    `/checkin/roster?${queryParams.toString()}`
  );
  return response.data;
}

/**
 * Check out a child from the roster
 * Uses the existing attendance checkout endpoint
 */
export async function checkoutFromRoster(attendanceIdKey: string): Promise<void> {
  // Backend returns 204 No Content
  await post(`/checkin/checkout/${attendanceIdKey}`, undefined);
}
