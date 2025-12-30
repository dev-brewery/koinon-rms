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
export * as authApi from './auth';
export * as peopleApi from './people';
export * as familiesApi from './families';
export * as groupsApi from './groups';
export * as checkinApi from './checkin';
export * as referenceApi from './reference';
export * as profileApi from './profile';
export * as authorizedPickupApi from './authorizedPickup';
export * as filesApi from './files';
export * as followupsApi from './followups';
export * as givingApi from './giving';
