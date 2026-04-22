/**
 * useImport — wizard-state state machine (pure, no TanStack Query).
 */

import { describe, it, expect } from 'vitest';
import { act, renderHook } from '@testing-library/react';
import { useImport } from '../useImport';

describe('useImport', () => {
  it('starts on the upload step with empty state', () => {
    const { result } = renderHook(() => useImport());
    expect(result.current.currentStep).toBe('upload');
    expect(result.current.file).toBeNull();
    expect(result.current.csvHeaders).toEqual([]);
    expect(result.current.previewRows).toEqual([]);
    expect(result.current.fieldMappings).toEqual([]);
  });

  it('handleFileSelected advances to the mapping step with the file + preview', () => {
    const { result } = renderHook(() => useImport());
    const file = new File(['a,b'], 'x.csv', { type: 'text/csv' });
    act(() => {
      result.current.handleFileSelected(file, ['a', 'b'], [['1', '2']]);
    });
    expect(result.current.currentStep).toBe('mapping');
    expect(result.current.file).toBe(file);
    expect(result.current.csvHeaders).toEqual(['a', 'b']);
    expect(result.current.previewRows).toEqual([['1', '2']]);
  });

  it('handleMappingsChange replaces mappings without changing step', () => {
    const { result } = renderHook(() => useImport());
    const mapping = [{ csvColumn: 'a', field: 'firstName' }] as never;
    act(() => {
      result.current.handleMappingsChange(mapping);
    });
    expect(result.current.fieldMappings).toEqual(mapping);
    expect(result.current.currentStep).toBe('upload');
  });

  it('goToStep navigates explicitly', () => {
    const { result } = renderHook(() => useImport());
    act(() => {
      result.current.goToStep('validation');
    });
    expect(result.current.currentStep).toBe('validation');
    act(() => {
      result.current.goToStep('progress');
    });
    expect(result.current.currentStep).toBe('progress');
  });

  it('reset returns to the initial blank state', () => {
    const { result } = renderHook(() => useImport());
    const file = new File(['x'], 'x.csv');
    act(() => {
      result.current.handleFileSelected(file, ['a'], [['1']]);
      result.current.handleMappingsChange([{ csvColumn: 'a' }] as never);
      result.current.goToStep('progress');
    });
    act(() => {
      result.current.reset();
    });
    expect(result.current.currentStep).toBe('upload');
    expect(result.current.file).toBeNull();
    expect(result.current.csvHeaders).toEqual([]);
    expect(result.current.previewRows).toEqual([]);
    expect(result.current.fieldMappings).toEqual([]);
  });
});
