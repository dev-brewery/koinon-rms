/**
 * useGroupTypes admin hooks.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/groupTypes', () => ({
  getGroupTypes: vi.fn(),
  getGroupType: vi.fn(),
  createGroupType: vi.fn(),
  updateGroupType: vi.fn(),
  archiveGroupType: vi.fn(),
  getGroupsByType: vi.fn(),
}));

import * as api from '@/services/api/groupTypes';
import {
  useArchiveGroupType,
  useCreateGroupType,
  useGroupType,
  useGroupTypes,
  useGroupsByType,
  useUpdateGroupType,
} from '../useGroupTypes';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useGroupTypes', () => {
  it('defaults includeArchived to false', async () => {
    vi.mocked(api.getGroupTypes).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useGroupTypes());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getGroupTypes).toHaveBeenCalledWith(false);
  });

  it('passes true when requested', async () => {
    vi.mocked(api.getGroupTypes).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useGroupTypes(true));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getGroupTypes).toHaveBeenCalledWith(true);
  });
});

describe('useGroupType', () => {
  it('idle without idKey', () => {
    const { result } = renderHookWithQuery(() => useGroupType());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fires with idKey', async () => {
    vi.mocked(api.getGroupType).mockResolvedValueOnce({ idKey: 'gt1' } as never);
    const { result } = renderHookWithQuery(() => useGroupType('gt1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getGroupType).toHaveBeenCalledWith('gt1');
  });
});

describe('useGroupsByType', () => {
  it('idle without idKey', () => {
    const { result } = renderHookWithQuery(() => useGroupsByType());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fires with idKey', async () => {
    vi.mocked(api.getGroupsByType).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useGroupsByType('gt1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getGroupsByType).toHaveBeenCalledWith('gt1');
  });
});

describe('create / update / archive', () => {
  it('create invalidates', async () => {
    vi.mocked(api.createGroupType).mockResolvedValueOnce({ idKey: 'gt1' } as never);
    const { result, client } = renderHookWithQuery(() => useCreateGroupType());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ name: 'N' } as never);
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['group-types'] });
  });

  it('update invalidates', async () => {
    vi.mocked(api.updateGroupType).mockResolvedValueOnce({ idKey: 'gt1' } as never);
    const { result, client } = renderHookWithQuery(() => useUpdateGroupType());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ idKey: 'gt1', request: { name: 'X' } as never });
    });
    expect(api.updateGroupType).toHaveBeenCalledWith('gt1', { name: 'X' });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['group-types'] });
  });

  it('archive invalidates', async () => {
    vi.mocked(api.archiveGroupType).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useArchiveGroupType());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('gt1');
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['group-types'] });
  });
});
