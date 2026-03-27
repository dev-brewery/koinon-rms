/**
 * Authentication Context
 * Provides authentication state and methods throughout the application
 */

import { createContext, useContext, useState, useCallback, useEffect, useRef, ReactNode } from 'react';
import { authApi, setTokens, clearTokens, getAccessToken, getRefreshToken } from '../services/api';
import type { LoginRequest, UserSummaryDto } from '../services/api/types';

// ============================================================================
// JWT Utilities (private to this module)
// ============================================================================

/**
 * Extract the roles claim from a JWT access token.
 * ASP.NET Core emits roles under either the standard claim URI or the
 * short-form "role" / "roles" key.  Returns an empty array on any failure.
 */
function extractRolesFromJwt(token: string): string[] {
  try {
    const payloadBase64 = token.split('.')[1];
    if (!payloadBase64) return [];
    const padded = payloadBase64.replace(/-/g, '+').replace(/_/g, '/');
    const payload: unknown = JSON.parse(atob(padded));
    if (typeof payload !== 'object' || payload === null) return [];
    const p = payload as Record<string, unknown>;
    const raw =
      p['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
      p['role'] ??
      p['roles'];
    if (Array.isArray(raw)) return raw.filter((r): r is string => typeof r === 'string');
    if (typeof raw === 'string') return [raw];
    return [];
  } catch {
    return [];
  }
}

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
// Context
// ============================================================================

const AuthContext = createContext<AuthContextValue | null>(null);

// ============================================================================
// Provider Component
// ============================================================================

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [state, setState] = useState<AuthState>({
    user: null,
    isAuthenticated: false,
    isLoading: true,
    error: null,
  });

  // Track if we've already checked auth on mount to prevent race conditions
  const hasCheckedAuth = useRef(false);

  /**
   * Login with username and password
   * Stores tokens in memory and sets user state
   */
  const login = useCallback(async (request: LoginRequest) => {
    setState(prev => ({ ...prev, isLoading: true, error: null }));

    try {
      const response = await authApi.login(request);

      // Merge roles from the JWT into the user object.  The API user payload
      // may omit roles, but the access token always carries them as claims.
      const roles = extractRolesFromJwt(response.accessToken);
      const userWithRoles: UserSummaryDto = { ...response.user, roles };

      setState({
        user: userWithRoles,
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

      // FIX: Keep existing user data - refresh endpoint doesn't return user info
      // The user was set during login and remains valid
      setState(prev => ({
        ...prev,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      }));
    } catch {
      clearTokens();
      setState({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: null,
      });
    }
  }, []);

  /**
   * Check for existing authentication on mount
   * Attempts to refresh token if one exists
   */
  useEffect(() => {
    // Prevent race condition - only check auth once on mount
    if (hasCheckedAuth.current) {
      return;
    }

    hasCheckedAuth.current = true;

    const checkAuth = async () => {
      const token = getAccessToken();

      if (token) {
        // Try to refresh to validate token
        await refreshAuth();
      } else {
        setState(prev => ({ ...prev, isLoading: false }));
      }
    };

    checkAuth();
    // Empty dependency array - only run once on mount
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
