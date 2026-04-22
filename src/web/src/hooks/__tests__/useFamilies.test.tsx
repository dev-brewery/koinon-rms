/**
 * useFamilies and siblings: query hooks wrapping services/api/families.
 *
 * Guards the boundary between UI and API:
 *   - useFamilies passes full param object through to `searchFamilies`.
 *   - useFamily(idKey) is idle until idKey is supplied.
 *   - Mutations (create/update/addMember/removeMember) invalidate the right
 *     query keys so lists and detail pages refetch.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/families', () => ({
  searchFamilies: vi.fn(),
  getFamilyByIdKey: vi.fn(),
  createFamily: vi.fn(),
  updateFamily: vi.fn(),
  addFamilyMember: vi.fn(),
  removeFamilyMember: vi.fn(),
}));

import * as familiesApi from '@/services/api/families';
import {
  useAddFamilyMember,
  useCreateFamily,
  useFamilies,
  useFamily,
  useRemoveFamilyMember,
  useUpdateFamily,
} from '../useFamilies';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

const searchMock = vi.mocked(familiesApi.searchFamilies);
const getMock = vi.mocked(familiesApi.getFamilyByIdKey);
const createMock = vi.mocked(familiesApi.createFamily);
const updateMock = vi.mocked(familiesApi.updateFamily);
const addMemberMock = vi.mocked(familiesApi.addFamilyMember);
const removeMemberMock = vi.mocked(familiesApi.removeFamilyMember);

beforeEach(() => vi.clearAllMocks());

describe('useFamilies', () => {
  it('forwards params to searchFamilies', async () => {
    searchMock.mockResolvedValueOnce({ data: [] } as never);
    const params = { q: 'Smith', page: 2 };
    const { result } = renderHookWithQuery(() => useFamilies(params));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(searchMock).toHaveBeenCalledWith(params);
  });
});

describe('useFamily', () => {
  it('is idle when idKey is falsy', () => {
    const { result } = renderHookWithQuery(() => useFamily(undefined));
    expect(result.current.fetchStatus).toBe('idle');
    expect(getMock).not.toHaveBeenCalled();
  });

  it('executes when idKey is provided', async () => {
    getMock.mockResolvedValueOnce({ idKey: 'f1' } as never);
    const { result } = renderHookWithQuery(() => useFamily('f1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(getMock).toHaveBeenCalledWith('f1');
    expect(result.current.data).toEqual({ idKey: 'f1' });
  });
});

describe('useCreateFamily', () => {
  it('creates and invalidates families list', async () => {
    createMock.mockResolvedValueOnce({ idKey: 'new' } as never);
    const { result, client } = renderHookWithQuery(() => useCreateFamily());
    const invalidate = vi.spyOn(client, 'invalidateQueries');

    await act(async () => {
      await result.current.mutateAsync({ name: 'Smith' } as never);
    });

    expect(createMock).toHaveBeenCalledWith({ name: 'Smith' });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['families'] });
  });
});

describe('useUpdateFamily', () => {
  it('updates and invalidates both specific and list keys', async () => {
    updateMock.mockResolvedValueOnce({ idKey: 'f1' } as never);
    const { result, client } = renderHookWithQuery(() => useUpdateFamily());
    const invalidate = vi.spyOn(client, 'invalidateQueries');

    await act(async () => {
      await result.current.mutateAsync({ idKey: 'f1', request: { name: 'X' } as never });
    });

    expect(updateMock).toHaveBeenCalledWith('f1', { name: 'X' });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['families', 'f1'] });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['families'] });
  });
});

describe('useAddFamilyMember', () => {
  it('adds a member and invalidates families + people', async () => {
    addMemberMock.mockResolvedValueOnce({ personIdKey: 'p1' } as never);
    const { result, client } = renderHookWithQuery(() => useAddFamilyMember());
    const invalidate = vi.spyOn(client, 'invalidateQueries');

    await act(async () => {
      await result.current.mutateAsync({
        familyIdKey: 'f1',
        request: { personIdKey: 'p1' } as never,
      });
    });

    expect(addMemberMock).toHaveBeenCalledWith('f1', { personIdKey: 'p1' });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['families', 'f1'] });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['families'] });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['people'] });
  });
});

describe('useRemoveFamilyMember', () => {
  it('removes a member with optional params', async () => {
    removeMemberMock.mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useRemoveFamilyMember());
    const invalidate = vi.spyOn(client, 'invalidateQueries');

    await act(async () => {
      await result.current.mutateAsync({
        familyIdKey: 'f1',
        personIdKey: 'p1',
        params: { removeFromAllGroups: true },
      });
    });

    expect(removeMemberMock).toHaveBeenCalledWith('f1', 'p1', {
      removeFromAllGroups: true,
    });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['families', 'f1'] });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['people'] });
  });
});
