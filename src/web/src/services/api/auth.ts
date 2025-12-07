/**
 * Authentication API service
 */

import { post, setTokens, clearTokens } from './client';
import { TokenResponseSchema, RefreshResponseSchema, ValidateSupervisorPinResponseSchema, parseWithSchema } from './validators';
import type {
  LoginRequest,
  TokenResponse,
  RefreshRequest,
  RefreshResponse,
  LogoutRequest,
  ValidateSupervisorPinRequest,
  ValidateSupervisorPinResponse,
} from './types';

/**
 * Login with username and password
 * Returns access token, refresh token, and user info
 */
export async function login(request: LoginRequest): Promise<TokenResponse> {
  const response = await post<{ data: unknown }>('/auth/login', request, {
    skipAuth: true,
  });

  // Validate the response
  const validated = parseWithSchema(TokenResponseSchema, response.data, 'login');

  // Store tokens in memory
  setTokens(validated.accessToken, validated.refreshToken);

  return validated;
}

/**
 * Refresh an expired access token using refresh token
 */
export async function refresh(refreshToken: string): Promise<RefreshResponse> {
  const request: RefreshRequest = { refreshToken };
  const response = await post<{ data: unknown }>('/auth/refresh', request, {
    skipAuth: true,
  });

  // Validate the response
  const validated = parseWithSchema(RefreshResponseSchema, response.data, 'refresh');

  return validated;
}

/**
 * Logout and invalidate refresh token
 */
export async function logout(refreshTokenValue: string): Promise<void> {
  try {
    const request: LogoutRequest = { refreshToken: refreshTokenValue };
    await post<void>('/auth/logout', request);
  } finally {
    // Clear tokens from memory even if API call fails
    clearTokens();
  }
}

/**
 * Clear local tokens without calling API
 * Use when token is already invalid or for quick logout
 */
export function clearSession(): void {
  clearTokens();
}

/**
 * Validate supervisor PIN for elevated access
 * Returns validation result with supervisor details if valid
 * Includes timing attack protection via minimum response delay
 */
export async function validateSupervisorPin(pin: string): Promise<ValidateSupervisorPinResponse> {
  const MIN_RESPONSE_DELAY = 500; // Minimum 500ms to mask timing differences
  const startTime = Date.now();

  try {
    const request: ValidateSupervisorPinRequest = { pin };
    const response = await post<{ data: unknown }>('/auth/supervisor/validate', request, {
      skipAuth: true,
    });

    // Validate the response
    const validated = parseWithSchema(ValidateSupervisorPinResponseSchema, response.data, 'validateSupervisorPin');
    return validated;
  } finally {
    // ALWAYS enforce minimum response delay to prevent timing attacks
    const elapsed = Date.now() - startTime;
    if (elapsed < MIN_RESPONSE_DELAY) {
      await new Promise(resolve => setTimeout(resolve, MIN_RESPONSE_DELAY - elapsed));
    }
  }
}
