/**
 * Base HTTP client for API communication
 * Uses native fetch with automatic token refresh and error handling
 */

import type { ApiError, ProblemDetails } from './types';
import {
  RefreshResponseSchema,
  parseWithSchema,
  safeJsonParse,
  parseErrorResponse as parseErrorResponseHelper,
} from './validators';
import { isNetworkError } from '../../lib/networkUtils';

// ============================================================================
// Configuration
// ============================================================================

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api/v1';
const DEFAULT_TIMEOUT_MS = 10000; // 10 seconds
const UPLOAD_TIMEOUT_MS = 60000; // 60 seconds for uploads

// ============================================================================
// Token Storage (localStorage for persistence across page loads)
// ============================================================================

const ACCESS_TOKEN_KEY = 'koinon_access_token';
const REFRESH_TOKEN_KEY = 'koinon_refresh_token';

let tokenRefreshPromise: Promise<boolean> | null = null;

// Initialize tokens from localStorage on module load
let accessToken: string | null = null;
let refreshToken: string | null = null;

try {
  accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);
  refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
} catch (error) {
  if (import.meta.env.DEV) {
    console.warn('Failed to load tokens from localStorage:', error);
  }
}

export function setTokens(access: string, refresh: string): void {
  accessToken = access;
  refreshToken = refresh;

  try {
    localStorage.setItem(ACCESS_TOKEN_KEY, access);
    localStorage.setItem(REFRESH_TOKEN_KEY, refresh);
  } catch (error) {
    if (import.meta.env.DEV) {
      console.error('Failed to save tokens to localStorage:', error);
    }
  }
}

export function clearTokens(): void {
  accessToken = null;
  refreshToken = null;
  tokenRefreshPromise = null;

  try {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  } catch (error) {
    if (import.meta.env.DEV) {
      console.error('Failed to clear tokens from localStorage:', error);
    }
  }
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
  public readonly format: 'problemDetails' | 'legacy';
  public readonly problemDetails?: ProblemDetails;
  public readonly legacyError?: ApiError['error'];
  public readonly traceId?: string;

  constructor(
    public statusCode: number,
    errorData: ProblemDetails | ApiError['error'],
    format: 'problemDetails' | 'legacy' = 'legacy',
    message?: string
  ) {
    // Determine message based on format
    let errorMessage = message;
    if (!errorMessage) {
      if (format === 'problemDetails' && 'detail' in errorData) {
        errorMessage = errorData.detail || 'An error occurred';
      } else if ('message' in errorData) {
        errorMessage = errorData.message;
      } else {
        errorMessage = 'An error occurred';
      }
    }

    super(errorMessage);
    this.name = 'ApiClientError';
    this.format = format;

    if (format === 'problemDetails' && 'detail' in errorData) {
      this.problemDetails = errorData as ProblemDetails;
      this.traceId = errorData.traceId;
    } else {
      this.legacyError = errorData as ApiError['error'];
      this.traceId = (errorData as ApiError['error']).traceId;
    }
  }

  /**
   * Get the error for backward compatibility with legacy code
   */
  get error(): ApiError['error'] {
    if (this.legacyError) {
      return this.legacyError;
    }

    // Convert ProblemDetails to legacy format for backwards compatibility
    if (this.problemDetails) {
      return {
        code: this.problemDetails.type?.split('/').pop() || 'ERROR',
        message: this.problemDetails.detail || this.message,
        traceId: this.problemDetails.traceId,
        details: this.problemDetails.extensions?.errors as Record<string, string[]> | undefined,
      };
    }

    // Fallback
    return {
      code: 'UNKNOWN_ERROR',
      message: this.message,
    };
  }
}

/**
 * Parse error response from API - supports both ProblemDetails and legacy formats
 */
async function parseErrorResponse(response: Response): Promise<ApiClientError> {
  const contentType = response.headers.get('content-type');

  if (contentType?.includes('application/json')) {
    try {
      const text = await response.text();
      const json = safeJsonParse(text);

      if (json) {
        const { format, error } = parseErrorResponseHelper(json);
        return new ApiClientError(response.status, error, format);
      }
    } catch (error) {
      if (import.meta.env.DEV) {
        console.error('Failed to parse error response:', {
          error,
          status: response.status,
          statusText: response.statusText,
        });
      }
      // Fall through to default error
    }
  }

  // Fallback for non-JSON errors
  let text = '';
  try {
    text = await response.text();
  } catch (error) {
    if (import.meta.env.DEV) {
      console.error('Failed to read error response text:', error);
    }
  }

  return new ApiClientError(
    response.status,
    {
      code: 'UNKNOWN_ERROR',
      message: text || response.statusText || 'An unknown error occurred',
    },
    'legacy'
  );
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
    if (import.meta.env.DEV) {
      console.warn('Cannot refresh token: no refresh token available');
    }
    return false;
  }

  // Create the refresh promise
  tokenRefreshPromise = (async () => {
    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), DEFAULT_TIMEOUT_MS);

      const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ refreshToken }),
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      if (!response.ok) {
        if (import.meta.env.DEV) {
          console.error('Token refresh failed:', {
            status: response.status,
            statusText: response.statusText,
          });
        }
        clearTokens();
        return false;
      }

      const text = await response.text();
      const json = safeJsonParse(text);

      if (!json) {
        if (import.meta.env.DEV) {
          console.error('Token refresh response is not valid JSON');
        }
        clearTokens();
        return false;
      }

      // Validate the response structure
      const data = json as { data?: unknown };
      if (!data.data) {
        if (import.meta.env.DEV) {
          console.error('Token refresh response missing data envelope');
        }
        clearTokens();
        return false;
      }

      const validated = parseWithSchema(
        RefreshResponseSchema,
        data.data,
        'token refresh'
      );

      accessToken = validated.accessToken;
      refreshToken = validated.refreshToken;

      if (import.meta.env.DEV) {
        console.info('Token refresh successful');
      }
      return true;
    } catch (error) {
      if (import.meta.env.DEV) {
        if (error instanceof Error) {
          if (error.name === 'AbortError') {
            console.error('Token refresh timeout');
          } else {
            console.error('Token refresh error:', {
              message: error.message,
              name: error.name,
            });
          }
        } else {
          console.error('Token refresh unknown error:', error);
        }
      }
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
  timeout?: number; // Custom timeout in milliseconds
}

