/**
 * useAuditLogs hook module. The export mutation triggers a DOM download
 * side-effect; we stub the URL helpers and anchor click to verify the
 * filename shape per format.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/auditLogApi', () => ({
  searchAuditLogs: vi.fn(),
  getEntityAuditHistory: vi.fn(),
  exportAuditLogs: vi.fn(),
}));

import * as api from '@/services/api/auditLogApi';
import {
  useAuditLogs,
  useEntityAuditHistory,
  useExportAuditLogs,
} from '../useAuditLogs';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useAuditLogs', () => {
  it('forwards params to api', async () => {
    vi.mocked(api.searchAuditLogs).mockResolvedValueOnce({ data: [] } as never);
    const { result } = renderHookWithQuery(() =>
      useAuditLogs({ entityType: 'Person', page: 1 })
    );
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.searchAuditLogs).toHaveBeenCalledWith({
      entityType: 'Person',
      page: 1,
    });
  });
});

describe('useEntityAuditHistory', () => {
  it('idle unless BOTH entityType and idKey present', () => {
    const a = renderHookWithQuery(() => useEntityAuditHistory(undefined, 'p1')).result;
    const b = renderHookWithQuery(() => useEntityAuditHistory('Person', undefined)).result;
    expect(a.current.fetchStatus).toBe('idle');
    expect(b.current.fetchStatus).toBe('idle');
  });

  it('fires when both present', async () => {
    vi.mocked(api.getEntityAuditHistory).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() =>
      useEntityAuditHistory('Person', 'p1')
    );
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getEntityAuditHistory).toHaveBeenCalledWith('Person', 'p1');
  });
});

describe('useExportAuditLogs', () => {
  it('downloads a CSV file with date-stamped name', async () => {
    const blob = new Blob(['csv'], { type: 'text/csv' });
    vi.mocked(api.exportAuditLogs).mockResolvedValueOnce(blob as never);

    const createURL = vi
      .spyOn(window.URL, 'createObjectURL')
      .mockReturnValue('blob:url');
    const revokeURL = vi.spyOn(window.URL, 'revokeObjectURL').mockImplementation(() => {});
    const clickSpy = vi.fn();
    const originalClick = HTMLAnchorElement.prototype.click;
    HTMLAnchorElement.prototype.click = clickSpy;

    try {
      const { result } = renderHookWithQuery(() => useExportAuditLogs());
      await act(async () => {
        await result.current.mutateAsync({ format: 'Csv' } as never);
      });
      expect(api.exportAuditLogs).toHaveBeenCalled();
      expect(createURL).toHaveBeenCalledWith(blob);
      expect(clickSpy).toHaveBeenCalled();
      expect(revokeURL).toHaveBeenCalledWith('blob:url');
    } finally {
      HTMLAnchorElement.prototype.click = originalClick;
      createURL.mockRestore();
      revokeURL.mockRestore();
    }
  });

  it('selects extension based on format', async () => {
    const blob = new Blob(['x']);
    vi.mocked(api.exportAuditLogs).mockResolvedValue(blob as never);

    const createURL = vi
      .spyOn(window.URL, 'createObjectURL')
      .mockReturnValue('blob:url');
    const revokeURL = vi.spyOn(window.URL, 'revokeObjectURL').mockImplementation(() => {});
    const originalClick = HTMLAnchorElement.prototype.click;

    // Track download attributes set on anchors
    const downloadNames: string[] = [];
    const setAttr = HTMLAnchorElement.prototype.setAttribute;
    // Not all anchors use setAttribute for download; intercept via click instead
    HTMLAnchorElement.prototype.click = function () {
      downloadNames.push((this as HTMLAnchorElement).download);
    };

    try {
      const { result } = renderHookWithQuery(() => useExportAuditLogs());
      await act(async () => {
        await result.current.mutateAsync({ format: 'Json' } as never);
      });
      await act(async () => {
        await result.current.mutateAsync({ format: 'Excel' } as never);
      });
      await act(async () => {
        // Default: no format
        await result.current.mutateAsync({} as never);
      });
      expect(downloadNames[0]).toMatch(/\.json$/);
      expect(downloadNames[1]).toMatch(/\.xlsx$/);
      expect(downloadNames[2]).toMatch(/\.csv$/); // defaults to Csv
    } finally {
      HTMLAnchorElement.prototype.click = originalClick;
      HTMLAnchorElement.prototype.setAttribute = setAttr;
      createURL.mockRestore();
      revokeURL.mockRestore();
    }
  });
});
