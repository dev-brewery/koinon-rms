/**
 * Save Template Dialog Component
 * Modal dialog for saving field mappings as a reusable template
 */

import { useState, useEffect, useRef, useId } from 'react';
import { cn } from '@/lib/utils';
import { Button } from '../ui/Button';
import { Input } from '../ui/Input';
import type { ImportType } from '@/types/template';
import { getImportTypeLabel } from '@/types/template';

export interface SaveTemplateDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (name: string) => void;
  importType: ImportType;
  mappedColumnsCount: number;
  isLoading?: boolean;
}

export function SaveTemplateDialog({
  isOpen,
  onClose,
  onSave,
  importType,
  mappedColumnsCount,
  isLoading = false,
}: SaveTemplateDialogProps) {
  const [name, setName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const dialogRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const inputId = useId();

  useEffect(() => {
    if (isOpen) {
      setName('');
      setError(null);
      setTimeout(() => inputRef.current?.focus(), 0);
    }
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) return;

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !isLoading) {
        onClose();
      }

      if (event.key !== 'Tab' || !dialogRef.current) return;

      const focusableElements = dialogRef.current.querySelectorAll<HTMLElement>(
        'button:not(:disabled), input:not(:disabled), [tabindex]:not([tabindex="-1"]):not(:disabled)'
      );

      const firstElement = focusableElements[0];
      const lastElement = focusableElements[focusableElements.length - 1];

      if (event.shiftKey) {
        if (document.activeElement === firstElement) {
          event.preventDefault();
          lastElement?.focus();
        }
      } else {
        if (document.activeElement === lastElement) {
          event.preventDefault();
          firstElement?.focus();
        }
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, isLoading, onClose]);

  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
      return () => {
        document.body.style.overflow = '';
      };
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const handleBackdropClick = (event: React.MouseEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget && !isLoading) {
      onClose();
    }
  };

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    const trimmedName = name.trim();

    if (!trimmedName) {
      setError('Template name is required');
      return;
    }

    if (trimmedName.length < 2) {
      setError('Template name must be at least 2 characters');
      return;
    }

    if (trimmedName.length > 50) {
      setError('Template name must be less than 50 characters');
      return;
    }

    onSave(trimmedName);
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
      onClick={handleBackdropClick}
      aria-labelledby="save-template-title"
      aria-modal="true"
      role="dialog"
    >
      <div className="fixed inset-0 bg-black/50 transition-opacity" aria-hidden="true" />

      <div
        ref={dialogRef}
        className="relative bg-white rounded-lg shadow-xl max-w-md w-full transform transition-all"
      >
        <form onSubmit={handleSubmit}>
          <div className="p-6">
            <div className="flex items-start gap-4">
              <div
                className={cn(
                  'flex-shrink-0 w-12 h-12 rounded-full flex items-center justify-center',
                  'bg-blue-100 text-blue-600'
                )}
              >
                <svg
                  className="w-6 h-6"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M8 7H5a2 2 0 00-2 2v9a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-3m-1 4l-3 3m0 0l-3-3m3 3V4"
                  />
                </svg>
              </div>

              <div className="flex-1 min-w-0">
                <h3 id="save-template-title" className="text-lg font-semibold text-gray-900">
                  Save as Template
                </h3>
                <p className="mt-1 text-sm text-gray-600">
                  Save your field mappings to reuse them in future imports.
                </p>
              </div>
            </div>

            <div className="mt-6 space-y-4">
              <Input
                ref={inputRef}
                id={inputId}
                label="Template Name"
                value={name}
                onChange={e => {
                  setName(e.target.value);
                  setError(null);
                }}
                placeholder="e.g., Planning Center Export"
                error={error || undefined}
                disabled={isLoading}
                aria-describedby="template-info"
              />

              <div
                id="template-info"
                className="bg-gray-50 border border-gray-200 rounded-lg p-4"
              >
                <dl className="grid grid-cols-2 gap-3 text-sm">
                  <div>
                    <dt className="text-gray-500">Import Type</dt>
                    <dd className="font-medium text-gray-900">
                      {getImportTypeLabel(importType)}
                    </dd>
                  </div>
                  <div>
                    <dt className="text-gray-500">Mapped Columns</dt>
                    <dd className="font-medium text-gray-900">
                      {mappedColumnsCount}
                    </dd>
                  </div>
                </dl>
              </div>
            </div>

            <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <button
                type="button"
                onClick={onClose}
                disabled={isLoading}
                className={cn(
                  'px-4 py-2 text-sm font-medium text-gray-700',
                  'border border-gray-300 rounded-lg',
                  'hover:bg-gray-50 active:bg-gray-100',
                  'focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                  'disabled:cursor-not-allowed disabled:opacity-50',
                  'transition-colors'
                )}
              >
                Cancel
              </button>
              <Button
                type="submit"
                disabled={isLoading}
                loading={isLoading}
              >
                Save Template
              </Button>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}
