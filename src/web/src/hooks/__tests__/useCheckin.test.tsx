/**
 * useCheckin hooks (kiosk-side). Paired with the services/api/checkin tests.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/checkin', () => ({
  getCheckinConfiguration: vi.fn(),
  searchFamiliesForCheckin: vi.fn(),
  getCheckinOpportunities: vi.fn(),
  recordAttendance: vi.fn(),
  checkout: vi.fn(),
  getLabels: vi.fn(),
}));

import * as checkinApi from '@/services/api/checkin';
import {
  useCheckinConfiguration,
  useCheckinOpportunities,
  useCheckinSearch,
  useCheckout,
  useLabels,
  useRecordAttendance,
} from '../useCheckin';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useCheckinConfiguration', () => {
  it('forwards params', async () => {
    vi.mocked(checkinApi.getCheckinConfiguration).mockResolvedValueOnce({} as never);
    const { result } = renderHookWithQuery(() => useCheckinConfiguration({ kioskId: 'k1' }));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(checkinApi.getCheckinConfiguration).toHaveBeenCalledWith({ kioskId: 'k1' });
  });
});

describe('useCheckinSearch', () => {
  it('idle when searchValue is empty', () => {
    const { result } = renderHookWithQuery(() => useCheckinSearch());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('idle when searchValue shorter than 2 chars', () => {
    const { result } = renderHookWithQuery(() => useCheckinSearch('a'));
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fires when searchValue has enough chars', async () => {
    vi.mocked(checkinApi.searchFamiliesForCheckin).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useCheckinSearch('jo', 'Name'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(checkinApi.searchFamiliesForCheckin).toHaveBeenCalledWith({
      searchValue: 'jo',
      searchType: 'Name',
    });
  });
});

describe('useCheckinOpportunities', () => {
  it('idle without familyIdKey', () => {
    const { result } = renderHookWithQuery(() => useCheckinOpportunities());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fires with familyIdKey', async () => {
    vi.mocked(checkinApi.getCheckinOpportunities).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useCheckinOpportunities('f1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(checkinApi.getCheckinOpportunities).toHaveBeenCalledWith('f1', {});
  });
});

describe('useRecordAttendance', () => {
  it('calls api and invalidates opportunities on success', async () => {
    vi.mocked(checkinApi.recordAttendance).mockResolvedValueOnce({ results: [] } as never);
    const { result, client } = renderHookWithQuery(() => useRecordAttendance());
    const inv = vi.spyOn(client, 'invalidateQueries');
    const items = [{ personIdKey: 'p1', groupIdKey: 'g1', locationIdKey: 'l1' }] as never;
    await act(async () => {
      await result.current.mutateAsync(items);
    });
    expect(checkinApi.recordAttendance).toHaveBeenCalledWith(items);
    expect(inv).toHaveBeenCalledWith({ queryKey: ['checkin', 'opportunities'] });
  });
});

describe('useCheckout', () => {
  it('calls api and invalidates', async () => {
    vi.mocked(checkinApi.checkout).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useCheckout());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('a1');
    });
    expect(checkinApi.checkout).toHaveBeenCalledWith('a1');
    expect(inv).toHaveBeenCalledWith({ queryKey: ['checkin', 'opportunities'] });
  });
});

describe('useLabels', () => {
  it('idle without attendanceIdKey', () => {
    const { result } = renderHookWithQuery(() => useLabels());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fires with attendanceIdKey', async () => {
    vi.mocked(checkinApi.getLabels).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useLabels('a1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(checkinApi.getLabels).toHaveBeenCalledWith('a1', {});
  });
});
