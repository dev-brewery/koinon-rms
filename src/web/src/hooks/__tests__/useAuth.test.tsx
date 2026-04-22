/**
 * useAuth / useUser / useIsAuthenticated — thin re-exports of AuthContext.
 *
 * We stub `useAuthContext` at the context module so the tests only exercise
 * the hook forwarding logic (not AuthContext itself, which has its own tests).
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook } from '@testing-library/react';

const mockUseAuthContext = vi.fn();
vi.mock('../../contexts/AuthContext', () => ({
  useAuthContext: () => mockUseAuthContext(),
}));

import { useAuth, useIsAuthenticated, useUser } from '../useAuth';

beforeEach(() => {
  mockUseAuthContext.mockReset();
});

describe('useAuth', () => {
  it('returns the whole context value', () => {
    const ctx = {
      user: { idKey: 'u' },
      isAuthenticated: true,
      isLoading: false,
      error: null,
      login: vi.fn(),
      logout: vi.fn(),
      refreshAuth: vi.fn(),
    };
    mockUseAuthContext.mockReturnValue(ctx);
    const { result } = renderHook(() => useAuth());
    expect(result.current).toBe(ctx);
  });
});

describe('useUser', () => {
  it('returns the user from context', () => {
    mockUseAuthContext.mockReturnValue({ user: { idKey: 'u1' } });
    const { result } = renderHook(() => useUser());
    expect(result.current).toEqual({ idKey: 'u1' });
  });

  it('returns null when not authenticated', () => {
    mockUseAuthContext.mockReturnValue({ user: null });
    const { result } = renderHook(() => useUser());
    expect(result.current).toBeNull();
  });
});

describe('useIsAuthenticated', () => {
  it('returns only isAuthenticated+isLoading (not the full user)', () => {
    mockUseAuthContext.mockReturnValue({
      isAuthenticated: true,
      isLoading: false,
      user: { idKey: 'u1' },
    });
    const { result } = renderHook(() => useIsAuthenticated());
    expect(result.current).toEqual({ isAuthenticated: true, isLoading: false });
    expect(result.current).not.toHaveProperty('user');
  });
});
