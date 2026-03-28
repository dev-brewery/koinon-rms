/**
 * Authentication Context
 * Provides authentication state and methods throughout the application
 */

import { createContext, useContext, useState, useCallback, useEffect, ReactNode } from 'react';
import { authApi, setTokens, clearTokens, getRefreshToken } from '../services/api';
import type { LoginRequest, UserSummaryDto } from '../services/api/types';

// ============================================================================
// Types
// ============================================================================

interface AuthState {
  user: UserSummaryDto | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

interface AuthContextValue extends AuthState {
  login: (request: LoginRequest) => Promise<void>;
  logout: () => Promise<void>;
  refreshAuth: () => Promise<void>;
}

// ============================================================================
// User Storage (localStorage for persistence across page loads)
// ============================================================================

const USER_STORAGE_KEY = 'koinon_user';

function saveUser(user: UserSummaryDto): void {
  try {
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(user));
  } catch {
    // Silently fail if localStorage is unavailable
  }
}

function loadUser(): UserSummaryDto | null {
  try {
    const stored = localStorage.getItem(USER_STORAGE_KEY);
    return stored ? JSON.parse(stored) : null;
  } catch {
    return null;
  }
}

function clearUser(): void {
  try {
    localStorage.removeItem(USER_STORAGE_KEY);
  } catch {
    // Silently fail
  }
}

// ============================================================================
// Token Expiry Check
// ============================================================================

/**
 * Decode a base64url-encoded string (used by JWT).
 * Converts base64url to standard base64 before decoding.
 */
function base64UrlDecode(str: string): string {
  // Replace base64url characters with standard base64
  let base64 = str.replace(/-/g, '+').replace(/_/g, '/');
  // Add padding if needed
  const pad = base64.length % 4;
  if (pad === 2) base64 += '==';
  else if (pad === 3) base64 += '=';
  return atob(base64);
}

/**
 * Check if a JWT access token is still valid (not expired).
 * Returns true if the token has at least 30 seconds of validity remaining.
 */
function isTokenValid(token: string): boolean {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return false;

    const payload = JSON.parse(base64UrlDecode(parts[1]));
    if (!payload.exp) return false;

    // Token is valid if it expires more than 30 seconds from now
    const expiresAt = payload.exp * 1000; // Convert to milliseconds
    return expiresAt > Date.now() + 30000;
  } catch {
    return false;
  }
}

// ============================================================================
// Context
// ============================================================================

const AuthContext = createContext<AuthContextValue | null>(null);

// ============================================================================
// Provider Component
// ============================================================================

interface AuthProviderProps {
  children: ReactNode;
}

/**
 * Compute initial auth state synchronously from localStorage.
 * If a valid access token exists, we start as authenticated immediately,
 * avoiding the flash where ProtectedRoute would redirect to /login.
 */
function getInitialAuthState(): AuthState {
  try {
    const token = localStorage.getItem('koinon_access_token');
    if (token && isTokenValid(token)) {
      const user = loadUser();
      return {
        user,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      };
    }
    // Token exists but is expired - need async refresh
    if (token) {
      return {
        user: null,
        isAuthenticated: false,
        isLoading: true,
        error: null,
      };
    }
  } catch {
    // localStorage unavailable
  }
  // No token at all - not authenticated, done loading
  return {
    user: null,
    isAuthenticated: false,
    isLoading: false,
    error: null,
  };
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [state, setState] = useState<AuthState>(getInitialAuthState);

  /**
   * Login with username and password
   * Stores tokens in memory and sets user state
   */
  const login = useCallback(async (request: LoginRequest) => {
    setState(prev => ({ ...prev, isLoading: true, error: null }));

    try {
      const response = await authApi.login(request);

      // Persist user info for page reloads
      saveUser(response.user);

      setState({
        user: response.user,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      });
    } catch (error) {
      setState({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: error instanceof Error ? error.message : 'Login failed',
      });
      throw error; // Re-throw so LoginForm can handle it
    }
  }, []);

  /**
   * Logout user and clear session
   * Calls API to invalidate refresh token
   */
  const logout = useCallback(async () => {
    try {
      const token = getRefreshToken();
      if (token) {
        await authApi.logout(token);
      }
    } finally {
      clearTokens();
      clearUser();
      setState({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: null,
      });
    }
  }, []);

  /**
   * Refresh authentication state
   * Attempts to refresh token and validate session
   * IMPORTANT: Preserves existing user state - refresh token endpoint doesn't return user data
   */
  const refreshAuth = useCallback(async () => {
    const token = getRefreshToken();

    if (!token) {
      setState(prev => ({ ...prev, isLoading: false }));
      return;
    }

    try {
      const response = await authApi.refresh(token);

      // Store the new tokens from the response (token rotation)
      setTokens(response.accessToken, response.refreshToken);

      // Keep existing user data - refresh endpoint doesn't return user info
      setState(prev => ({
        ...prev,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      }));
    } catch {
      clearTokens();
      clearUser();
      setState({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: null,
      });
    }
  }, []);

  /**
   * Handle expired token refresh on mount.
   * Valid tokens are handled synchronously in getInitialAuthState.
   * This effect only runs when we have an expired token that needs refreshing.
   */
  useEffect(() => {
    // Only attempt refresh if we're still in loading state
    // (meaning getInitialAuthState found an expired token)
    if (!state.isLoading) return;

    const attemptRefresh = async () => {
      await refreshAuth();
    };

    attemptRefresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const value: AuthContextValue = {
    ...state,
    login,
    logout,
    refreshAuth,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
}

// ============================================================================
// Hook
// ============================================================================

/**
 * Use authentication context
 * Must be used within AuthProvider
 */
// eslint-disable-next-line react-refresh/only-export-components
export function useAuthContext() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuthContext must be used within AuthProvider');
  }

  return context;
}
