/**
 * useAnalytics hook module.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor } from '@testing-library/react';

vi.mock('@/services/api/analytics', () => ({
  getAttendanceAnalytics: vi.fn(),
  getAttendanceTrends: vi.fn(),
  getAttendanceByGroup: vi.fn(),
  getTodaysFirstTimeVisitors: vi.fn(),
  getFirstTimeVisitorsByDateRange: vi.fn(),
}));

import * as api from '@/services/api/analytics';
import {
  useAttendanceAnalytics,
  useAttendanceByGroup,
  useAttendanceTrends,
  useFirstTimeVisitorsByDateRange,
  useTodaysFirstTimeVisitors,
} from '../useAnalytics';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

const params = { startDate: '2024-01-01', endDate: '2024-02-01' };

beforeEach(() => vi.clearAllMocks());

describe('analytics queries', () => {
  it('useAttendanceAnalytics forwards params', async () => {
    vi.mocked(api.getAttendanceAnalytics).mockResolvedValueOnce({} as never);
    const { result } = renderHookWithQuery(() => useAttendanceAnalytics(params));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getAttendanceAnalytics).toHaveBeenCalledWith(params);
  });

  it('useAttendanceTrends forwards params', async () => {
    vi.mocked(api.getAttendanceTrends).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useAttendanceTrends(params));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getAttendanceTrends).toHaveBeenCalledWith(params);
  });

  it('useAttendanceByGroup forwards params', async () => {
    vi.mocked(api.getAttendanceByGroup).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useAttendanceByGroup(params));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getAttendanceByGroup).toHaveBeenCalledWith(params);
  });

  it('useTodaysFirstTimeVisitors optional campus', async () => {
    vi.mocked(api.getTodaysFirstTimeVisitors).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useTodaysFirstTimeVisitors('c1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getTodaysFirstTimeVisitors).toHaveBeenCalledWith('c1');
  });

  it('useFirstTimeVisitorsByDateRange forwards args', async () => {
    vi.mocked(api.getFirstTimeVisitorsByDateRange).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() =>
      useFirstTimeVisitorsByDateRange('2024-01-01', '2024-02-01', 'c1')
    );
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getFirstTimeVisitorsByDateRange).toHaveBeenCalledWith(
      '2024-01-01',
      '2024-02-01',
      'c1'
    );
  });
});
