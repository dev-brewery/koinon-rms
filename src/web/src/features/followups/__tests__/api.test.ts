/**
 * features/followups/api.ts tests.
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
  assignFollowUp,
  FollowUpStatus,
  getFollowUp,
  getPendingFollowUps,
  updateFollowUpStatus,
} from '../api';

const mockGet = vi.mocked(client.get);
const mockPut = vi.mocked(client.put);

beforeEach(() => {
  mockGet.mockReset();
  mockPut.mockReset();
});

describe('followups api', () => {
  it('getPendingFollowUps without assignee', async () => {
    mockGet.mockResolvedValueOnce({ data: [] } as never);
    await getPendingFollowUps();
    expect(mockGet).toHaveBeenCalledWith('/followups/pending');
  });

  it('getPendingFollowUps appends assignedToIdKey', async () => {
    mockGet.mockResolvedValueOnce({ data: [] } as never);
    await getPendingFollowUps('u1');
    expect(mockGet).toHaveBeenLastCalledWith('/followups/pending?assignedToIdKey=u1');
  });

  it('getFollowUp unwraps envelope', async () => {
    mockGet.mockResolvedValueOnce({ data: { idKey: 'f1' } } as never);
    const out = await getFollowUp('f1');
    expect(mockGet).toHaveBeenCalledWith('/followups/f1');
    expect(out).toEqual({ idKey: 'f1' });
  });

  it('updateFollowUpStatus sends status + notes as body', async () => {
    mockPut.mockResolvedValueOnce({ data: { idKey: 'f1' } } as never);
    await updateFollowUpStatus('f1', FollowUpStatus.Connected, 'note');
    expect(mockPut).toHaveBeenCalledWith('/followups/f1/status', {
      status: FollowUpStatus.Connected,
      notes: 'note',
    });
  });

  it('assignFollowUp sends assignedToIdKey body, no return unwrap', async () => {
    mockPut.mockResolvedValueOnce(undefined as never);
    await assignFollowUp('f1', 'u1');
    expect(mockPut).toHaveBeenCalledWith('/followups/f1/assign', {
      assignedToIdKey: 'u1',
    });
  });
});
