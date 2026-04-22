/**
 * useGiving hook module — the biggest hook file in the repo.
 * Covers batches, contributions, statements, funds (admin), plus the
 * statement-pdf download side-effect.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/giving', () => ({
  getBatches: vi.fn(),
  getBatch: vi.fn(),
  getBatchSummary: vi.fn(),
  getBatchContributions: vi.fn(),
  getActiveFunds: vi.fn(),
  openBatch: vi.fn(),
  closeBatch: vi.fn(),
  createBatch: vi.fn(),
  addContribution: vi.fn(),
  updateContribution: vi.fn(),
  deleteContribution: vi.fn(),
  getStatements: vi.fn(),
  getStatement: vi.fn(),
  getEligiblePeople: vi.fn(),
  generateStatement: vi.fn(),
  getAllFundsAdmin: vi.fn(),
  createFund: vi.fn(),
  updateFund: vi.fn(),
  deactivateFund: vi.fn(),
  downloadStatementPdf: vi.fn(),
}));
vi.mock('@/services/api/reference', () => ({
  getCampuses: vi.fn(),
}));

import * as api from '@/services/api/giving';
import * as referenceApi from '@/services/api/reference';
import {
  useActiveFunds,
  useAddContribution,
  useAdminFunds,
  useBatch,
  useBatchContributions,
  useBatchSummary,
  useBatches,
  useCampuses,
  useCloseBatch,
  useCreateBatch,
  useCreateFund,
  useDeactivateFund,
  useDeleteContribution,
  useDownloadStatementPdf,
  useEligiblePeople,
  useGenerateStatement,
  useOpenBatch,
  useStatement,
  useStatements,
  useUpdateContribution,
  useUpdateFund,
} from '../useGiving';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('batch queries', () => {
  it('useBatches forwards filter', async () => {
    vi.mocked(api.getBatches).mockResolvedValueOnce({ data: [] } as never);
    const { result } = renderHookWithQuery(() => useBatches({ status: 'Open' }));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getBatches).toHaveBeenCalledWith({ status: 'Open' });
  });

  it('useBatch idle without idKey, fires with idKey', async () => {
    const idle = renderHookWithQuery(() => useBatch()).result;
    expect(idle.current.fetchStatus).toBe('idle');

    vi.mocked(api.getBatch).mockResolvedValueOnce({ idKey: 'b1' } as never);
    const { result } = renderHookWithQuery(() => useBatch('b1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
  });

  it('useBatchSummary / useBatchContributions both need idKey', async () => {
    const s = renderHookWithQuery(() => useBatchSummary()).result;
    const c = renderHookWithQuery(() => useBatchContributions()).result;
    expect(s.current.fetchStatus).toBe('idle');
    expect(c.current.fetchStatus).toBe('idle');

    vi.mocked(api.getBatchSummary).mockResolvedValueOnce({} as never);
    vi.mocked(api.getBatchContributions).mockResolvedValueOnce([] as never);

    const s2 = renderHookWithQuery(() => useBatchSummary('b1'));
    const c2 = renderHookWithQuery(() => useBatchContributions('b1'));
    await waitFor(() => expect(s2.result.current.isSuccess).toBe(true));
    await waitFor(() => expect(c2.result.current.isSuccess).toBe(true));
    expect(api.getBatchSummary).toHaveBeenCalledWith('b1');
    expect(api.getBatchContributions).toHaveBeenCalledWith('b1');
  });
});

describe('batch mutations invalidate batches', () => {
  it('open/close/create all invalidate', async () => {
    vi.mocked(api.openBatch).mockResolvedValueOnce({ message: '' } as never);
    vi.mocked(api.closeBatch).mockResolvedValueOnce({ message: '' } as never);
    vi.mocked(api.createBatch).mockResolvedValueOnce({ idKey: 'b1' } as never);

    const open = renderHookWithQuery(() => useOpenBatch());
    const close = renderHookWithQuery(() => useCloseBatch());
    const create = renderHookWithQuery(() => useCreateBatch());
    const invO = vi.spyOn(open.client, 'invalidateQueries');
    const invC = vi.spyOn(close.client, 'invalidateQueries');
    const invCr = vi.spyOn(create.client, 'invalidateQueries');

    await act(async () => {
      await open.result.current.mutateAsync('b1');
    });
    expect(api.openBatch).toHaveBeenCalledWith('b1');
    expect(invO).toHaveBeenCalledWith({ queryKey: ['batches'] });

    await act(async () => {
      await close.result.current.mutateAsync('b1');
    });
    expect(invC).toHaveBeenCalledWith({ queryKey: ['batches'] });

    await act(async () => {
      await create.result.current.mutateAsync({ name: 'Jan' } as never);
    });
    expect(invCr).toHaveBeenCalledWith({ queryKey: ['batches'] });
  });
});

describe('contribution mutations', () => {
  it('add contribution invalidates batch contributions + summary', async () => {
    vi.mocked(api.addContribution).mockResolvedValueOnce({ idKey: 'c1' } as never);
    const { result, client } = renderHookWithQuery(() => useAddContribution());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({
        batchIdKey: 'b1',
        request: { amount: 10 } as never,
      });
    });
    expect(api.addContribution).toHaveBeenCalledWith('b1', { amount: 10 });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['batches', 'b1', 'contributions'] });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['batches', 'b1', 'summary'] });
  });

  it('update contribution invalidates by returned batchIdKey', async () => {
    vi.mocked(api.updateContribution).mockResolvedValueOnce({
      idKey: 'c1',
      batchIdKey: 'b1',
    } as never);
    const { result, client } = renderHookWithQuery(() => useUpdateContribution());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({
        idKey: 'c1',
        data: { amount: 15 } as never,
      });
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['batches', 'b1', 'contributions'] });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['contributions'] });
  });

  it('delete contribution invalidates by batchIdKey variable', async () => {
    vi.mocked(api.deleteContribution).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useDeleteContribution());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ idKey: 'c1', batchIdKey: 'b1' });
    });
    expect(api.deleteContribution).toHaveBeenCalledWith('c1');
    expect(inv).toHaveBeenCalledWith({ queryKey: ['batches', 'b1', 'contributions'] });
  });
});

describe('fund hooks', () => {
  it('useActiveFunds fetches', async () => {
    vi.mocked(api.getActiveFunds).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useActiveFunds());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getActiveFunds).toHaveBeenCalled();
  });

  it('useAdminFunds fetches', async () => {
    vi.mocked(api.getAllFundsAdmin).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useAdminFunds());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
  });

  it('create / update / deactivate fund invalidate funds', async () => {
    vi.mocked(api.createFund).mockResolvedValueOnce({ idKey: 'f1' } as never);
    vi.mocked(api.updateFund).mockResolvedValueOnce({ idKey: 'f1' } as never);
    vi.mocked(api.deactivateFund).mockResolvedValueOnce(undefined as never);

    const c = renderHookWithQuery(() => useCreateFund());
    const u = renderHookWithQuery(() => useUpdateFund());
    const d = renderHookWithQuery(() => useDeactivateFund());
    const invC = vi.spyOn(c.client, 'invalidateQueries');
    const invU = vi.spyOn(u.client, 'invalidateQueries');
    const invD = vi.spyOn(d.client, 'invalidateQueries');

    await act(async () => {
      await c.result.current.mutateAsync({ name: 'General' } as never);
    });
    expect(invC).toHaveBeenCalledWith({ queryKey: ['funds'] });

    await act(async () => {
      await u.result.current.mutateAsync({ idKey: 'f1', request: { name: 'X' } as never });
    });
    expect(api.updateFund).toHaveBeenCalledWith('f1', { name: 'X' });
    expect(invU).toHaveBeenCalledWith({ queryKey: ['funds'] });

    await act(async () => {
      await d.result.current.mutateAsync('f1');
    });
    expect(invD).toHaveBeenCalledWith({ queryKey: ['funds'] });
  });
});

describe('campus hook (for filter dropdown)', () => {
  it('useCampuses calls reference.getCampuses', async () => {
    vi.mocked(referenceApi.getCampuses).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useCampuses());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(referenceApi.getCampuses).toHaveBeenCalled();
  });
});

describe('statements', () => {
  it('useStatements defaults to page 1, pageSize 25', async () => {
    vi.mocked(api.getStatements).mockResolvedValueOnce({ data: [] } as never);
    const { result } = renderHookWithQuery(() => useStatements());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getStatements).toHaveBeenCalledWith(1, 25);
  });

  it('useStatement idle without idKey, fires with', async () => {
    const idle = renderHookWithQuery(() => useStatement()).result;
    expect(idle.current.fetchStatus).toBe('idle');

    vi.mocked(api.getStatement).mockResolvedValueOnce({ idKey: 'st1' } as never);
    const { result } = renderHookWithQuery(() => useStatement('st1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getStatement).toHaveBeenCalledWith('st1');
  });

  it('useEligiblePeople disabled unless both startDate+endDate present', () => {
    const none = renderHookWithQuery(() => useEligiblePeople('', '')).result;
    expect(none.current.fetchStatus).toBe('idle');
  });

  it('useEligiblePeople fires with both dates', async () => {
    vi.mocked(api.getEligiblePeople).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() =>
      useEligiblePeople('2024-01-01', '2024-02-01', 25)
    );
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getEligiblePeople).toHaveBeenCalledWith('2024-01-01', '2024-02-01', 25);
  });

  it('useGenerateStatement invalidates statements', async () => {
    vi.mocked(api.generateStatement).mockResolvedValueOnce({ idKey: 'st1' } as never);
    const { result, client } = renderHookWithQuery(() => useGenerateStatement());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ startDate: 'a', endDate: 'b' } as never);
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['statements'] });
  });
});

describe('useDownloadStatementPdf', () => {
  it('creates an anchor, clicks it, and revokes the URL', async () => {
    const blob = new Blob(['%PDF'], { type: 'application/pdf' });
    vi.mocked(api.downloadStatementPdf).mockResolvedValueOnce(blob as never);

    const createURL = vi
      .spyOn(window.URL, 'createObjectURL')
      .mockReturnValue('blob:url');
    const revokeURL = vi.spyOn(window.URL, 'revokeObjectURL').mockImplementation(() => {});
    const clickSpy = vi.fn();
    const originalClick = HTMLAnchorElement.prototype.click;
    HTMLAnchorElement.prototype.click = clickSpy;

    try {
      const { result } = renderHookWithQuery(() => useDownloadStatementPdf());
      await act(async () => {
        await result.current.mutateAsync('st1');
      });
      expect(api.downloadStatementPdf).toHaveBeenCalledWith('st1');
      expect(createURL).toHaveBeenCalledWith(blob);
      expect(clickSpy).toHaveBeenCalled();
      expect(revokeURL).toHaveBeenCalledWith('blob:url');
    } finally {
      HTMLAnchorElement.prototype.click = originalClick;
      createURL.mockRestore();
      revokeURL.mockRestore();
    }
  });
});
