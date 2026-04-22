/**
 * features/followups/hooks.ts
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('../api', () => ({
  getPendingFollowUps: vi.fn(),
  getFollowUp: vi.fn(),
  updateFollowUpStatus: vi.fn(),
  assignFollowUp: vi.fn(),
  FollowUpStatus: { Pending: 0, Completed: 1 },
}));

import * as api from '../api';
import {
  useAssignFollowUp,
  useFollowUp,
  usePendingFollowUps,
  useUpdateFollowUpStatus,
} from '../hooks';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('queries', () => {
  it('usePendingFollowUps without assignee', async () => {
    vi.mocked(api.getPendingFollowUps).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => usePendingFollowUps());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getPendingFollowUps).toHaveBeenCalledWith(undefined);
  });

  it('usePendingFollowUps with assignee', async () => {
    vi.mocked(api.getPendingFollowUps).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => usePendingFollowUps('u1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getPendingFollowUps).toHaveBeenCalledWith('u1');
  });

  it('useFollowUp fetches by idKey', async () => {
    vi.mocked(api.getFollowUp).mockResolvedValueOnce({ idKey: 'f1' } as never);
    const { result } = renderHookWithQuery(() => useFollowUp('f1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getFollowUp).toHaveBeenCalledWith('f1');
  });
});

describe('useUpdateFollowUpStatus', () => {
  it('sets detail cache and invalidates all followups', async () => {
    const updated = { idKey: 'f1', status: 1 };
    vi.mocked(api.updateFollowUpStatus).mockResolvedValueOnce(updated as never);
    const { result, client } = renderHookWithQuery(() => useUpdateFollowUpStatus());
    const setData = vi.spyOn(client, 'setQueryData');
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ idKey: 'f1', status: 1, notes: 'n' } as never);
    });
    expect(api.updateFollowUpStatus).toHaveBeenCalledWith('f1', 1, 'n');
    expect(setData).toHaveBeenCalledWith(['followups', 'detail', 'f1'], updated);
    expect(inv).toHaveBeenCalledWith({ queryKey: ['followups'] });
  });
});

describe('useAssignFollowUp', () => {
  it('invalidates all followups', async () => {
    vi.mocked(api.assignFollowUp).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useAssignFollowUp());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ idKey: 'f1', assignedToIdKey: 'u1' });
    });
    expect(api.assignFollowUp).toHaveBeenCalledWith('f1', 'u1');
    expect(inv).toHaveBeenCalledWith({ queryKey: ['followups'] });
  });
});
