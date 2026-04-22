/**
 * AuthContext tests
 *
 * Covers:
 *  - Hook guard rail (throws outside provider)
 *  - Initial state from localStorage (no token, valid token, expired token)
 *  - login success path (calls API, persists user, flips state)
 *  - login failure path (clears state, preserves error message, rethrows)
 *  - logout (calls API with refresh token, always clears local state)
 *  - logout with missing refresh token (skips API call, still clears state)
 *  - refreshAuth success (new tokens stored via setTokens)
 *  - refreshAuth failure (clears tokens/user, resets state)
 *  - refreshAuth with no refresh token (flips loading off without calling API)
 */

import { act, render, renderHook, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import { AuthProvider, useAuthContext } from '../AuthContext';

// Mock the API module so tests don't hit network.
vi.mock('../../services/api', () => {
  return {
    authApi: {
      login: vi.fn(),
      logout: vi.fn(),
      refresh: vi.fn(),
    },
    setTokens: vi.fn(),
    clearTokens: vi.fn(),
    getRefreshToken: vi.fn(),
  };
});

import * as apiModule from '../../services/api';

const { authApi, setTokens, clearTokens, getRefreshToken } = apiModule as unknown as {
  authApi: {
    login: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
    refresh: ReturnType<typeof vi.fn>;
  };
  setTokens: ReturnType<typeof vi.fn>;
  clearTokens: ReturnType<typeof vi.fn>;
  getRefreshToken: ReturnType<typeof vi.fn>;
};

const USER_STORAGE_KEY = 'koinon_user';
const ACCESS_TOKEN_KEY = 'koinon_access_token';

function fakeUser(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    idKey: 'USR1',
    userName: 'alice',
    fullName: 'Alice Example',
    email: 'alice@example.test',
    personIdKey: 'PER1',
    isSystemAdmin: false,
    ...overrides,
  };
}

// Build a JWT whose `exp` claim is `secondsFromNow` seconds from the current time.
function makeJwt(secondsFromNow: number): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString(
    'base64url',
  );
  const payload = Buffer.from(
    JSON.stringify({ sub: 'alice', exp: Math.floor(Date.now() / 1000) + secondsFromNow }),
  ).toString('base64url');
  return `${header}.${payload}.signature`;
}

function wrapper({ children }: { children: ReactNode }) {
  return <AuthProvider>{children}</AuthProvider>;
}

beforeEach(() => {
  localStorage.clear();
  vi.clearAllMocks();
});

afterEach(() => {
  localStorage.clear();
});

describe('useAuthContext', () => {
  it('throws if used outside AuthProvider', () => {
    // Silence the expected React error boundary log.
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {});
    expect(() => renderHook(() => useAuthContext())).toThrow(
      /useAuthContext must be used within AuthProvider/,
    );
    spy.mockRestore();
  });
});

describe('AuthProvider initial state', () => {
  it('defaults to unauthenticated when no token present', () => {
    const { result } = renderHook(() => useAuthContext(), { wrapper });
    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.user).toBeNull();
    expect(result.current.error).toBeNull();
  });

  it('starts authenticated when access token is still valid', () => {
    localStorage.setItem(ACCESS_TOKEN_KEY, makeJwt(3600));
    const storedUser = fakeUser();
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(storedUser));

    const { result } = renderHook(() => useAuthContext(), { wrapper });

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.user).toEqual(storedUser);
  });

  it('starts loading and attempts refresh when token is expired', async () => {
    localStorage.setItem(ACCESS_TOKEN_KEY, makeJwt(-60));
    getRefreshToken.mockReturnValue('refresh-abc');
    authApi.refresh.mockResolvedValue({
      accessToken: 'new-access',
      refreshToken: 'new-refresh',
    });

    const { result } = renderHook(() => useAuthContext(), { wrapper });

    // Initial synchronous state: loading until the effect resolves.
    expect(result.current.isLoading).toBe(true);

    await waitFor(() => expect(result.current.isLoading).toBe(false));
    expect(authApi.refresh).toHaveBeenCalledWith('refresh-abc');
    expect(setTokens).toHaveBeenCalledWith('new-access', 'new-refresh');
    expect(result.current.isAuthenticated).toBe(true);
  });

  it('treats a malformed token as no token (unauthenticated, not loading)', () => {
    localStorage.setItem(ACCESS_TOKEN_KEY, 'not-a-jwt');
    // Token is present but invalid → loading branch triggers a refresh attempt,
    // but with no refresh token the provider should settle to loading=false.
    getRefreshToken.mockReturnValue(null);

    const { result } = renderHook(() => useAuthContext(), { wrapper });

    // The malformed token triggers the "expired token" path → loading initially true,
    // then cleared by refreshAuth when there is no refresh token.
    return waitFor(() => expect(result.current.isLoading).toBe(false));
  });
});

