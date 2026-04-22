/**
 * useProfile (my-profile) hooks.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/profile', () => ({
  getMyProfile: vi.fn(),
  updateMyProfile: vi.fn(),
  getMyFamily: vi.fn(),
  updateFamilyMember: vi.fn(),
  getMyInvolvement: vi.fn(),
}));

import * as api from '@/services/api/profile';
import {
  useMyFamily,
  useMyInvolvement,
  useMyProfile,
  useUpdateFamilyMember,
  useUpdateMyProfile,
} from '../useProfile';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useMyProfile / update', () => {
  it('useMyProfile fetches', async () => {
    vi.mocked(api.getMyProfile).mockResolvedValueOnce({ idKey: 'u1' } as never);
    const { result } = renderHookWithQuery(() => useMyProfile());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getMyProfile).toHaveBeenCalled();
  });

  it('useUpdateMyProfile invalidates profile', async () => {
    vi.mocked(api.updateMyProfile).mockResolvedValueOnce({ idKey: 'u1' } as never);
    const { result, client } = renderHookWithQuery(() => useUpdateMyProfile());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ firstName: 'A' } as never);
    });
    expect(api.updateMyProfile).toHaveBeenCalledWith({ firstName: 'A' });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['profile', 'me'] });
  });
});

describe('family', () => {
  it('useMyFamily fetches', async () => {
    vi.mocked(api.getMyFamily).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useMyFamily());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getMyFamily).toHaveBeenCalled();
  });

  it('useUpdateFamilyMember invalidates family list', async () => {
    vi.mocked(api.updateFamilyMember).mockResolvedValueOnce({ idKey: 'p1' } as never);
    const { result, client } = renderHookWithQuery(() => useUpdateFamilyMember());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({
        personIdKey: 'p1',
        data: { firstName: 'X' } as never,
      });
    });
    expect(api.updateFamilyMember).toHaveBeenCalledWith('p1', { firstName: 'X' });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['profile', 'me', 'family'] });
  });
});

describe('involvement', () => {
  it('useMyInvolvement fetches', async () => {
    vi.mocked(api.getMyInvolvement).mockResolvedValueOnce({ groups: [] } as never);
    const { result } = renderHookWithQuery(() => useMyInvolvement());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getMyInvolvement).toHaveBeenCalled();
  });
});
