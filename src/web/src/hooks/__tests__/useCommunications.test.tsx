/**
 * useCommunications + related hooks.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/communications', () => ({
  getCommunication: vi.fn(),
  getCommunications: vi.fn(),
  createCommunication: vi.fn(),
  sendCommunication: vi.fn(),
  scheduleCommunication: vi.fn(),
  cancelSchedule: vi.fn(),
  getMergeFields: vi.fn(),
  previewCommunication: vi.fn(),
}));

import * as api from '@/services/api/communications';
import {
  useCancelSchedule,
  useCommunication,
  useCommunications,
  useCreateCommunication,
  useMergeFields,
  usePreviewCommunication,
  useScheduleCommunication,
  useSendCommunication,
} from '../useCommunications';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useCommunication / useCommunications / useMergeFields', () => {
  it('useCommunication idle without idKey, fires with', async () => {
    const idle = renderHookWithQuery(() => useCommunication()).result;
    expect(idle.current.fetchStatus).toBe('idle');

    vi.mocked(api.getCommunication).mockResolvedValueOnce({ idKey: 'c1' } as never);
    const { result } = renderHookWithQuery(() => useCommunication('c1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getCommunication).toHaveBeenCalledWith('c1');
  });

  it('useCommunications forwards params', async () => {
    vi.mocked(api.getCommunications).mockResolvedValueOnce({ data: [] } as never);
    const { result } = renderHookWithQuery(() => useCommunications({ page: 2 }));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getCommunications).toHaveBeenCalledWith({ page: 2 });
  });

  it('useMergeFields fetches once', async () => {
    vi.mocked(api.getMergeFields).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useMergeFields());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
  });
});

describe('mutations invalidate communications', () => {
  it('create invalidates communications', async () => {
    vi.mocked(api.createCommunication).mockResolvedValueOnce({ idKey: 'c1' } as never);
    const { result, client } = renderHookWithQuery(() => useCreateCommunication());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ subject: 'S' } as never);
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['communications'] });
  });

  it('send invalidates specific + list', async () => {
    vi.mocked(api.sendCommunication).mockResolvedValueOnce({ idKey: 'c1' } as never);
    const { result, client } = renderHookWithQuery(() => useSendCommunication());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('c1');
    });
    expect(api.sendCommunication).toHaveBeenCalledWith('c1');
    expect(inv).toHaveBeenCalledWith({ queryKey: ['communications', 'c1'] });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['communications'] });
  });

  it('schedule invalidates specific + list', async () => {
    vi.mocked(api.scheduleCommunication).mockResolvedValueOnce({ idKey: 'c1' } as never);
    const { result, client } = renderHookWithQuery(() => useScheduleCommunication());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({
        idKey: 'c1',
        scheduledDateTime: '2024-01-01T10:00:00Z',
      });
    });
    expect(api.scheduleCommunication).toHaveBeenCalledWith('c1', '2024-01-01T10:00:00Z');
    expect(inv).toHaveBeenCalledWith({ queryKey: ['communications', 'c1'] });
  });

  it('cancelSchedule invalidates', async () => {
    vi.mocked(api.cancelSchedule).mockResolvedValueOnce({ idKey: 'c1' } as never);
    const { result, client } = renderHookWithQuery(() => useCancelSchedule());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('c1');
    });
    expect(api.cancelSchedule).toHaveBeenCalledWith('c1');
    expect(inv).toHaveBeenCalledWith({ queryKey: ['communications', 'c1'] });
  });

  it('previewCommunication mutation calls api', async () => {
    vi.mocked(api.previewCommunication).mockResolvedValueOnce({ body: 'x' } as never);
    const { result } = renderHookWithQuery(() => usePreviewCommunication());
    await act(async () => {
      await result.current.mutateAsync({ templateIdKey: 't1' } as never);
    });
    expect(api.previewCommunication).toHaveBeenCalledWith({ templateIdKey: 't1' });
  });
});