/**
 * Determine appropriate timeout based on request type
 */
function getRequestTimeout(method?: string, customTimeout?: number): number {
  if (customTimeout !== undefined) {
    return customTimeout;
  }

  // Use longer timeout for mutation operations
  if (method === 'POST' || method === 'PUT' || method === 'PATCH') {
    return UPLOAD_TIMEOUT_MS;
  }

  return DEFAULT_TIMEOUT_MS;
}

/**
 * Make an authenticated API request
 * Automatically adds auth header and handles token refresh on 401
 */
export async function apiClient<T>(
  endpoint: string,
  options: ApiClientOptions = {}
): Promise<T> {
  const { skipAuth = false, timeout, ...requestInit } = options;
  const url = `${API_BASE_URL}${endpoint}`;

  // Determine timeout based on request type
  const timeoutMs = getRequestTimeout(requestInit.method, timeout);

  // Create AbortController for timeout
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

  const headers = new Headers(requestInit.headers);

  // Set content type for JSON requests
  if (requestInit.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  // Add auth header if token exists and not skipped
  if (!skipAuth && accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`);
  }

  try {
    // Make the request
    let response = await fetch(url, {
      ...requestInit,
      headers,
      signal: controller.signal,
    });

    clearTimeout(timeoutId);

    // Handle 401 - try to refresh token and retry
    if (response.status === 401 && !skipAuth && refreshToken) {
      if (import.meta.env.DEV) {
        console.info('Received 401, attempting token refresh');
      }
      const refreshed = await tryRefreshToken();

      if (refreshed && accessToken) {
        // Retry the request with new token
        if (import.meta.env.DEV) {
          console.info('Retrying request with new token');
        }
        headers.set('Authorization', `Bearer ${accessToken}`);

        // Create new timeout for retry
        const retryController = new AbortController();
        const retryTimeoutId = setTimeout(() => retryController.abort(), timeoutMs);

        response = await fetch(url, {
          ...requestInit,
          headers,
          signal: retryController.signal,
        });

        clearTimeout(retryTimeoutId);
      } else {
        if (import.meta.env.DEV) {
          console.warn('Token refresh failed, request will fail with 401');
        }
      }
    }

    // Handle error responses
    if (!response.ok) {
      const error = await parseErrorResponse(response);
      if (import.meta.env.DEV) {
        console.error('API request failed:', {
          endpoint,
          status: response.status,
          error,
          traceId: error.traceId,
        });
      }
      throw error;
    }

    // Handle 204 No Content
    if (response.status === 204) {
      return undefined as T;
    }

    // Parse and return JSON response
    const contentType = response.headers.get('content-type');
    if (contentType?.includes('application/json')) {
      const text = await response.text();
      const json = safeJsonParse(text);

      if (json === null) {
        if (import.meta.env.DEV) {
          console.error('Response is not valid JSON:', {
            endpoint,
            contentType,
            preview: text.substring(0, 200),
          });
        }
        throw new Error('Invalid JSON response from server');
      }

      // Note: Callers should validate the specific response type
      // This ensures we at least have valid JSON
      return json as T;
    }

    // For non-JSON responses, return text
    const text = await response.text();
    return text as T;
  } catch (error) {
    clearTimeout(timeoutId);

    if (error instanceof ApiClientError) {
      // Re-throw API errors
      throw error;
    }

    if (error instanceof Error) {
      if (error.name === 'AbortError') {
        if (import.meta.env.DEV) {
          console.error('API request timeout:', {
            endpoint,
            timeout: timeoutMs,
          });
        }
        throw new ApiClientError(
          408,
          {
            code: 'REQUEST_TIMEOUT',
            message: `Request timeout after ${timeoutMs}ms`,
          },
          'legacy',
          'Request timeout'
        );
      }

      // Check for network errors using centralized function
      if (isNetworkError(error)) {
        if (import.meta.env.DEV) {
          console.error('Network error:', {
            endpoint,
            message: error.message,
          });
        }
        throw new ApiClientError(
          0,
          {
            code: 'NETWORK_ERROR',
            message: 'Network connection failed',
          },
          'legacy',
          'Network error'
        );
      }

      if (import.meta.env.DEV) {
        console.error('API request error:', {
          endpoint,
          message: error.message,
          name: error.name,
        });
      }

      // Re-throw the error as-is to preserve the message
      throw error;
    }

    if (import.meta.env.DEV) {
      console.error('API request unknown error:', {
        endpoint,
        error,
      });
    }

    throw new ApiClientError(
      0,
      {
        code: 'UNKNOWN_ERROR',
        message: 'An unknown error occurred',
      },
      'legacy',
      'Unknown error'
    );
  }
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
