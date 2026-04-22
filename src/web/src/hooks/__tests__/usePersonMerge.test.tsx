/**
 * usePersonMerge hooks — dedupe workflow.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/personMerge', () => ({
  getDuplicates: vi.fn(),
  getDuplicatesForPerson: vi.fn(),
  comparePeople: vi.fn(),
  mergePeople: vi.fn(),
  getMergeHistory: vi.fn(),
  ignoreDuplicate: vi.fn(),
  unignoreDuplicate: vi.fn(),
}));

import * as api from '@/services/api/personMerge';
import {
  useDuplicates,
  useDuplicatesForPerson,
  useIgnoreDuplicate,
  useMergeHistory,
  useMergePeople,
  usePersonComparison,
  useUnignoreDuplicate,
} from '../usePersonMerge';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useDuplicates', () => {
  it('defaults page=1, pageSize=25', async () => {
    vi.mocked(api.getDuplicates).mockResolvedValueOnce({ data: [] } as never);
    const { result } = renderHookWithQuery(() => useDuplicates());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getDuplicates).toHaveBeenCalledWith(1, 25);
  });
});

describe('useDuplicatesForPerson', () => {
  it('idle without idKey', () => {
    const { result } = renderHookWithQuery(() => useDuplicatesForPerson());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fires with idKey', async () => {
    vi.mocked(api.getDuplicatesForPerson).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useDuplicatesForPerson('p1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getDuplicatesForPerson).toHaveBeenCalledWith('p1');
  });
});

describe('usePersonComparison', () => {
  it('idle without BOTH ids', () => {
    const a = renderHookWithQuery(() => usePersonComparison('p1')).result;
    const b = renderHookWithQuery(() => usePersonComparison(undefined, 'p2')).result;
    expect(a.current.fetchStatus).toBe('idle');
    expect(b.current.fetchStatus).toBe('idle');
  });

  it('fires with both', async () => {
    vi.mocked(api.comparePeople).mockResolvedValueOnce({} as never);
    const { result } = renderHookWithQuery(() => usePersonComparison('p1', 'p2'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.comparePeople).toHaveBeenCalledWith('p1', 'p2');
  });
});

describe('useMergePeople', () => {
  it('invalidates duplicates + people + mergeHistory', async () => {
    vi.mocked(api.mergePeople).mockResolvedValueOnce({} as never);
    const { result, client } = renderHookWithQuery(() => useMergePeople());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ targetIdKey: 'p1', sourceIdKey: 'p2' } as never);
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['duplicates'] });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['people'] });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['mergeHistory'] });
  });
});

describe('useMergeHistory', () => {
  it('defaults to page 1 size 25', async () => {
    vi.mocked(api.getMergeHistory).mockResolvedValueOnce({ data: [] } as never);
    const { result } = renderHookWithQuery(() => useMergeHistory());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getMergeHistory).toHaveBeenCalledWith(1, 25);
  });
});

describe('useIgnoreDuplicate / useUnignoreDuplicate', () => {
  it('both invalidate duplicates', async () => {
    vi.mocked(api.ignoreDuplicate).mockResolvedValueOnce(undefined as never);
    vi.mocked(api.unignoreDuplicate).mockResolvedValueOnce(undefined as never);

    const ig = renderHookWithQuery(() => useIgnoreDuplicate());
    const un = renderHookWithQuery(() => useUnignoreDuplicate());
    const invIg = vi.spyOn(ig.client, 'invalidateQueries');
    const invUn = vi.spyOn(un.client, 'invalidateQueries');

    await act(async () => {
      await ig.result.current.mutateAsync({ person1IdKey: 'p1', person2IdKey: 'p2' } as never);
    });
    expect(invIg).toHaveBeenCalledWith({ queryKey: ['duplicates'] });

    await act(async () => {
      await un.result.current.mutateAsync({ person1IdKey: 'p1', person2IdKey: 'p2' });
    });
    expect(api.unignoreDuplicate).toHaveBeenCalledWith('p1', 'p2');
    expect(invUn).toHaveBeenCalledWith({ queryKey: ['duplicates'] });
  });
});
