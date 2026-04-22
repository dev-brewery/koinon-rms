/**
 * useMembershipRequests — group membership approval flow.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/membershipRequests', () => ({
  getPendingRequests: vi.fn(),
  submitMembershipRequest: vi.fn(),
  processRequest: vi.fn(),
}));

import * as api from '@/services/api/membershipRequests';
import {
  usePendingRequests,
  useProcessRequest,
  useSubmitMembershipRequest,
} from '../useMembershipRequests';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('usePendingRequests', () => {
  it('idle without groupIdKey', () => {
    const { result } = renderHookWithQuery(() => usePendingRequests());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fires with groupIdKey', async () => {
    vi.mocked(api.getPendingRequests).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => usePendingRequests('g1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getPendingRequests).toHaveBeenCalledWith('g1');
  });
});

describe('useSubmitMembershipRequest', () => {
  it('calls api with groupIdKey and invalidates group-scoped keys', async () => {
    vi.mocked(api.submitMembershipRequest).mockResolvedValueOnce({ idKey: 'r1' } as never);
    const { result, client } = renderHookWithQuery(() => useSubmitMembershipRequest('g1'));
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ note: 'x' } as never);
    });
    expect(api.submitMembershipRequest).toHaveBeenCalledWith('g1', { note: 'x' });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['groups', 'g1'] });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['groups', 'g1', 'membership-requests'] });
  });
});

describe('useProcessRequest', () => {
  it('calls api and invalidates requests + members', async () => {
    vi.mocked(api.processRequest).mockResolvedValueOnce({ idKey: 'r1' } as never);
    const { result, client } = renderHookWithQuery(() => useProcessRequest('g1'));
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({
        requestIdKey: 'r1',
        request: { approve: true } as never,
      });
    });
    expect(api.processRequest).toHaveBeenCalledWith('g1', 'r1', { approve: true });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['groups', 'g1', 'membership-requests'] });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['groups', 'g1', 'members'] });
  });
});