describe('AuthProvider.login', () => {
  it('authenticates and persists user on success', async () => {
    const user = fakeUser({ userName: 'bob' });
    authApi.login.mockResolvedValue({
      accessToken: 'a',
      refreshToken: 'r',
      user,
    });

    const { result } = renderHook(() => useAuthContext(), { wrapper });

    await act(async () => {
      await result.current.login({ email: 'bob', password: 'pw' });
    });

    expect(authApi.login).toHaveBeenCalledWith({ email: 'bob', password: 'pw' });
    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user).toEqual(user);
    expect(result.current.error).toBeNull();
    expect(localStorage.getItem(USER_STORAGE_KEY)).toBe(JSON.stringify(user));
  });

  it('records error message and rethrows on login failure', async () => {
    authApi.login.mockRejectedValue(new Error('bad creds'));

    const { result } = renderHook(() => useAuthContext(), { wrapper });

    let caught: unknown;
    await act(async () => {
      try {
        await result.current.login({ email: 'bob', password: 'wrong' });
      } catch (e) {
        caught = e;
      }
    });

    expect((caught as Error)?.message).toBe('bad creds');
    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.user).toBeNull();
    expect(result.current.error).toBe('bad creds');
    expect(result.current.isLoading).toBe(false);
  });

  it('uses a fallback message when the login error is not an Error instance', async () => {
    authApi.login.mockRejectedValue('network down');

    const { result } = renderHook(() => useAuthContext(), { wrapper });

    await act(async () => {
      try {
        await result.current.login({ email: 'bob', password: 'x' });
      } catch {
        /* ignore — we're checking the state mutation */
      }
    });

    expect(result.current.error).toBe('Login failed');
  });
});

describe('AuthProvider.logout', () => {
  it('calls the API with the refresh token and clears local state', async () => {
    getRefreshToken.mockReturnValue('refresh-abc');
    authApi.logout.mockResolvedValue(undefined);

    // Seed authenticated state via login.
    authApi.login.mockResolvedValue({
      accessToken: 'a',
      refreshToken: 'r',
      user: fakeUser(),
    });

    const { result } = renderHook(() => useAuthContext(), { wrapper });
    await act(async () => {
      await result.current.login({ email: 'a', password: 'b' });
    });
    expect(result.current.isAuthenticated).toBe(true);

    await act(async () => {
      await result.current.logout();
    });

    expect(authApi.logout).toHaveBeenCalledWith('refresh-abc');
    expect(clearTokens).toHaveBeenCalled();
    expect(localStorage.getItem(USER_STORAGE_KEY)).toBeNull();
    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.user).toBeNull();
  });

  it('still clears state when the logout API call fails', async () => {
    getRefreshToken.mockReturnValue('refresh-abc');
    authApi.logout.mockRejectedValue(new Error('server down'));
    authApi.login.mockResolvedValue({
      accessToken: 'a',
      refreshToken: 'r',
      user: fakeUser(),
    });

    const { result } = renderHook(() => useAuthContext(), { wrapper });
    await act(async () => {
      await result.current.login({ email: 'a', password: 'b' });
    });

    let caught: unknown;
    await act(async () => {
      try {
        await result.current.logout();
      } catch (e) {
        caught = e;
      }
    });

    expect((caught as Error)?.message).toBe('server down');
    expect(clearTokens).toHaveBeenCalled();
    expect(result.current.isAuthenticated).toBe(false);
  });

  it('skips the API call entirely when no refresh token is present', async () => {
    getRefreshToken.mockReturnValue(null);

    const { result } = renderHook(() => useAuthContext(), { wrapper });
    await act(async () => {
      await result.current.logout();
    });

    expect(authApi.logout).not.toHaveBeenCalled();
    expect(clearTokens).toHaveBeenCalled();
    expect(result.current.isAuthenticated).toBe(false);
  });
});

describe('AuthProvider.refreshAuth', () => {
  it('stores rotated tokens and keeps the existing user', async () => {
    getRefreshToken.mockReturnValue('refresh-abc');
    authApi.refresh.mockResolvedValue({
      accessToken: 'new-access',
      refreshToken: 'new-refresh',
    });

    // Seed authenticated state so we can verify user is preserved across refresh.
    const seededUser = fakeUser({ userName: 'carol' });
    authApi.login.mockResolvedValue({
      accessToken: 'a',
      refreshToken: 'r',
      user: seededUser,
    });

    const { result } = renderHook(() => useAuthContext(), { wrapper });
    await act(async () => {
      await result.current.login({ email: 'carol', password: 'pw' });
    });

    await act(async () => {
      await result.current.refreshAuth();
    });

    expect(authApi.refresh).toHaveBeenCalledWith('refresh-abc');
    expect(setTokens).toHaveBeenCalledWith('new-access', 'new-refresh');
    expect(result.current.user).toEqual(seededUser); // preserved
    expect(result.current.isAuthenticated).toBe(true);
  });

  it('clears tokens and user when refresh fails', async () => {
    getRefreshToken.mockReturnValue('refresh-abc');
    authApi.refresh.mockRejectedValue(new Error('expired'));

    const { result } = renderHook(() => useAuthContext(), { wrapper });

    await act(async () => {
      await result.current.refreshAuth();
    });

    expect(clearTokens).toHaveBeenCalled();
    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.user).toBeNull();
  });

  it('no-ops quickly when there is no refresh token', async () => {
    getRefreshToken.mockReturnValue(null);

    const { result } = renderHook(() => useAuthContext(), { wrapper });

    await act(async () => {
      await result.current.refreshAuth();
    });

    expect(authApi.refresh).not.toHaveBeenCalled();
    expect(result.current.isLoading).toBe(false);
  });
});

describe('AuthProvider rendering', () => {
  it('exposes context to children via useAuthContext', () => {
    function Probe() {
      const ctx = useAuthContext();
      return <div data-testid="auth-state">{String(ctx.isAuthenticated)}</div>;
    }

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>,
    );
    expect(screen.getByTestId('auth-state').textContent).toBe('false');
  });
});
