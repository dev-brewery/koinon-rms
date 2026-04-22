/**
 * features/pager/api.ts tests.
 * Note: unlike services/api/pager.ts, this module returns the body directly
 * (no `{ data: ... }` unwrap) for all endpoints.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';

vi.mock('@/services/api/client', () => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  del: vi.fn(),
}));

import * as client from '@/services/api/client';
import {
  getNextPagerNumber,
  getPageHistory,
  PagerMessageType,
  searchPagers,
  sendPage,
} from '../api';

const mockGet = vi.mocked(client.get);
const mockPost = vi.mocked(client.post);

beforeEach(() => {
  mockGet.mockReset();
  mockPost.mockReset();
});

describe('pager api (features/pager)', () => {
  it('searchPagers without args => /pager/search', async () => {
    mockGet.mockResolvedValueOnce([] as never);
    await searchPagers();
    expect(mockGet).toHaveBeenCalledWith('/pager/search');
  });

  it('searchPagers with searchTerm + date', async () => {
    mockGet.mockResolvedValueOnce([] as never);
    await searchPagers('Smith', '2024-01-01');
    const url = mockGet.mock.calls[0][0] as string;
    expect(url).toContain('/pager/search?');
    expect(url).toContain('searchTerm=Smith');
    expect(url).toContain('date=2024-01-01');
  });

  it('sendPage posts payload', async () => {
    mockPost.mockResolvedValueOnce({ idKey: 'm1' } as never);
    await sendPage({
      pagerNumber: '7',
      messageType: PagerMessageType.PickupNeeded,
    });
    expect(mockPost).toHaveBeenCalledWith('/pager/send', {
      pagerNumber: '7',
      messageType: PagerMessageType.PickupNeeded,
    });
  });

  it('getPageHistory without date', async () => {
    mockGet.mockResolvedValueOnce({} as never);
    await getPageHistory(7);
    expect(mockGet).toHaveBeenCalledWith('/pager/7/history');
  });

  it('getPageHistory with date', async () => {
    mockGet.mockResolvedValueOnce({} as never);
    await getPageHistory(7, '2024-01-01');
    expect(mockGet).toHaveBeenLastCalledWith('/pager/7/history?date=2024-01-01');
  });

  it('getNextPagerNumber with/without campus', async () => {
    mockGet.mockResolvedValueOnce(42 as never);
    expect(await getNextPagerNumber()).toBe(42);
    expect(mockGet).toHaveBeenCalledWith('/pager/next-number');

    mockGet.mockResolvedValueOnce(43 as never);
    await getNextPagerNumber('c1');
    expect(mockGet).toHaveBeenLastCalledWith('/pager/next-number?campusId=c1');
  });
});
