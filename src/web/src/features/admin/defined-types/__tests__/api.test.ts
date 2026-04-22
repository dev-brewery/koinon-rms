/**
 * features/admin/defined-types/api.ts — defined-types admin API.
 * Pattern: mock ./client; assert URL + HTTP method + envelope unwrapping.
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
  createDefinedValue,
  deleteDefinedValue,
  getDefinedType,
  getDefinedTypes,
  reorderDefinedValues,
  updateDefinedValue,
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

describe('defined-types api', () => {
  it('getDefinedTypes unwraps envelope', async () => {
    mockGet.mockResolvedValueOnce({ data: [{ idKey: 'dt1' }] } as never);
    const out = await getDefinedTypes();
    expect(mockGet).toHaveBeenCalledWith('/defined-types');
    expect(out).toEqual([{ idKey: 'dt1' }]);
  });

  it('getDefinedType unwraps envelope', async () => {
    mockGet.mockResolvedValueOnce({ data: { idKey: 'dt1' } } as never);
    await getDefinedType('dt1');
    expect(mockGet).toHaveBeenCalledWith('/defined-types/dt1');
  });

  it('createDefinedValue posts to type-scoped URL', async () => {
    mockPost.mockResolvedValueOnce({ data: { idKey: 'v1' } } as never);
    await createDefinedValue('dt1', {
      value: 'Active',
      description: 'd',
      order: 1,
    });
    expect(mockPost).toHaveBeenCalledWith('/defined-types/dt1/values', {
      value: 'Active',
      description: 'd',
      order: 1,
    });
  });

  it('updateDefinedValue puts to value-scoped URL', async () => {
    mockPut.mockResolvedValueOnce({ data: { idKey: 'v1' } } as never);
    await updateDefinedValue('v1', {
      value: 'Inactive',
      order: 1,
      isActive: false,
    });
    expect(mockPut).toHaveBeenCalledWith('/defined-types/values/v1', {
      value: 'Inactive',
      order: 1,
      isActive: false,
    });
  });

  it('deleteDefinedValue deletes value-scoped URL', async () => {
    mockDel.mockResolvedValueOnce(undefined as never);
    await deleteDefinedValue('v1');
    expect(mockDel).toHaveBeenCalledWith('/defined-types/values/v1');
  });

  it('reorderDefinedValues posts to /reorder with items payload', async () => {
    mockPost.mockResolvedValueOnce(undefined as never);
    await reorderDefinedValues('dt1', {
      items: [
        { idKey: 'v1', order: 0 },
        { idKey: 'v2', order: 1 },
      ],
    });
    expect(mockPost).toHaveBeenCalledWith('/defined-types/dt1/values/reorder', {
      items: [
        { idKey: 'v1', order: 0 },
        { idKey: 'v2', order: 1 },
      ],
    });
  });
});
