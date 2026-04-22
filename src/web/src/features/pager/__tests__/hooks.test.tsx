/**
 * features/pager/hooks.ts
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('../api', () => ({
  searchPagers: vi.fn(),
  getPageHistory: vi.fn(),
  sendPage: vi.fn(),
  PagerMessageType: { PickupNeeded: 'PickupNeeded' },
}));

import * as api from '../api';
import { usePageHistory, usePagerSearch, useSendPage } from '../hooks';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('usePagerSearch', () => {
  it('fires even without searchTerm (always enabled)', async () => {
    vi.mocked(api.searchPagers).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => usePagerSearch());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.searchPagers).toHaveBeenCalledWith(undefined, undefined);
  });

  it('forwards searchTerm + date', async () => {
    vi.mocked(api.searchPagers).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() =>
      usePagerSearch('Smith', '2024-01-01')
    );
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.searchPagers).toHaveBeenCalledWith('Smith', '2024-01-01');
  });
});

describe('usePageHistory', () => {
  it('idle when pagerNumber is null', () => {
    const { result } = renderHookWithQuery(() => usePageHistory(null));
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fires with pagerNumber', async () => {
    vi.mocked(api.getPageHistory).mockResolvedValueOnce({
      pagerNumber: 7,
    } as never);
    const { result } = renderHookWithQuery(() => usePageHistory(7, '2024-01-01'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getPageHistory).toHaveBeenCalledWith(7, '2024-01-01');
  });
});

describe('useSendPage', () => {
  it('invalidates all pager keys on success', async () => {
    vi.mocked(api.sendPage).mockResolvedValueOnce({} as never);
    const { result, client } = renderHookWithQuery(() => useSendPage());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({
        pagerNumber: '7',
        messageType: 'PickupNeeded',
      } as never);
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['pagers'] });
  });
});
