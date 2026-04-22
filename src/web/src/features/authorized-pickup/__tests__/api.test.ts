/**
 * features/authorized-pickup/api.ts tests.
 * This module returns bodies directly (no `{ data: ... }` unwrap).
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
  addAuthorizedPickup,
  AuthorizationLevel,
  autoPopulateFamilyMembers,
  deleteAuthorizedPickup,
  getAuthorizedPickups,
  getPickupHistory,
  PickupRelationship,
  recordPickup,
  updateAuthorizedPickup,
  verifyPickup,
} from '../api';

const mockGet = vi.mocked(client.get);
const mockPost = vi.mocked(client.post);
const mockPut = vi.mocked(client.put);
const mockDel = vi.mocked(client.del);

beforeEach(() => {
  mockGet.mockReset();
  mockPost.mockReset();
  mockPut.mockReset();
  mockDel.mockReset();
});

describe('authorized-pickup api (features tier)', () => {
  it('getAuthorizedPickups (no envelope)', async () => {
    mockGet.mockResolvedValueOnce([{ idKey: 'ap1' }] as never);
    const out = await getAuthorizedPickups('c1');
    expect(mockGet).toHaveBeenCalledWith('/people/c1/authorized-pickups');
    expect(out).toEqual([{ idKey: 'ap1' }]);
  });

  it('addAuthorizedPickup posts', async () => {
    mockPost.mockResolvedValueOnce({ idKey: 'ap1' } as never);
    await addAuthorizedPickup('c1', {
      name: 'Bob',
      relationship: PickupRelationship.Parent,
      authorizationLevel: AuthorizationLevel.Always,
    });
    const [url, body] = mockPost.mock.calls[0];
    expect(url).toBe('/people/c1/authorized-pickups');
    expect(body).toMatchObject({
      name: 'Bob',
      relationship: PickupRelationship.Parent,
      authorizationLevel: AuthorizationLevel.Always,
    });
  });

  it('updateAuthorizedPickup puts', async () => {
    mockPut.mockResolvedValueOnce({ idKey: 'ap1' } as never);
    await updateAuthorizedPickup('ap1', { isActive: false });
    expect(mockPut).toHaveBeenCalledWith('/authorized-pickups/ap1', {
      isActive: false,
    });
  });

  it('deleteAuthorizedPickup deletes', async () => {
    mockDel.mockResolvedValueOnce(undefined as never);
    await deleteAuthorizedPickup('ap1');
    expect(mockDel).toHaveBeenCalledWith('/authorized-pickups/ap1');
  });

  it('autoPopulateFamilyMembers posts without body', async () => {
    mockPost.mockResolvedValueOnce({ message: 'ok', count: 0, pickups: [] } as never);
    await autoPopulateFamilyMembers('c1');
    expect(mockPost).toHaveBeenCalledWith(
      '/people/c1/authorized-pickups/auto-populate'
    );
  });

  it('verifyPickup posts to /checkin/verify-pickup', async () => {
    mockPost.mockResolvedValueOnce({ isAuthorized: true } as never);
    const req = {
      attendanceIdKey: 'a1',
      pickupPersonName: 'X',
      securityCode: '1234',
    };
    await verifyPickup(req);
    expect(mockPost).toHaveBeenCalledWith('/checkin/verify-pickup', req);
  });

  it('recordPickup posts to /checkin/record-pickup', async () => {
    mockPost.mockResolvedValueOnce({ idKey: 'log1' } as never);
    const req = {
      attendanceIdKey: 'a1',
      wasAuthorized: true,
      supervisorOverride: false,
    };
    await recordPickup(req);
    expect(mockPost).toHaveBeenCalledWith('/checkin/record-pickup', req);
  });

  it('getPickupHistory with/without date range', async () => {
    mockGet.mockResolvedValueOnce([] as never);
    await getPickupHistory('c1');
    expect(mockGet).toHaveBeenCalledWith('/people/c1/pickup-history');

    mockGet.mockResolvedValueOnce([] as never);
    await getPickupHistory('c1', '2024-01-01', '2024-02-01');
    const url = mockGet.mock.calls.at(-1)?.[0] as string;
    expect(url).toContain('fromDate=2024-01-01');
    expect(url).toContain('toDate=2024-02-01');
  });
});
