/**
 * Authentication Hooks
 * Convenience hooks for accessing authentication state
 */

import { useAuthContext } from '../contexts/AuthContext';

/**
 * Get full authentication context
 * Includes user, authentication status, and auth methods
 */
export function useAuth() {
  return useAuthContext();
}

/**
 * Get current user
 * Returns null if not authenticated
 */
export function useUser() {
  const { user } = useAuthContext();
  return user;
}

/**
 * Get authentication status
 * Returns both authentication state and loading state
 */
export function useIsAuthenticated() {
  const { isAuthenticated, isLoading } = useAuthContext();
  return { isAuthenticated, isLoading };
}

/**
 * Returns true if the current user has the 'Admin' role.
 * Roles are extracted from the JWT claims at login time and stored on the
 * user object — no token re-parsing on every render.
 */
export function useIsAdmin(): boolean {
  const { user, isAuthenticated } = useAuthContext();
  return isAuthenticated && (user?.roles?.includes('Admin') ?? false);
}
