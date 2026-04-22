/**
 * features/admin/defined-types/hooks.ts
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('../api', () => ({
  getDefinedTypes: vi.fn(),
  getDefinedType: vi.fn(),
  createDefinedValue: vi.fn(),
  updateDefinedValue: vi.fn(),
  deleteDefinedValue: vi.fn(),
  reorderDefinedValues: vi.fn(),
}));

import * as api from '../api';
import {
  useCreateDefinedValue,
  useDefinedType,
  useDefinedTypes,
  useDeleteDefinedValue,
  useReorderDefinedValues,
  useUpdateDefinedValue,
} from '../hooks';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('queries', () => {
  it('useDefinedTypes fetches list', async () => {
    vi.mocked(api.getDefinedTypes).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useDefinedTypes());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getDefinedTypes).toHaveBeenCalled();
  });

  it('useDefinedType fetches detail', async () => {
    vi.mocked(api.getDefinedType).mockResolvedValueOnce({ idKey: 'dt1' } as never);
    const { result } = renderHookWithQuery(() => useDefinedType('dt1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getDefinedType).toHaveBeenCalledWith('dt1');
  });
});

describe('mutations invalidate detail for the parent type', () => {
  it('create invalidates the type detail', async () => {
    vi.mocked(api.createDefinedValue).mockResolvedValueOnce({ idKey: 'v1' } as never);
    const { result, client } = renderHookWithQuery(() => useCreateDefinedValue());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({
        typeIdKey: 'dt1',
        request: { value: 'X', order: 1 } as never,
      });
    });
    expect(api.createDefinedValue).toHaveBeenCalledWith('dt1', {
      value: 'X',
      order: 1,
    });
    // The key factory is: ['defined-types','detail','dt1']
    expect(inv).toHaveBeenCalledWith({ queryKey: ['defined-types', 'detail', 'dt1'] });
  });

  it('update invalidates parent type detail', async () => {
    vi.mocked(api.updateDefinedValue).mockResolvedValueOnce({ idKey: 'v1' } as never);
    const { result, client } = renderHookWithQuery(() => useUpdateDefinedValue());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({
        valueIdKey: 'v1',
        typeIdKey: 'dt1',
        request: { value: 'X', order: 1, isActive: true } as never,
      });
    });
    expect(api.updateDefinedValue).toHaveBeenCalledWith('v1', {
      value: 'X',
      order: 1,
      isActive: true,
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['defined-types', 'detail', 'dt1'] });
  });

  it('delete invalidates parent type detail', async () => {
    vi.mocked(api.deleteDefinedValue).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useDeleteDefinedValue());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ valueIdKey: 'v1', typeIdKey: 'dt1' });
    });
    expect(api.deleteDefinedValue).toHaveBeenCalledWith('v1');
    expect(inv).toHaveBeenCalledWith({ queryKey: ['defined-types', 'detail', 'dt1'] });
  });

  it('reorder invalidates parent type detail', async () => {
    vi.mocked(api.reorderDefinedValues).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useReorderDefinedValues());
    const inv = vi.spyOn(client, 'invalidateQueries');
    const items = { items: [{ idKey: 'v1', order: 0 }] };
    await act(async () => {
      await result.current.mutateAsync({ typeIdKey: 'dt1', request: items });
    });
    expect(api.reorderDefinedValues).toHaveBeenCalledWith('dt1', items);
    expect(inv).toHaveBeenCalledWith({ queryKey: ['defined-types', 'detail', 'dt1'] });
  });
});
