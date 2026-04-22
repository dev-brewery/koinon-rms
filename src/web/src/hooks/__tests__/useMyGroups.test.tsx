/**
 * useMyGroups — hooks for group leaders managing their groups.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/myGroups', () => ({
  getMyGroups: vi.fn(),
  getMyGroupMembers: vi.fn(),
  updateGroupMember: vi.fn(),
  removeGroupMember: vi.fn(),
  recordAttendance: vi.fn(),
}));

import * as api from '@/services/api/myGroups';
import {
  useMyGroupMembers,
  useMyGroups,
  useRecordAttendance,
  useRemoveGroupMember,
  useUpdateGroupMember,
} from '../useMyGroups';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useMyGroups', () => {
  it('fetches my groups', async () => {
    vi.mocked(api.getMyGroups).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useMyGroups());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getMyGroups).toHaveBeenCalled();
  });
});

describe('useMyGroupMembers', () => {
  it('idle without groupIdKey', () => {
    const { result } = renderHookWithQuery(() => useMyGroupMembers());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fires with groupIdKey', async () => {
    vi.mocked(api.getMyGroupMembers).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useMyGroupMembers('g1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getMyGroupMembers).toHaveBeenCalledWith('g1');
  });
});

describe('useUpdateGroupMember / useRemoveGroupMember', () => {
  it('update calls api and invalidates members/groups', async () => {
    vi.mocked(api.updateGroupMember).mockResolvedValueOnce({} as never);
    const { result, client } = renderHookWithQuery(() => useUpdateGroupMember('g1'));
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({
        memberIdKey: 'm1',
        data: { status: 'Active' } as never,
      });
    });
    expect(api.updateGroupMember).toHaveBeenCalledWith('g1', 'm1', { status: 'Active' });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['my-groups', 'g1', 'members'] });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['my-groups'] });
  });

  it('remove calls api and invalidates', async () => {
    vi.mocked(api.removeGroupMember).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useRemoveGroupMember('g1'));
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('m1');
    });
    expect(api.removeGroupMember).toHaveBeenCalledWith('g1', 'm1');
    expect(inv).toHaveBeenCalledWith({ queryKey: ['my-groups', 'g1', 'members'] });
  });
});

describe('useRecordAttendance', () => {
  it('calls api and invalidates my-groups + group attendance', async () => {
    vi.mocked(api.recordAttendance).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useRecordAttendance('g1'));
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ attendance: [] } as never);
    });
    expect(api.recordAttendance).toHaveBeenCalledWith('g1', { attendance: [] });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['my-groups'] });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['groups', 'g1', 'attendance'] });
  });
});
