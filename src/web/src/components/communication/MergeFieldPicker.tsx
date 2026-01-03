/**
 * MergeFieldPicker Component
 * Dropdown button that displays available merge fields for inserting into message templates
 */

import { useState, useRef, useEffect } from 'react';
import { useMergeFields } from '@/hooks/useCommunications';

export interface MergeField {
  name: string;
  token: string;
  description: string;
}

export interface MergeFieldPickerProps {
  onInsert: (token: string) => void;
  disabled?: boolean;
}

export function MergeFieldPicker({ onInsert, disabled = false }: MergeFieldPickerProps) {
  const [isOpen, setIsOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const { data: mergeFields = [] } = useMergeFields();

  // Close dropdown when clicking outside or pressing Escape
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }

    function handleEscapeKey(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setIsOpen(false);
      }
    }

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
      document.addEventListener('keydown', handleEscapeKey);
      return () => {
        document.removeEventListener('mousedown', handleClickOutside);
        document.removeEventListener('keydown', handleEscapeKey);
      };
    }
  }, [isOpen]);

  const handleFieldClick = (token: string) => {
    onInsert(token);
    setIsOpen(false);
  };

  return (
    <div className="relative" ref={menuRef}>
      <button
        id="merge-field-button"
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        disabled={disabled}
        aria-haspopup="true"
        aria-expanded={isOpen}
        aria-label="Insert merge field"
        className="inline-flex items-center gap-2 px-3 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:bg-white transition-colors"
      >
        <svg
          className="w-4 h-4"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z"
          />
        </svg>
        Insert Field
        <svg
          className={`w-4 h-4 text-gray-400 transition-transform ${isOpen ? 'rotate-180' : ''}`}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
        </svg>
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <div
          role="menu"
          aria-labelledby="merge-field-button"
          className="absolute left-0 mt-2 w-72 bg-white rounded-lg shadow-lg border border-gray-200 py-1 z-50 max-h-80 overflow-y-auto"
        >
          <div className="px-4 py-2 border-b border-gray-200">
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider">
              Available Fields
            </p>
          </div>

          {mergeFields.map((field) => (
            <button
              key={field.token}
              type="button"
              role="menuitem"
              onClick={() => handleFieldClick(field.token)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault();
                  handleFieldClick(field.token);
                }
              }}
              className="w-full px-4 py-3 text-left hover:bg-gray-50 focus:bg-gray-50 focus:outline-none transition-colors"
            >
              <div className="flex items-start gap-3">
                <div className="flex-shrink-0 w-5 h-5 mt-0.5 text-primary-600">
                  <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                    />
                  </svg>
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-900">{field.name}</p>
                  <p className="text-xs text-gray-500 mt-0.5">{field.description}</p>
                  <p className="text-xs text-primary-600 font-mono mt-1">{field.token}</p>
                </div>
              </div>
            </button>
          ))}

          {mergeFields.length === 0 && (
            <div className="px-4 py-8 text-center">
              <p className="text-sm text-gray-500">No merge fields available</p>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
