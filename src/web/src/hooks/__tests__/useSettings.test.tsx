/**
 * useSettings hook module: preferences, sessions, password, 2FA.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/settings', () => ({
  getPreferences: vi.fn(),
  updatePreferences: vi.fn(),
  getSessions: vi.fn(),
  revokeSession: vi.fn(),
  changePassword: vi.fn(),
  getTwoFactorStatus: vi.fn(),
  setupTwoFactor: vi.fn(),
  verifyTwoFactor: vi.fn(),
  disableTwoFactor: vi.fn(),
}));

import * as api from '@/services/api/settings';
import {
  useChangePassword,
  useDisableTwoFactor,
  usePreferences,
  useRevokeSession,
  useSessions,
  useSetupTwoFactor,
  useTwoFactorStatus,
  useUpdatePreferences,
  useVerifyTwoFactor,
} from '../useSettings';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('preferences', () => {
  it('usePreferences fetches', async () => {
    vi.mocked(api.getPreferences).mockResolvedValueOnce({ theme: 'dark' } as never);
    const { result } = renderHookWithQuery(() => usePreferences());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getPreferences).toHaveBeenCalled();
  });

  it('useUpdatePreferences invalidates preferences', async () => {
    vi.mocked(api.updatePreferences).mockResolvedValueOnce({ theme: 'light' } as never);
    const { result, client } = renderHookWithQuery(() => useUpdatePreferences());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ theme: 'light' } as never);
    });
    expect(api.updatePreferences).toHaveBeenCalledWith({ theme: 'light' });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['settings', 'preferences'] });
  });
});

describe('sessions', () => {
  it('useSessions fetches', async () => {
    vi.mocked(api.getSessions).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useSessions());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getSessions).toHaveBeenCalled();
  });

  it('useRevokeSession invalidates sessions', async () => {
    vi.mocked(api.revokeSession).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useRevokeSession());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('s1');
    });
    expect(api.revokeSession).toHaveBeenCalledWith('s1');
    expect(inv).toHaveBeenCalledWith({ queryKey: ['settings', 'sessions'] });
  });
});

describe('security', () => {
  it('useChangePassword calls API', async () => {
    vi.mocked(api.changePassword).mockResolvedValueOnce(undefined as never);
    const { result } = renderHookWithQuery(() => useChangePassword());
    await act(async () => {
      await result.current.mutateAsync({ currentPassword: 'a', newPassword: 'b' } as never);
    });
    expect(api.changePassword).toHaveBeenCalledWith({
      currentPassword: 'a',
      newPassword: 'b',
    });
  });

  it('useTwoFactorStatus fetches', async () => {
    vi.mocked(api.getTwoFactorStatus).mockResolvedValueOnce({ isEnabled: false } as never);
    const { result } = renderHookWithQuery(() => useTwoFactorStatus());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getTwoFactorStatus).toHaveBeenCalled();
  });

  it('useSetupTwoFactor triggers generation', async () => {
    vi.mocked(api.setupTwoFactor).mockResolvedValueOnce({ qrCode: 'x' } as never);
    const { result } = renderHookWithQuery(() => useSetupTwoFactor());
    await act(async () => {
      await result.current.mutateAsync();
    });
    expect(api.setupTwoFactor).toHaveBeenCalled();
  });

  it('useVerifyTwoFactor invalidates status', async () => {
    vi.mocked(api.verifyTwoFactor).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useVerifyTwoFactor());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('123456');
    });
    expect(api.verifyTwoFactor).toHaveBeenCalledWith('123456');
    expect(inv).toHaveBeenCalledWith({ queryKey: ['settings', 'two-factor', 'status'] });
  });

  it('useDisableTwoFactor invalidates status', async () => {
    vi.mocked(api.disableTwoFactor).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useDisableTwoFactor());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('123456');
    });
    expect(api.disableTwoFactor).toHaveBeenCalledWith('123456');
    expect(inv).toHaveBeenCalledWith({ queryKey: ['settings', 'two-factor', 'status'] });
  });
});
