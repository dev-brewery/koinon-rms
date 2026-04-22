/**
 * useCommunicationTemplates — list/detail/CRUD with onError invalidation.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/communicationTemplates', () => ({
  getCommunicationTemplates: vi.fn(),
  getCommunicationTemplate: vi.fn(),
  createCommunicationTemplate: vi.fn(),
  updateCommunicationTemplate: vi.fn(),
  deleteCommunicationTemplate: vi.fn(),
}));

import * as api from '@/services/api/communicationTemplates';
import {
  useCommunicationTemplate,
  useCommunicationTemplates,
  useCreateCommunicationTemplate,
  useDeleteCommunicationTemplate,
  useUpdateCommunicationTemplate,
} from '../useCommunicationTemplates';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useCommunicationTemplates', () => {
  it('forwards params', async () => {
    vi.mocked(api.getCommunicationTemplates).mockResolvedValueOnce({ data: [] } as never);
    const { result } = renderHookWithQuery(() => useCommunicationTemplates({ page: 2 }));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getCommunicationTemplates).toHaveBeenCalledWith({ page: 2 });
  });
});

describe('useCommunicationTemplate', () => {
  it('idle without idKey', () => {
    const { result } = renderHookWithQuery(() => useCommunicationTemplate());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fires with idKey', async () => {
    vi.mocked(api.getCommunicationTemplate).mockResolvedValueOnce({ idKey: 't1' } as never);
    const { result } = renderHookWithQuery(() => useCommunicationTemplate('t1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getCommunicationTemplate).toHaveBeenCalledWith('t1');
  });
});

describe('mutations invalidate on success AND on error', () => {
  it('create invalidates on success', async () => {
    vi.mocked(api.createCommunicationTemplate).mockResolvedValueOnce({ idKey: 't1' } as never);
    const { result, client } = renderHookWithQuery(() => useCreateCommunicationTemplate());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ name: 'N' } as never);
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['communication-templates'] });
  });

  it('create invalidates on error', async () => {
    vi.mocked(api.createCommunicationTemplate).mockRejectedValueOnce(new Error('boom'));
    const { result, client } = renderHookWithQuery(() => useCreateCommunicationTemplate());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      try {
        await result.current.mutateAsync({ name: 'N' } as never);
      } catch {
        /* expected */
      }
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['communication-templates'] });
  });

  it('update invalidates list + specific on success', async () => {
    vi.mocked(api.updateCommunicationTemplate).mockResolvedValueOnce({ idKey: 't1' } as never);
    const { result, client } = renderHookWithQuery(() => useUpdateCommunicationTemplate());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ idKey: 't1', request: { name: 'X' } as never });
    });
    expect(api.updateCommunicationTemplate).toHaveBeenCalledWith('t1', { name: 'X' });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['communication-templates', 't1'] });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['communication-templates'] });
  });

  it('update invalidates list on error', async () => {
    vi.mocked(api.updateCommunicationTemplate).mockRejectedValueOnce(new Error('nope'));
    const { result, client } = renderHookWithQuery(() => useUpdateCommunicationTemplate());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      try {
        await result.current.mutateAsync({ idKey: 't1', request: { name: 'X' } as never });
      } catch {
        /* expected */
      }
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['communication-templates'] });
  });

  it('delete invalidates on success and on error', async () => {
    vi.mocked(api.deleteCommunicationTemplate).mockResolvedValueOnce(undefined as never);
    const ok = renderHookWithQuery(() => useDeleteCommunicationTemplate());
    const invOk = vi.spyOn(ok.client, 'invalidateQueries');
    await act(async () => {
      await ok.result.current.mutateAsync('t1');
    });
    expect(invOk).toHaveBeenCalledWith({ queryKey: ['communication-templates'] });

    vi.mocked(api.deleteCommunicationTemplate).mockRejectedValueOnce(new Error('boom'));
    const bad = renderHookWithQuery(() => useDeleteCommunicationTemplate());
    const invBad = vi.spyOn(bad.client, 'invalidateQueries');
    await act(async () => {
      try {
        await bad.result.current.mutateAsync('t1');
      } catch {
        /* expected */
      }
    });
    expect(invBad).toHaveBeenCalledWith({ queryKey: ['communication-templates'] });
  });
});
