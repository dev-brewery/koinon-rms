/**
 * Check-in API service
 */

import { get, post } from './client';
import type {
  CheckinConfigDto,
  CheckinConfigParams,
  CheckinSearchRequest,
  CheckinFamilyDto,
  CheckinPersonDto,
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
  KioskFamilyRegistrationRequest,
  CheckinFamilySearchResultDto,
  CheckinFamilyMemberDto,
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
 * Search for families to check in.
 * Uses POST /checkin/search with body to avoid query-string URL mismatch
 * with E2E route interception patterns.
 *
 * The response `data` array may arrive in either the backend DTO format
 * (familyIdKey / familyName / personIdKey) or the frontend DTO format
 * (idKey / name) depending on whether the request is intercepted by
 * Playwright mocks. Both shapes are handled transparently.
 */
export async function searchFamiliesForCheckin(
  request: CheckinSearchRequest
): Promise<CheckinFamilyDto[]> {
  const response = await post<{ data: Array<CheckinFamilySearchResultDto & Partial<CheckinFamilyDto>> }>(
    '/checkin/search',
    { searchValue: request.searchValue, searchType: request.searchType }
  );
  return response.data.map((item) => {
    // Backend format has familyIdKey; mock/frontend format has idKey directly
    if (item.familyIdKey) {
      return mapSearchResultToFamily(item as CheckinFamilySearchResultDto);
    }
    // Already in frontend DTO shape (from mocked responses)
    return item as unknown as CheckinFamilyDto;
  });
}

/**
 * Get available check-in opportunities for a family.
 * Backend returns CheckinFamilySearchResultDto shapes; map to frontend DTOs.
 */
export async function getCheckinOpportunities(
  familyIdKey: string,
  params: CheckinOpportunitiesParams = {}
): Promise<CheckinOpportunitiesResponse> {
  const queryParams = new URLSearchParams();

  if (params.scheduleId) queryParams.set('scheduleId', params.scheduleId);

  const query = queryParams.toString();
  const endpoint = `/checkin/opportunities/${familyIdKey}${query ? `?${query}` : ''}`;

  // Backend wraps in { data: { family: SearchResultDto, opportunities: [...] } }
  const response = await get<{ data: {
    family: CheckinFamilySearchResultDto;
    opportunities: Array<{
      person: CheckinFamilyMemberDto;
      currentAttendance: CheckinOpportunitiesResponse['opportunities'][0]['currentAttendance'];
      availableOptions: CheckinOpportunitiesResponse['opportunities'][0]['availableOptions'];
    }>;
  } }>(endpoint);

  const raw = response.data;
  return {
    family: mapSearchResultToFamily(raw.family),
    opportunities: raw.opportunities.map((opp) => ({
      person: mapMemberToPersonDto(opp.person),
      currentAttendance: opp.currentAttendance,
      availableOptions: opp.availableOptions,
    })),
  };
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
      // Backend "LocationIdKey" is actually the Group ID (check-in area)
      locationIdKey: item.groupIdKey,
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

  const response = await get<{ data: RoomRosterDto[] | RoomRosterDto }>(
    `/checkin/roster?${queryParams.toString()}`
  );
  // Normalize: API may return a single roster object or an array
  return Array.isArray(response.data) ? response.data : [response.data];
}

/**
 * Map a backend CheckinFamilyMemberDto (from registration) to the
 * CheckinPersonDto shape used throughout the kiosk flow.
 */
function mapMemberToPersonDto(member: CheckinFamilyMemberDto): CheckinPersonDto {
  return {
    idKey: member.personIdKey,
    firstName: member.firstName,
    nickName: member.nickName,
    lastName: member.lastName,
    fullName: member.fullName,
    age: member.age,
    grade: member.grade,
    photoUrl: member.photoUrl,
    lastCheckIn: member.lastCheckIn,
    allergies: member.allergies,
    hasCriticalAllergies: member.hasCriticalAllergies,
    specialNeeds: member.specialNeeds,
  };
}

/**
 * Map a backend CheckinFamilySearchResultDto (registration response) to the
 * CheckinFamilyDto shape expected by CheckinPage and downstream components.
 */
function mapSearchResultToFamily(result: CheckinFamilySearchResultDto): CheckinFamilyDto {
  return {
    idKey: result.familyIdKey,
    name: result.familyName,
    members: result.members.map(mapMemberToPersonDto),
  };
}

/**
 * Register a new family at the kiosk.
 *
 * Creates a family, parent, and one or more children in a single call.
 * The backend returns CheckinFamilySearchResultDto wrapped in { data: ... };
 * this function unwraps and maps it to CheckinFamilyDto so the kiosk can
 * immediately advance to the member selection step.
 */
export async function registerFamily(
  request: KioskFamilyRegistrationRequest
): Promise<CheckinFamilyDto> {
  const response = await post<{ data: CheckinFamilySearchResultDto }>(
    '/checkin/register-family',
    request
  );
  return mapSearchResultToFamily(response.data);
}

/**
 * Check out a child from the roster
 * Uses the existing attendance checkout endpoint
 */
export async function checkoutFromRoster(attendanceIdKey: string): Promise<void> {
  // Backend returns 204 No Content
  await post(`/checkin/checkout/${attendanceIdKey}`, undefined);
}
