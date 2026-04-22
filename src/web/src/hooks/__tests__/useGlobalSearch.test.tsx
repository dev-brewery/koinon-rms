/**
 * useGlobalSearch — debounced search + recent-searches localStorage state.
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { act, waitFor } from '@testing-library/react';

vi.mock('@/services/api/search', () => ({
  globalSearch: vi.fn(),
}));

import * as searchApi from '@/services/api/search';
import { useGlobalSearch } from '../useGlobalSearch';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

const RECENT_KEY = 'koinon:recent-searches';

beforeEach(() => {
  localStorage.clear();
  vi.clearAllMocks();
});

afterEach(() => {
  localStorage.clear();
});

describe('useGlobalSearch initial state', () => {
  it('starts empty with page=1 and no recent searches', () => {
    const { result } = renderHookWithQuery(() => useGlobalSearch());
    expect(result.current.query).toBe('');
    expect(result.current.page).toBe(1);
    expect(result.current.results).toEqual([]);
    expect(result.current.recentSearches).toEqual([]);
  });

  it('hydrates recent searches from localStorage on mount', () => {
    localStorage.setItem(RECENT_KEY, JSON.stringify(['alice', 'bob']));
    const { result } = renderHookWithQuery(() => useGlobalSearch());
    expect(result.current.recentSearches).toEqual(['alice', 'bob']);
  });
});

describe('useGlobalSearch — search gating', () => {
  it('does not search for queries shorter than 2 chars', async () => {
    const { result } = renderHookWithQuery(() => useGlobalSearch());
    act(() => {
      result.current.setQuery('a');
    });
    // Give debounce time to resolve — we use real timers here; 350ms is enough.
    await new Promise((r) => setTimeout(r, 350));
    expect(searchApi.globalSearch).not.toHaveBeenCalled();
  });

  it('searches with debounced query (>=2 chars)', async () => {
    vi.mocked(searchApi.globalSearch).mockResolvedValue({
      results: [{ idKey: 'x' }],
      totalCount: 1,
      categoryCounts: {},
      pageSize: 20,
    } as never);

    const { result } = renderHookWithQuery(() => useGlobalSearch());
    act(() => {
      result.current.setQuery('jo');
    });

    await waitFor(() => {
      expect(searchApi.globalSearch).toHaveBeenCalledWith(
        expect.objectContaining({
          query: 'jo',
          pageNumber: 1,
          pageSize: 20,
        })
      );
    });
    await waitFor(() => expect(result.current.results).toHaveLength(1));
  });

  it('resets page to 1 when query changes', async () => {
    vi.mocked(searchApi.globalSearch).mockResolvedValue({
      results: [],
      totalCount: 0,
      categoryCounts: {},
      pageSize: 20,
    } as never);
    const { result } = renderHookWithQuery(() => useGlobalSearch());

    act(() => {
      result.current.setQuery('jo');
    });
    await waitFor(() => expect(searchApi.globalSearch).toHaveBeenCalled());
    act(() => {
      result.current.setPage(3);
    });
    expect(result.current.page).toBe(3);
    // Changing the query resets page back to 1.
    act(() => {
      result.current.setQuery('jack');
    });
    await waitFor(() => expect(result.current.page).toBe(1));
  });
});

describe('useGlobalSearch — recent searches', () => {
  it('adds successful search to recent searches (newest first, deduped)', async () => {
    vi.mocked(searchApi.globalSearch).mockResolvedValue({
      results: [],
      totalCount: 0,
      categoryCounts: {},
      pageSize: 20,
    } as never);
    const { result } = renderHookWithQuery(() => useGlobalSearch());

    act(() => {
      result.current.setQuery('alpha');
    });
    await waitFor(() =>
      expect(result.current.recentSearches).toContain('alpha')
    );

    act(() => {
      result.current.setQuery('beta');
    });
    await waitFor(() => expect(result.current.recentSearches[0]).toBe('beta'));

    // Re-search 'alpha' — should bubble to front without duplication.
    act(() => {
      result.current.setQuery('alpha');
    });
    await waitFor(() => expect(result.current.recentSearches[0]).toBe('alpha'));
    expect(
      result.current.recentSearches.filter((s) => s === 'alpha')
    ).toHaveLength(1);
  });

  it('clearRecentSearches wipes both state and localStorage', () => {
    localStorage.setItem(RECENT_KEY, JSON.stringify(['x']));
    const { result } = renderHookWithQuery(() => useGlobalSearch());
    expect(result.current.recentSearches).toEqual(['x']);
    act(() => {
      result.current.clearRecentSearches();
    });
    expect(result.current.recentSearches).toEqual([]);
    expect(localStorage.getItem(RECENT_KEY)).toBeNull();
  });

  it('addToRecentSearches directly adds a manual entry (>=2 chars)', () => {
    const { result } = renderHookWithQuery(() => useGlobalSearch());
    act(() => {
      result.current.addToRecentSearches('smith');
    });
    expect(result.current.recentSearches).toContain('smith');
    // 1-char entries are rejected.
    act(() => {
      result.current.addToRecentSearches('x');
    });
    expect(result.current.recentSearches).not.toContain('x');
  });
});

describe('useGlobalSearch — category filter', () => {
  it('resets page when category changes', async () => {
    vi.mocked(searchApi.globalSearch).mockResolvedValue({
      results: [],
      totalCount: 0,
      categoryCounts: {},
      pageSize: 20,
    } as never);
    const { result } = renderHookWithQuery(() => useGlobalSearch());
    act(() => {
      result.current.setQuery('smith');
    });
    await waitFor(() => expect(searchApi.globalSearch).toHaveBeenCalled());
    act(() => {
      result.current.setPage(2);
    });
    expect(result.current.page).toBe(2);
    act(() => {
      result.current.setCategory('People');
    });
    await waitFor(() => expect(result.current.page).toBe(1));
  });
});
