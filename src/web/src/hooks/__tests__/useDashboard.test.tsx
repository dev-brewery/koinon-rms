/**
 * useDashboardStats is a one-line TanStack Query wrapper.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor } from '@testing-library/react';

vi.mock('@/services/api/dashboard', () => ({
  getDashboardStats: vi.fn(),
}));

import * as dashboardApi from '@/services/api/dashboard';
import { useDashboardStats } from '../useDashboard';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useDashboardStats', () => {
  it('fetches and returns stats', async () => {
    vi.mocked(dashboardApi.getDashboardStats).mockResolvedValueOnce({
      activeCheckins: 42,
    } as never);
    const { result } = renderHookWithQuery(() => useDashboardStats());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data).toEqual({ activeCheckins: 42 });
  });

  it('surfaces errors', async () => {
    vi.mocked(dashboardApi.getDashboardStats).mockRejectedValueOnce(new Error('boom'));
    const { result } = renderHookWithQuery(() => useDashboardStats());
    await waitFor(() => expect(result.current.isError).toBe(true));
    expect((result.current.error as Error).message).toBe('boom');
  });
});
