/**
 * useCampuses / useCampus / useCreateCampus / useUpdateCampus / useDeleteCampus.
 *
 * These hooks are thin TanStack Query wrappers around `services/api/campuses`.
 * The behaviors worth locking in are:
 *   - Queries execute on mount and eventually resolve with the mock value.
 *   - `useCampus(idKey)` is idle (disabled) until an idKey is supplied.
 *   - Mutations call through to the right API function with the right args.
 *   - After a mutation resolves, invalidation happens at the `['campuses']`
 *     key so downstream lists refetch.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/campuses', () => ({
  getCampuses: vi.fn(),
  getCampus: vi.fn(),
  createCampus: vi.fn(),
  updateCampus: vi.fn(),
  deleteCampus: vi.fn(),
}));

import * as campusesApi from '@/services/api/campuses';
import {
  useCampus,
  useCampuses,
  useCreateCampus,
  useDeleteCampus,
  useUpdateCampus,
} from '../useCampuses';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

const getCampusesMock = vi.mocked(campusesApi.getCampuses);
const getCampusMock = vi.mocked(campusesApi.getCampus);
const createCampusMock = vi.mocked(campusesApi.createCampus);
const updateCampusMock = vi.mocked(campusesApi.updateCampus);
const deleteCampusMock = vi.mocked(campusesApi.deleteCampus);

beforeEach(() => {
  vi.clearAllMocks();
});

describe('useCampuses', () => {
  it('fetches campuses with includeInactive=false by default', async () => {
    getCampusesMock.mockResolvedValueOnce([{ idKey: 'c1', name: 'Main' }] as never);
    const { result } = renderHookWithQuery(() => useCampuses());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(getCampusesMock).toHaveBeenCalledWith(false);
    expect(result.current.data).toEqual([{ idKey: 'c1', name: 'Main' }]);
  });

  it('passes includeInactive=true when requested', async () => {
    getCampusesMock.mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useCampuses(true));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(getCampusesMock).toHaveBeenCalledWith(true);
  });

  it('surfaces fetch errors as isError', async () => {
    getCampusesMock.mockRejectedValueOnce(new Error('boom'));
    const { result } = renderHookWithQuery(() => useCampuses());
    await waitFor(() => expect(result.current.isError).toBe(true));
    expect((result.current.error as Error).message).toBe('boom');
  });
});

describe('useCampus', () => {
  it('is disabled when idKey is undefined', () => {
    const { result } = renderHookWithQuery(() => useCampus(undefined));
    // react-query v5: disabled queries have fetchStatus='idle', status='pending'
    expect(result.current.fetchStatus).toBe('idle');
    expect(getCampusMock).not.toHaveBeenCalled();
  });

  it('executes when idKey is provided', async () => {
    getCampusMock.mockResolvedValueOnce({ idKey: 'c1' } as never);
    const { result } = renderHookWithQuery(() => useCampus('c1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(getCampusMock).toHaveBeenCalledWith('c1');
  });
});

describe('useCreateCampus', () => {
  it('calls createCampus and invalidates campuses list', async () => {
    createCampusMock.mockResolvedValueOnce({ idKey: 'new' } as never);
    const { result, client } = renderHookWithQuery(() => useCreateCampus());
    const invalidate = vi.spyOn(client, 'invalidateQueries');

    await act(async () => {
      await result.current.mutateAsync({ name: 'North' } as never);
    });

    expect(createCampusMock).toHaveBeenCalledWith({ name: 'North' });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['campuses'] });
  });
});

describe('useUpdateCampus', () => {
  it('forwards idKey + request and invalidates', async () => {
    updateCampusMock.mockResolvedValueOnce({ idKey: 'c1' } as never);
    const { result, client } = renderHookWithQuery(() => useUpdateCampus());
    const invalidate = vi.spyOn(client, 'invalidateQueries');

    await act(async () => {
      await result.current.mutateAsync({ idKey: 'c1', request: { name: 'x' } as never });
    });

    expect(updateCampusMock).toHaveBeenCalledWith('c1', { name: 'x' });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['campuses'] });
  });
});

describe('useDeleteCampus', () => {
  it('deletes by idKey and invalidates', async () => {
    deleteCampusMock.mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useDeleteCampus());
    const invalidate = vi.spyOn(client, 'invalidateQueries');

    await act(async () => {
      await result.current.mutateAsync('c1');
    });

    expect(deleteCampusMock).toHaveBeenCalledWith('c1');
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['campuses'] });
  });
});
