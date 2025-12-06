/**
 * Base HTTP client for API communication
 * Uses native fetch with automatic token refresh and error handling
 */

import type { ApiError } from './types';

// ============================================================================
// Configuration
// ============================================================================

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api/v1';

// ============================================================================
// Token Storage (in memory, not localStorage for security)
// ============================================================================

let accessToken: string | null = null;
let refreshToken: string | null = null;
let tokenRefreshPromise: Promise<boolean> | null = null;

export function setTokens(access: string, refresh: string): void {
  accessToken = access;
  refreshToken = refresh;
}

export function clearTokens(): void {
  accessToken = null;
  refreshToken = null;
  tokenRefreshPromise = null;
}

export function getAccessToken(): string | null {
  return accessToken;
}

export function getRefreshToken(): string | null {
  return refreshToken;
}

// ============================================================================
// Error Handling
// ============================================================================

export class ApiClientError extends Error {
  constructor(
    public statusCode: number,
    public error: ApiError['error'],
    message?: string
  ) {
    super(message || error.message);
    this.name = 'ApiClientError';
  }
}

/**
 * Parse error response from API
 */
async function parseErrorResponse(response: Response): Promise<ApiError['error']> {
  const contentType = response.headers.get('content-type');

  if (contentType?.includes('application/json')) {
    try {
      const json = await response.json() as ApiError;
      return json.error;
    } catch {
      // Fall through to default error
    }
  }

  // Fallback for non-JSON errors
  const text = await response.text();
  return {
    code: 'UNKNOWN_ERROR',
    message: text || response.statusText || 'An unknown error occurred',
  };
}

// ============================================================================
// Token Refresh
// ============================================================================

/**
 * Attempt to refresh the access token using the refresh token
 * Returns true if successful, false otherwise
 */
async function tryRefreshToken(): Promise<boolean> {
  // If already refreshing, wait for that to complete
  if (tokenRefreshPromise) {
    return tokenRefreshPromise;
  }

  if (!refreshToken) {
    return false;
  }

  // Create the refresh promise
  tokenRefreshPromise = (async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ refreshToken }),
      });

      if (!response.ok) {
        clearTokens();
        return false;
      }

      const data = await response.json();
      accessToken = data.data.accessToken;
      return true;
    } catch {
      clearTokens();
      return false;
    } finally {
      tokenRefreshPromise = null;
    }
  })();

  return tokenRefreshPromise;
}

// ============================================================================
// HTTP Client
// ============================================================================

export interface ApiClientOptions extends RequestInit {
  skipAuth?: boolean;
}

/**
 * Make an authenticated API request
 * Automatically adds auth header and handles token refresh on 401
 */
export async function apiClient<T>(
  endpoint: string,
  options: ApiClientOptions = {}
): Promise<T> {
  const { skipAuth = false, ...requestInit } = options;
  const url = `${API_BASE_URL}${endpoint}`;

  const headers = new Headers(requestInit.headers);

  // Set content type for JSON requests
  if (requestInit.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  // Add auth header if token exists and not skipped
  if (!skipAuth && accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`);
  }

  // Make the request
  let response = await fetch(url, {
    ...requestInit,
    headers,
  });

  // Handle 401 - try to refresh token and retry
  if (response.status === 401 && !skipAuth && refreshToken) {
    const refreshed = await tryRefreshToken();

    if (refreshed && accessToken) {
      // Retry the request with new token
      headers.set('Authorization', `Bearer ${accessToken}`);
      response = await fetch(url, {
        ...requestInit,
        headers,
      });
    }
  }

  // Handle error responses
  if (!response.ok) {
    const error = await parseErrorResponse(response);
    throw new ApiClientError(response.status, error);
  }

  // Handle 204 No Content
  if (response.status === 204) {
    return undefined as T;
  }

  // Parse and return JSON response
  const contentType = response.headers.get('content-type');
  if (contentType?.includes('application/json')) {
    return response.json();
  }

  // For non-JSON responses, return text
  return response.text() as T;
}

// ============================================================================
// Convenience Methods
// ============================================================================

export async function get<T>(endpoint: string, options?: ApiClientOptions): Promise<T> {
  return apiClient<T>(endpoint, { ...options, method: 'GET' });
}

export async function post<T>(
  endpoint: string,
  body?: unknown,
  options?: ApiClientOptions
): Promise<T> {
  return apiClient<T>(endpoint, {
    ...options,
    method: 'POST',
    body: body ? JSON.stringify(body) : undefined,
  });
}

export async function put<T>(
  endpoint: string,
  body?: unknown,
  options?: ApiClientOptions
): Promise<T> {
  return apiClient<T>(endpoint, {
    ...options,
    method: 'PUT',
    body: body ? JSON.stringify(body) : undefined,
  });
}

export async function del<T>(endpoint: string, options?: ApiClientOptions): Promise<T> {
  return apiClient<T>(endpoint, { ...options, method: 'DELETE' });
}

export async function patch<T>(
  endpoint: string,
  body?: unknown,
  options?: ApiClientOptions
): Promise<T> {
  return apiClient<T>(endpoint, {
    ...options,
    method: 'PATCH',
    body: body ? JSON.stringify(body) : undefined,
  });
}
