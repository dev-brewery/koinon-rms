/**
 * useRoomRoster / useMultipleRoomRosters / useCheckOutFromRoster.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/checkin', () => ({
  getRoomRoster: vi.fn(),
  getMultipleRoomRosters: vi.fn(),
  checkoutFromRoster: vi.fn(),
}));

import * as checkinApi from '@/services/api/checkin';
import {
  useCheckOutFromRoster,
  useMultipleRoomRosters,
  useRoomRoster,
} from '../useRoomRoster';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useRoomRoster', () => {
  it('idle without locationIdKey', () => {
    const { result } = renderHookWithQuery(() => useRoomRoster());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fires with locationIdKey', async () => {
    vi.mocked(checkinApi.getRoomRoster).mockResolvedValueOnce({
      locationIdKey: 'l1',
    } as never);
    const { result } = renderHookWithQuery(() => useRoomRoster('l1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(checkinApi.getRoomRoster).toHaveBeenCalledWith('l1');
  });
});

describe('useMultipleRoomRosters', () => {
  it('idle when empty list', () => {
    const { result } = renderHookWithQuery(() => useMultipleRoomRosters([]));
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('idle when undefined', () => {
    const { result } = renderHookWithQuery(() => useMultipleRoomRosters());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fires when list has items', async () => {
    vi.mocked(checkinApi.getMultipleRoomRosters).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useMultipleRoomRosters(['l1', 'l2']));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(checkinApi.getMultipleRoomRosters).toHaveBeenCalledWith(['l1', 'l2']);
  });
});

describe('useCheckOutFromRoster', () => {
  it('invalidates all roster queries', async () => {
    vi.mocked(checkinApi.checkoutFromRoster).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useCheckOutFromRoster());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('a1');
    });
    expect(checkinApi.checkoutFromRoster).toHaveBeenCalledWith('a1');
    expect(inv).toHaveBeenCalledWith({ queryKey: ['checkin', 'roster'] });
  });
});
