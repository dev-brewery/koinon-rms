/**
 * Security roles admin hooks.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/securityRoles', () => ({
  getSecurityRoles: vi.fn(),
  getSecurityRole: vi.fn(),
  getSecurityClaims: vi.fn(),
  createSecurityRole: vi.fn(),
  updateSecurityRole: vi.fn(),
  deleteSecurityRole: vi.fn(),
  assignRoleClaim: vi.fn(),
  removeRoleClaim: vi.fn(),
  addRoleMember: vi.fn(),
  removeRoleMember: vi.fn(),
}));

import * as api from '@/services/api/securityRoles';
import {
  useAddRoleMember,
  useAssignRoleClaim,
  useCreateSecurityRole,
  useDeleteSecurityRole,
  useRemoveRoleClaim,
  useRemoveRoleMember,
  useSecurityClaims,
  useSecurityRole,
  useSecurityRoles,
  useUpdateSecurityRole,
} from '../useSecurityRoles';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('query hooks', () => {
  it('useSecurityRoles fetches', async () => {
    vi.mocked(api.getSecurityRoles).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useSecurityRoles());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getSecurityRoles).toHaveBeenCalled();
  });

  it('useSecurityRole idle without idKey', () => {
    const { result } = renderHookWithQuery(() => useSecurityRole());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('useSecurityRole fires with idKey', async () => {
    vi.mocked(api.getSecurityRole).mockResolvedValueOnce({ idKey: 'r1' } as never);
    const { result } = renderHookWithQuery(() => useSecurityRole('r1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getSecurityRole).toHaveBeenCalledWith('r1');
  });

  it('useSecurityClaims fetches', async () => {
    vi.mocked(api.getSecurityClaims).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useSecurityClaims());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
  });
});

describe('mutations', () => {
  it('create invalidates roles', async () => {
    vi.mocked(api.createSecurityRole).mockResolvedValueOnce({ idKey: 'r1' } as never);
    const { result, client } = renderHookWithQuery(() => useCreateSecurityRole());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ name: 'Admin' } as never);
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['security-roles'] });
  });

  it('update invalidates both list and specific', async () => {
    vi.mocked(api.updateSecurityRole).mockResolvedValueOnce({ idKey: 'r1' } as never);
    const { result, client } = renderHookWithQuery(() => useUpdateSecurityRole());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ idKey: 'r1', request: { name: 'N' } as never });
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['security-roles'] });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['security-roles', 'r1'] });
  });

  it('delete invalidates', async () => {
    vi.mocked(api.deleteSecurityRole).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useDeleteSecurityRole());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('r1');
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['security-roles'] });
  });

  it('claim assign/remove invalidate list + specific', async () => {
    vi.mocked(api.assignRoleClaim).mockResolvedValueOnce(undefined as never);
    vi.mocked(api.removeRoleClaim).mockResolvedValueOnce(undefined as never);
    const assign = renderHookWithQuery(() => useAssignRoleClaim());
    const remove = renderHookWithQuery(() => useRemoveRoleClaim());
    const invA = vi.spyOn(assign.client, 'invalidateQueries');
    const invR = vi.spyOn(remove.client, 'invalidateQueries');

    await act(async () => {
      await assign.result.current.mutateAsync({
        idKey: 'r1',
        request: { claimIdKey: 'c1' } as never,
      });
    });
    expect(api.assignRoleClaim).toHaveBeenCalledWith('r1', { claimIdKey: 'c1' });
    expect(invA).toHaveBeenCalledWith({ queryKey: ['security-roles', 'r1'] });

    await act(async () => {
      await remove.result.current.mutateAsync({ idKey: 'r1', claimIdKey: 'c1' });
    });
    expect(api.removeRoleClaim).toHaveBeenCalledWith('r1', 'c1');
    expect(invR).toHaveBeenCalledWith({ queryKey: ['security-roles', 'r1'] });
  });

  it('member add/remove invalidate list + specific', async () => {
    vi.mocked(api.addRoleMember).mockResolvedValueOnce(undefined as never);
    vi.mocked(api.removeRoleMember).mockResolvedValueOnce(undefined as never);
    const add = renderHookWithQuery(() => useAddRoleMember());
    const rm = renderHookWithQuery(() => useRemoveRoleMember());
    const invA = vi.spyOn(add.client, 'invalidateQueries');
    const invR = vi.spyOn(rm.client, 'invalidateQueries');

    await act(async () => {
      await add.result.current.mutateAsync({
        idKey: 'r1',
        request: { personIdKey: 'p1' } as never,
      });
    });
    expect(api.addRoleMember).toHaveBeenCalledWith('r1', { personIdKey: 'p1' });
    expect(invA).toHaveBeenCalledWith({ queryKey: ['security-roles', 'r1'] });

    await act(async () => {
      await rm.result.current.mutateAsync({ idKey: 'r1', personIdKey: 'p1' });
    });
    expect(api.removeRoleMember).toHaveBeenCalledWith('r1', 'p1');
    expect(invR).toHaveBeenCalledWith({ queryKey: ['security-roles', 'r1'] });
  });
});
