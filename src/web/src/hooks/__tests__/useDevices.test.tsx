/**
 * useDevices hook module.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/devices', () => ({
  getDevices: vi.fn(),
  getDeviceByIdKey: vi.fn(),
  createDevice: vi.fn(),
  updateDevice: vi.fn(),
  deleteDevice: vi.fn(),
  generateKioskToken: vi.fn(),
}));

import * as api from '@/services/api/devices';
import {
  useCreateDevice,
  useDeleteDevice,
  useDevice,
  useDevices,
  useGenerateKioskToken,
  useUpdateDevice,
} from '../useDevices';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useDevices', () => {
  it('forwards params', async () => {
    vi.mocked(api.getDevices).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useDevices({ q: 'iPad' }));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getDevices).toHaveBeenCalledWith({ q: 'iPad' });
  });
});

describe('useDevice', () => {
  it('idle without idKey', () => {
    const { result } = renderHookWithQuery(() => useDevice());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fires with idKey', async () => {
    vi.mocked(api.getDeviceByIdKey).mockResolvedValueOnce({ idKey: 'd1' } as never);
    const { result } = renderHookWithQuery(() => useDevice('d1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getDeviceByIdKey).toHaveBeenCalledWith('d1');
  });
});

describe('mutations', () => {
  it('create invalidates', async () => {
    vi.mocked(api.createDevice).mockResolvedValueOnce({ idKey: 'd1' } as never);
    const { result, client } = renderHookWithQuery(() => useCreateDevice());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ name: 'iPad' } as never);
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['devices'] });
  });

  it('update invalidates specific + list', async () => {
    vi.mocked(api.updateDevice).mockResolvedValueOnce({ idKey: 'd1' } as never);
    const { result, client } = renderHookWithQuery(() => useUpdateDevice());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ idKey: 'd1', request: { name: 'X' } as never });
    });
    expect(api.updateDevice).toHaveBeenCalledWith('d1', { name: 'X' });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['devices', 'd1'] });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['devices'] });
  });

  it('delete invalidates', async () => {
    vi.mocked(api.deleteDevice).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useDeleteDevice());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('d1');
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['devices'] });
  });

  it('generateKioskToken invalidates device-specific key', async () => {
    vi.mocked(api.generateKioskToken).mockResolvedValueOnce({
      token: 't',
      deviceIdKey: 'd1',
      deviceName: 'A',
    } as never);
    const { result, client } = renderHookWithQuery(() => useGenerateKioskToken());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('d1');
    });
    expect(api.generateKioskToken).toHaveBeenCalledWith('d1');
    expect(inv).toHaveBeenCalledWith({ queryKey: ['devices', 'd1'] });
  });
});
