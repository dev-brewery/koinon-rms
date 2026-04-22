/**
 * useImportJobs — query + client-side type filter.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/import', () => ({
  getImportJobs: vi.fn(),
}));

import * as api from '@/services/api/import';
import { useImportJobs } from '../useImportJobs';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useImportJobs', () => {
  it('fetches all jobs and exposes them as allJobs', async () => {
    vi.mocked(api.getImportJobs).mockResolvedValueOnce([
      { idKey: 'j1', importType: 'People' },
      { idKey: 'j2', importType: 'Families' },
    ] as never);
    const { result } = renderHookWithQuery(() => useImportJobs());
    await waitFor(() => expect(result.current.allJobs).toHaveLength(2));
    // default filter is "all"
    expect(result.current.typeFilter).toBe('all');
    expect(result.current.jobs).toHaveLength(2);
  });

  it('filters by typeFilter when set', async () => {
    vi.mocked(api.getImportJobs).mockResolvedValueOnce([
      { idKey: 'j1', importType: 'People' },
      { idKey: 'j2', importType: 'Families' },
    ] as never);
    const { result } = renderHookWithQuery(() => useImportJobs());
    await waitFor(() => expect(result.current.allJobs).toHaveLength(2));
    act(() => {
      result.current.setTypeFilter('People');
    });
    expect(result.current.jobs).toEqual([{ idKey: 'j1', importType: 'People' }]);
    // allJobs is unchanged
    expect(result.current.allJobs).toHaveLength(2);
  });

  it('returns [] while loading', () => {
    vi.mocked(api.getImportJobs).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useImportJobs());
    expect(result.current.jobs).toEqual([]);
  });
});
