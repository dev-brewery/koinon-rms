/**
 * useCommunicationPreferences + the single/bulk update mutations.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/communications', () => ({
  getCommunicationPreferences: vi.fn(),
  updateCommunicationPreference: vi.fn(),
  bulkUpdateCommunicationPreferences: vi.fn(),
}));

import * as api from '@/services/api/communications';
import {
  useBulkUpdateCommunicationPreferences,
  useCommunicationPreferences,
  useUpdateCommunicationPreference,
} from '../useCommunicationPreferences';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useCommunicationPreferences', () => {
  it('idle without personIdKey', () => {
    const { result } = renderHookWithQuery(() => useCommunicationPreferences());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fires with personIdKey', async () => {
    vi.mocked(api.getCommunicationPreferences).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useCommunicationPreferences('p1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getCommunicationPreferences).toHaveBeenCalledWith('p1');
  });
});

describe('useUpdateCommunicationPreference', () => {
  it('calls api and invalidates per-person key', async () => {
    vi.mocked(api.updateCommunicationPreference).mockResolvedValueOnce({} as never);
    const { result, client } = renderHookWithQuery(() => useUpdateCommunicationPreference());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({
        personIdKey: 'p1',
        type: 'Email',
        request: { optIn: false } as never,
      });
    });
    expect(api.updateCommunicationPreference).toHaveBeenCalledWith(
      'p1',
      'Email',
      { optIn: false }
    );
    expect(inv).toHaveBeenCalledWith({
      queryKey: ['communication-preferences', 'p1'],
    });
  });
});

describe('useBulkUpdateCommunicationPreferences', () => {
  it('calls api and invalidates per-person key', async () => {
    vi.mocked(api.bulkUpdateCommunicationPreferences).mockResolvedValueOnce([] as never);
    const { result, client } = renderHookWithQuery(() =>
      useBulkUpdateCommunicationPreferences()
    );
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({
        personIdKey: 'p1',
        request: { preferences: [] } as never,
      });
    });
    expect(api.bulkUpdateCommunicationPreferences).toHaveBeenCalledWith('p1', {
      preferences: [],
    });
    expect(inv).toHaveBeenCalledWith({
      queryKey: ['communication-preferences', 'p1'],
    });
  });
});
