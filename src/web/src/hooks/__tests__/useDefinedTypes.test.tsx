/**
 * useDefinedTypes / useDefinedTypeValues.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor } from '@testing-library/react';

vi.mock('@/services/api/reference', () => ({
  getDefinedTypes: vi.fn(),
  getDefinedTypeValues: vi.fn(),
}));

import * as referenceApi from '@/services/api/reference';
import { useDefinedTypes, useDefinedTypeValues } from '../useDefinedTypes';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useDefinedTypes', () => {
  it('calls getDefinedTypes', async () => {
    vi.mocked(referenceApi.getDefinedTypes).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useDefinedTypes());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(referenceApi.getDefinedTypes).toHaveBeenCalled();
  });
});

describe('useDefinedTypeValues', () => {
  it('idle when idKeyOrGuid is undefined', () => {
    const { result } = renderHookWithQuery(() => useDefinedTypeValues(undefined));
    expect(result.current.fetchStatus).toBe('idle');
    expect(referenceApi.getDefinedTypeValues).not.toHaveBeenCalled();
  });

  it('idle for empty string', () => {
    const { result } = renderHookWithQuery(() => useDefinedTypeValues(''));
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fetches when idKeyOrGuid is provided', async () => {
    vi.mocked(referenceApi.getDefinedTypeValues).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useDefinedTypeValues('dt1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(referenceApi.getDefinedTypeValues).toHaveBeenCalledWith('dt1');
  });
});
