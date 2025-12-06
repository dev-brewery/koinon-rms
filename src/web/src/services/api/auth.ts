/**
 * Authentication API service
 */

import { post, setTokens, clearTokens } from './client';
import type {
  LoginRequest,
  TokenResponse,
  RefreshRequest,
  RefreshResponse,
  LogoutRequest,
} from './types';

/**
 * Login with username and password
 * Returns access token, refresh token, and user info
 */
export async function login(request: LoginRequest): Promise<TokenResponse> {
  const response = await post<{ data: TokenResponse }>('/auth/login', request, {
    skipAuth: true,
  });

  // Store tokens in memory
  setTokens(response.data.accessToken, response.data.refreshToken);

  return response.data;
}

/**
 * Refresh an expired access token using refresh token
 */
export async function refresh(refreshToken: string): Promise<RefreshResponse> {
  const request: RefreshRequest = { refreshToken };
  const response = await post<{ data: RefreshResponse }>('/auth/refresh', request, {
    skipAuth: true,
  });

  return response.data;
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
