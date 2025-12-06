/**
 * Authentication Context
 * Provides authentication state and methods throughout the application
 */

import { createContext, useContext, useState, useCallback, useEffect, ReactNode } from 'react';
import { authApi, setTokens, clearTokens, getAccessToken, getRefreshToken } from '../services/api';
import type { LoginRequest, UserSummaryDto } from '../services/api/types';

// ============================================================================
// Types
// ============================================================================

interface AuthState {
  user: UserSummaryDto | null;
  isAuthenticated: boolean;
  isLoading: boolean;
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
  });

  /**
   * Login with username and password
   * Stores tokens in memory and sets user state
   */
  const login = useCallback(async (request: LoginRequest) => {
    const response = await authApi.login(request);

    setState({
      user: response.user,
      isAuthenticated: true,
      isLoading: false,
    });
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
      });
    }
  }, []);

  /**
   * Refresh authentication state
   * Attempts to refresh token and validate session
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

      // Token refreshed successfully, but we need user info
      // For now, mark as authenticated but user data will be fetched separately
      setState({
        user: null, // User data should be fetched from /me endpoint
        isAuthenticated: true,
        isLoading: false,
      });
    } catch {
      clearTokens();
      setState({
        user: null,
        isAuthenticated: false,
        isLoading: false,
      });
    }
  }, []);

  /**
   * Check for existing authentication on mount
   * Attempts to refresh token if one exists
   */
  useEffect(() => {
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
  }, [refreshAuth]);

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
export function useAuthContext() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuthContext must be used within AuthProvider');
  }

  return context;
}
