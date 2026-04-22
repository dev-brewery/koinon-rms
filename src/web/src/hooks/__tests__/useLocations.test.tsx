/**
 * useLocations / useLocationTree / useLocation / CRUD mutations.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/locations', () => ({
  getLocations: vi.fn(),
  getLocationTree: vi.fn(),
  getLocation: vi.fn(),
  createLocation: vi.fn(),
  updateLocation: vi.fn(),
  deleteLocation: vi.fn(),
}));

import * as locationsApi from '@/services/api/locations';
import {
  useCreateLocation,
  useDeleteLocation,
  useLocation,
  useLocations,
  useLocationTree,
  useUpdateLocation,
} from '../useLocations';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useLocations', () => {
  it('passes options to api', async () => {
    vi.mocked(locationsApi.getLocations).mockResolvedValueOnce([] as never);
    const opts = { campusIdKey: 'c1', includeInactive: true };
    const { result } = renderHookWithQuery(() => useLocations(opts));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(locationsApi.getLocations).toHaveBeenCalledWith(opts);
  });

  it('defaults to no options', async () => {
    vi.mocked(locationsApi.getLocations).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useLocations());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(locationsApi.getLocations).toHaveBeenCalledWith(undefined);
  });
});

describe('useLocationTree', () => {
  it('fires with optional options', async () => {
    vi.mocked(locationsApi.getLocationTree).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useLocationTree({ campusIdKey: 'c1' }));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(locationsApi.getLocationTree).toHaveBeenCalledWith({ campusIdKey: 'c1' });
  });
});

describe('useLocation', () => {
  it('idle without idKey', () => {
    const { result } = renderHookWithQuery(() => useLocation());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fires with idKey', async () => {
    vi.mocked(locationsApi.getLocation).mockResolvedValueOnce({ idKey: 'l1' } as never);
    const { result } = renderHookWithQuery(() => useLocation('l1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(locationsApi.getLocation).toHaveBeenCalledWith('l1');
  });
});

describe('CRUD mutations', () => {
  it('create invalidates locations', async () => {
    vi.mocked(locationsApi.createLocation).mockResolvedValueOnce({ idKey: 'l1' } as never);
    const { result, client } = renderHookWithQuery(() => useCreateLocation());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ name: 'Room A' } as never);
    });
    expect(locationsApi.createLocation).toHaveBeenCalledWith({ name: 'Room A' });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['locations'] });
  });

  it('update invalidates', async () => {
    vi.mocked(locationsApi.updateLocation).mockResolvedValueOnce({ idKey: 'l1' } as never);
    const { result, client } = renderHookWithQuery(() => useUpdateLocation());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ idKey: 'l1', request: { name: 'Y' } as never });
    });
    expect(locationsApi.updateLocation).toHaveBeenCalledWith('l1', { name: 'Y' });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['locations'] });
  });

  it('delete invalidates', async () => {
    vi.mocked(locationsApi.deleteLocation).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useDeleteLocation());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('l1');
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['locations'] });
  });
});
