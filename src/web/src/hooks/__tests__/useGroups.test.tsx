/**
 * useGroups + related hooks.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/groups', () => ({
  searchGroups: vi.fn(),
  getGroupByIdKey: vi.fn(),
  getChildGroups: vi.fn(),
  createGroup: vi.fn(),
  updateGroup: vi.fn(),
  deleteGroup: vi.fn(),
  getGroupSchedules: vi.fn(),
  addGroupSchedule: vi.fn(),
  removeGroupSchedule: vi.fn(),
  getGroupAttendanceHistory: vi.fn(),
  getGroupAttendanceDetail: vi.fn(),
}));

import * as groupsApi from '@/services/api/groups';
import {
  useAddGroupSchedule,
  useChildGroups,
  useCreateGroup,
  useDeleteGroup,
  useGroup,
  useGroupAttendanceDetail,
  useGroupAttendanceHistory,
  useGroupSchedules,
  useGroups,
  useRemoveGroupSchedule,
  useUpdateGroup,
} from '../useGroups';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useGroups / useGroup / useChildGroups', () => {
  it('list search forwards params', async () => {
    vi.mocked(groupsApi.searchGroups).mockResolvedValueOnce({ data: [] } as never);
    const { result } = renderHookWithQuery(() => useGroups({ q: 'x' }));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(groupsApi.searchGroups).toHaveBeenCalledWith({ q: 'x' });
  });

  it('useGroup is idle without idKey', () => {
    const { result } = renderHookWithQuery(() => useGroup());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('useGroup fires on idKey', async () => {
    vi.mocked(groupsApi.getGroupByIdKey).mockResolvedValueOnce({ idKey: 'g1' } as never);
    const { result } = renderHookWithQuery(() => useGroup('g1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(groupsApi.getGroupByIdKey).toHaveBeenCalledWith('g1');
  });

  it('useChildGroups idle without parent', () => {
    const { result } = renderHookWithQuery(() => useChildGroups());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('useChildGroups fires on parent', async () => {
    vi.mocked(groupsApi.getChildGroups).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useChildGroups('g1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(groupsApi.getChildGroups).toHaveBeenCalledWith('g1');
  });
});

describe('group CRUD mutations', () => {
  it('create invalidates groups list', async () => {
    vi.mocked(groupsApi.createGroup).mockResolvedValueOnce({ idKey: 'g1' } as never);
    const { result, client } = renderHookWithQuery(() => useCreateGroup());
    const invalidate = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ name: 'X' } as never);
    });
    expect(groupsApi.createGroup).toHaveBeenCalledWith({ name: 'X' });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['groups'] });
  });

  it('update invalidates specific + list', async () => {
    vi.mocked(groupsApi.updateGroup).mockResolvedValueOnce({ idKey: 'g1' } as never);
    const { result, client } = renderHookWithQuery(() => useUpdateGroup());
    const invalidate = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ idKey: 'g1', request: { name: 'Y' } as never });
    });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['groups', 'g1'] });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['groups'] });
  });

  it('delete invalidates list', async () => {
    vi.mocked(groupsApi.deleteGroup).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useDeleteGroup());
    const invalidate = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('g1');
    });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['groups'] });
  });
});

describe('group schedules', () => {
  it('useGroupSchedules idle without groupIdKey', () => {
    const { result } = renderHookWithQuery(() => useGroupSchedules());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('useGroupSchedules fires on groupIdKey', async () => {
    vi.mocked(groupsApi.getGroupSchedules).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useGroupSchedules('g1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(groupsApi.getGroupSchedules).toHaveBeenCalledWith('g1');
  });

  it('add / remove schedule invalidate the schedule key', async () => {
    vi.mocked(groupsApi.addGroupSchedule).mockResolvedValueOnce({ idKey: 's1' } as never);
    vi.mocked(groupsApi.removeGroupSchedule).mockResolvedValueOnce(undefined as never);
    const add = renderHookWithQuery(() => useAddGroupSchedule());
    const rm = renderHookWithQuery(() => useRemoveGroupSchedule());
    const invAdd = vi.spyOn(add.client, 'invalidateQueries');
    const invRm = vi.spyOn(rm.client, 'invalidateQueries');

    await act(async () => {
      await add.result.current.mutateAsync({
        groupIdKey: 'g1',
        request: { scheduleIdKey: 's1' } as never,
      });
    });
    expect(invAdd).toHaveBeenCalledWith({ queryKey: ['groups', 'g1', 'schedules'] });

    await act(async () => {
      await rm.result.current.mutateAsync({ groupIdKey: 'g1', scheduleIdKey: 's1' });
    });
    expect(invRm).toHaveBeenCalledWith({ queryKey: ['groups', 'g1', 'schedules'] });
  });
});

describe('group attendance queries', () => {
  it('useGroupAttendanceHistory idle without groupIdKey', () => {
    const { result } = renderHookWithQuery(() => useGroupAttendanceHistory());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('useGroupAttendanceDetail idle until BOTH keys present', async () => {
    const onlyGroup = renderHookWithQuery(() => useGroupAttendanceDetail('g1'));
    expect(onlyGroup.result.current.fetchStatus).toBe('idle');

    vi.mocked(groupsApi.getGroupAttendanceDetail).mockResolvedValueOnce([] as never);
    const both = renderHookWithQuery(() => useGroupAttendanceDetail('g1', 'o1'));
    await waitFor(() => expect(both.result.current.isSuccess).toBe(true));
    expect(groupsApi.getGroupAttendanceDetail).toHaveBeenCalledWith('g1', 'o1');
  });
});
