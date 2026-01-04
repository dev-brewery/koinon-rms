/**
 * Koinon RMS API Client
 * Central export for all API services and types
 */

// Export client utilities
export {
  apiClient,
  get,
  post,
  put,
  del,
  patch,
  setTokens,
  clearTokens,
  getAccessToken,
  getRefreshToken,
  ApiClientError,
} from './client';

// Export all types
export * from './types';

// Export services
export * as analyticsApi from './analytics';
export * as auditLogApi from './auditLogApi';
export * as authApi from './auth';
export * as authorizedPickupApi from './authorizedPickup';
export * as campusesApi from './campuses';
export * as checkinApi from './checkin';
export * as communicationsApi from './communications';
export * as dashboardApi from './dashboard';
export * as familiesApi from './families';
export * as filesApi from './files';
export * as followupsApi from './followups';
export * as givingApi from './giving';
export * as groupsApi from './groups';
export * as groupTypesApi from './groupTypes';
export * as importApi from './import';
export * as locationsApi from './locations';
export * as membershipRequestsApi from './membershipRequests';
export * as myGroupsApi from './myGroups';
export * as pagerApi from './pager';
export * as peopleApi from './people';
export * as pickupApi from './pickup';
export * as profileApi from './profile';
export * as publicGroupsApi from './publicGroups';
export * as referenceApi from './reference';
export * as schedulesApi from './schedules';
export * as settingsApi from './settings';
