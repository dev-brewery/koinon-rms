/**
 * useSchedules + detail/occurrences/CRUD hooks.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/schedules', () => ({
  searchSchedules: vi.fn(),
  getScheduleByIdKey: vi.fn(),
  getScheduleOccurrences: vi.fn(),
  createSchedule: vi.fn(),
  updateSchedule: vi.fn(),
  deleteSchedule: vi.fn(),
}));

import * as schedulesApi from '@/services/api/schedules';
import {
  useCreateSchedule,
  useDeleteSchedule,
  useSchedule,
  useScheduleOccurrences,
  useSchedules,
  useUpdateSchedule,
} from '../useSchedules';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useSchedules', () => {
  it('forwards params', async () => {
    vi.mocked(schedulesApi.searchSchedules).mockResolvedValueOnce({ data: [] } as never);
    const { result } = renderHookWithQuery(() => useSchedules({ query: 's' }));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(schedulesApi.searchSchedules).toHaveBeenCalledWith({ query: 's' });
  });
});

describe('useSchedule', () => {
  it('idle without idKey', () => {
    const { result } = renderHookWithQuery(() => useSchedule());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fires with idKey', async () => {
    vi.mocked(schedulesApi.getScheduleByIdKey).mockResolvedValueOnce({ idKey: 's1' } as never);
    const { result } = renderHookWithQuery(() => useSchedule('s1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(schedulesApi.getScheduleByIdKey).toHaveBeenCalledWith('s1');
  });
});

describe('useScheduleOccurrences', () => {
  it('idle without idKey', () => {
    const { result } = renderHookWithQuery(() => useScheduleOccurrences());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('passes idKey/startDate/count to api; count defaults to 10', async () => {
    vi.mocked(schedulesApi.getScheduleOccurrences).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useScheduleOccurrences('s1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(schedulesApi.getScheduleOccurrences).toHaveBeenCalledWith('s1', undefined, 10);
  });

  it('respects custom startDate and count', async () => {
    vi.mocked(schedulesApi.getScheduleOccurrences).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() =>
      useScheduleOccurrences('s1', '2024-01-01', 20)
    );
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(schedulesApi.getScheduleOccurrences).toHaveBeenCalledWith('s1', '2024-01-01', 20);
  });
});

describe('schedule mutations', () => {
  it('create invalidates schedules', async () => {
    vi.mocked(schedulesApi.createSchedule).mockResolvedValueOnce({ idKey: 's1' } as never);
    const { result, client } = renderHookWithQuery(() => useCreateSchedule());
    const invalidate = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ name: 'N' } as never);
    });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['schedules'] });
  });

  it('update invalidates specific + list', async () => {
    vi.mocked(schedulesApi.updateSchedule).mockResolvedValueOnce({ idKey: 's1' } as never);
    const { result, client } = renderHookWithQuery(() => useUpdateSchedule());
    const invalidate = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ idKey: 's1', request: { name: 'N' } as never });
    });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['schedules', 's1'] });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['schedules'] });
  });

  it('delete invalidates list', async () => {
    vi.mocked(schedulesApi.deleteSchedule).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useDeleteSchedule());
    const invalidate = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('s1');
    });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['schedules'] });
  });
});
