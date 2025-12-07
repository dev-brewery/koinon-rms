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
  RecordAttendanceRequest,
  RecordAttendanceResponse,
  CheckoutRequest,
  CheckoutResponse,
  LabelDto,
  LabelParams,
  SupervisorLoginRequest,
  SupervisorLoginResponse,
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
 * Record check-in attendance for one or more people
 */
export async function recordAttendance(
  request: RecordAttendanceRequest
): Promise<RecordAttendanceResponse> {
  const response = await post<{ data: RecordAttendanceResponse }>(
    '/checkin/attendance',
    request
  );
  return response.data;
}

/**
 * Record check-out for an attendance record
 */
export async function checkout(attendanceIdKey: string): Promise<CheckoutResponse> {
  const request: CheckoutRequest = { attendanceIdKey };
  const response = await post<{ data: CheckoutResponse }>('/checkin/checkout', request);
  return response.data;
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
