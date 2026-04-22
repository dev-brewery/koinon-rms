/**
 * usePublicGroups — single-line wrapper.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor } from '@testing-library/react';

vi.mock('@/services/api/publicGroups', () => ({
  searchPublicGroups: vi.fn(),
}));

import * as api from '@/services/api/publicGroups';
import { usePublicGroups } from '../usePublicGroups';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('usePublicGroups', () => {
  it('default: no params', async () => {
    vi.mocked(api.searchPublicGroups).mockResolvedValueOnce({ data: [] } as never);
    const { result } = renderHookWithQuery(() => usePublicGroups());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.searchPublicGroups).toHaveBeenCalledWith({});
  });

  it('forwards params', async () => {
    vi.mocked(api.searchPublicGroups).mockResolvedValueOnce({ data: [] } as never);
    const { result } = renderHookWithQuery(() =>
      usePublicGroups({ searchTerm: 'bible', pageNumber: 1 })
    );
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.searchPublicGroups).toHaveBeenCalledWith({
      searchTerm: 'bible',
      pageNumber: 1,
    });
  });
});
