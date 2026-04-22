/**
 * useSupervisorAttendance + extractLocationIdKeys.
 *
 * The hook aggregates multiple room rosters into a flat, time-sorted
 * AttendanceResultDto list. The mapping function is private, so we exercise
 * it via the public query result.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor } from '@testing-library/react';

vi.mock('@/services/api/checkin', () => ({
  getMultipleRoomRosters: vi.fn(),
}));

import * as checkinApi from '@/services/api/checkin';
import {
  extractLocationIdKeys,
  useSupervisorAttendance,
} from '../useSupervisorAttendance';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('extractLocationIdKeys', () => {
  it('returns [] when areas is undefined', () => {
    expect(extractLocationIdKeys(undefined)).toEqual([]);
  });

  it('deduplicates location idKeys across areas', () => {
    const areas = [
      {
        locations: [{ idKey: 'l1' }, { idKey: 'l2' }],
      },
      {
        locations: [{ idKey: 'l2' }, { idKey: 'l3' }],
      },
    ] as never;
    const out = extractLocationIdKeys(areas);
    expect(out.sort()).toEqual(['l1', 'l2', 'l3']);
  });
});

describe('useSupervisorAttendance', () => {
  it('idle when enabled=false', () => {
    const { result } = renderHookWithQuery(() => useSupervisorAttendance([], false));
    expect(result.current.fetchStatus).toBe('idle');
    expect(checkinApi.getMultipleRoomRosters).not.toHaveBeenCalled();
  });

  it('treats undefined locations as empty list', async () => {
    vi.mocked(checkinApi.getMultipleRoomRosters).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useSupervisorAttendance(undefined));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(checkinApi.getMultipleRoomRosters).toHaveBeenCalledWith([]);
  });

  it('flattens rosters + maps roster children into AttendanceResult, sorted newest first', async () => {
    vi.mocked(checkinApi.getMultipleRoomRosters).mockResolvedValueOnce([
      {
        locationName: 'Room A',
        children: [
          {
            attendanceIdKey: 'a1',
            personIdKey: 'p1',
            fullName: 'Alice',
            securityCode: 'AAA',
            checkInTime: '2024-01-01T09:00:00Z',
            isFirstTime: false,
          },
          {
            attendanceIdKey: 'a2',
            personIdKey: 'p2',
            fullName: 'Bob',
            securityCode: null,
            checkInTime: '2024-01-01T10:00:00Z',
            isFirstTime: true,
          },
        ],
      },
      // Empty roster is skipped.
      { locationName: 'Room B', children: [] },
    ] as never);

    const { result } = renderHookWithQuery(() =>
      useSupervisorAttendance(['l1', 'l2'])
    );
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    const data = result.current.data!;
    expect(data).toHaveLength(2);
    // Sorted newest → oldest.
    expect(data[0].personName).toBe('Bob');
    expect(data[1].personName).toBe('Alice');
    // Null securityCode becomes ''.
    expect(data[1].securityCode).toBe('AAA');
    expect(data[0].securityCode).toBe('');
    // Location name carries through; group/schedule are empty strings.
    expect(data[0].locationName).toBe('Room A');
    expect(data[0].groupName).toBe('');
    expect(data[0].scheduleName).toBe('');
  });

  it('rethrows API errors with a wrapping message', async () => {
    vi.mocked(checkinApi.getMultipleRoomRosters).mockRejectedValueOnce(
      new Error('boom')
    );
    const { result } = renderHookWithQuery(() => useSupervisorAttendance(['l1']));
    await waitFor(() => expect(result.current.isError).toBe(true));
    expect((result.current.error as Error).message).toMatch(
      /Failed to fetch supervisor attendance: boom/
    );
  });
});
