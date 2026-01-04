/**
 * useImport hook
 * Manages multi-step import wizard state
 */

import { useState, useCallback } from 'react';
import type { FieldMapping } from '@/components/import/FieldMappingEditor';

export type ImportStep = 'upload' | 'mapping' | 'validation' | 'progress';

export interface UseImportState {
  currentStep: ImportStep;
  file: File | null;
  csvHeaders: string[];
  previewRows: string[][];
  fieldMappings: FieldMapping[];
}

export function useImport() {
  const [state, setState] = useState<UseImportState>({
    currentStep: 'upload',
    file: null,
    csvHeaders: [],
    previewRows: [],
    fieldMappings: [],
  });

  const handleFileSelected = useCallback(
    (file: File, headers: string[], previewRows: string[][]) => {
      setState((prev) => ({
        ...prev,
        currentStep: 'mapping',
        file,
        csvHeaders: headers,
        previewRows,
      }));
    },
    []
  );

  const handleMappingsChange = useCallback((mappings: FieldMapping[]) => {
    setState((prev) => ({
      ...prev,
      fieldMappings: mappings,
    }));
  }, []);

  const goToStep = useCallback((step: ImportStep) => {
    setState((prev) => ({
      ...prev,
      currentStep: step,
    }));
  }, []);

  const reset = useCallback(() => {
    setState({
      currentStep: 'upload',
      file: null,
      csvHeaders: [],
      previewRows: [],
      fieldMappings: [],
    });
  }, []);

  return {
    ...state,
    handleFileSelected,
    handleMappingsChange,
    goToStep,
    reset,
  };
}
