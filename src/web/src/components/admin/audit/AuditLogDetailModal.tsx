/**
 * Audit Log Detail Modal Component
 * Displays detailed information about a single audit log entry
 */

import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { formatDateTime } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import type { AuditLogDto } from '@/services/api/types';

// ============================================================================
// Types
// ============================================================================

export interface AuditLogDetailModalProps {
  auditLog: AuditLogDto | null;
  isOpen: boolean;
  onClose: () => void;
}

// ============================================================================
// Helper Functions
// ============================================================================

function parseDiff(oldValues: string | undefined, newValues: string | undefined): {
  old: Record<string, unknown> | null;
  new: Record<string, unknown> | null;
  changed: Set<string>;
} {
  let old: Record<string, unknown> | null = null;
  let newObj: Record<string, unknown> | null = null;
  const changed = new Set<string>();

  try {
    if (oldValues) {
      old = JSON.parse(oldValues);
    }
  } catch {
    // Invalid JSON, ignore
  }

  try {
    if (newValues) {
      newObj = JSON.parse(newValues);
    }
  } catch {
    // Invalid JSON, ignore
  }

  // Find changed properties
  if (old && newObj) {
    const allKeys = new Set([...Object.keys(old), ...Object.keys(newObj)]);
    allKeys.forEach((key) => {
      if (JSON.stringify(old?.[key]) !== JSON.stringify(newObj?.[key])) {
        changed.add(key);
      }
    });
  }

  return { old, new: newObj, changed };
}

function formatJsonValue(value: unknown): string {
  if (value === null || value === undefined) {
    return 'null';
  }
  if (typeof value === 'string') {
    return value;
  }
  return JSON.stringify(value, null, 2);
}

// ============================================================================
// Component
// ============================================================================

export function AuditLogDetailModal({ auditLog, isOpen, onClose }: AuditLogDetailModalProps) {
  const dialogRef = useRef<HTMLDivElement>(null);
  const closeButtonRef = useRef<HTMLButtonElement>(null);
  const [copiedField, setCopiedField] = useState<string | null>(null);

  // Focus trap and escape key handling
  useEffect(() => {
    if (isOpen && closeButtonRef.current) {
      closeButtonRef.current.focus();
    }

    if (!isOpen) return;

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose();
        return;
      }

      if (event.key !== 'Tab' || !dialogRef.current) return;

      const focusableElements = dialogRef.current.querySelectorAll<HTMLElement>(
        'button:not(:disabled), [href], input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex]:not([tabindex="-1"]):not(:disabled)'
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
  }, [isOpen, onClose]);

  // Prevent body scroll when modal is open
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
      return () => {
        document.body.style.overflow = '';
      };
    }
  }, [isOpen]);

  const handleCopyToClipboard = async (text: string, field: string) => {
    try {
      await navigator.clipboard.writeText(text);
      setCopiedField(field);
      setTimeout(() => setCopiedField(null), 2000);
    } catch {
      // Clipboard API not available, ignore
    }
  };

  if (!isOpen || !auditLog) return null;

  const handleBackdropClick = (event: React.MouseEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget) {
      onClose();
    }
  };

  const { old, new: newObj, changed } = parseDiff(auditLog.oldValues, auditLog.newValues);

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
      onClick={handleBackdropClick}
      aria-labelledby="modal-title"
      aria-modal="true"
      role="dialog"
    >
      {/* Backdrop */}
      <div className="fixed inset-0 bg-black/50 transition-opacity" aria-hidden="true" />

      {/* Modal */}
      <div
        ref={dialogRef}
        className="relative bg-white rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] overflow-hidden flex flex-col"
      >
        {/* Header */}
        <div className="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
          <h2 id="modal-title" className="text-xl font-semibold text-gray-900">
            Audit Log Details
          </h2>
          <button
            ref={closeButtonRef}
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 transition-colors"
            aria-label="Close modal"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>
        </div>

        {/* Content */}
        <div className="px-6 py-4 overflow-y-auto flex-1">
          {/* Basic Information */}
          <div className="space-y-4 mb-6">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">
                  Timestamp
                </h3>
                <p className="text-sm text-gray-900">{formatDateTime(auditLog.timestamp)}</p>
              </div>
              <div>
                <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">
                  Action Type
                </h3>
                <p className="text-sm text-gray-900">{auditLog.actionType}</p>
              </div>
              <div>
                <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">
                  Entity Type
                </h3>
                <p className="text-sm text-gray-900">{auditLog.entityType}</p>
              </div>
              <div>
                <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">
                  Entity ID
                </h3>
                <p className="text-sm font-mono text-gray-600">{auditLog.entityIdKey}</p>
              </div>
              <div>
                <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">
                  User
                </h3>
                {auditLog.personIdKey ? (
                  <Link
                    to={`/admin/people/${auditLog.personIdKey}`}
                    className="text-sm text-blue-600 hover:text-blue-700 transition-colors"
                  >
                    {auditLog.personName || 'Unknown'}
                  </Link>
                ) : (
                  <p className="text-sm text-gray-500 italic">System</p>
                )}
              </div>
              {auditLog.ipAddress && (
                <div>
                  <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">
                    IP Address
                  </h3>
                  <p className="text-sm font-mono text-gray-600">{auditLog.ipAddress}</p>
                </div>
              )}
            </div>

            {auditLog.userAgent && (
              <div>
                <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">
                  User Agent
                </h3>
                <p className="text-sm text-gray-600 break-all">{auditLog.userAgent}</p>
              </div>
            )}

            {auditLog.additionalInfo && (
              <div>
                <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1">
                  Additional Info
                </h3>
                <p className="text-sm text-gray-900">{auditLog.additionalInfo}</p>
              </div>
            )}
          </div>

          {/* Changed Properties */}
          {old && newObj && changed.size > 0 && (
            <div className="border-t border-gray-200 pt-6">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-semibold text-gray-900">Changed Properties</h3>
                <button
                  onClick={() =>
                    handleCopyToClipboard(
                      JSON.stringify({ old, new: newObj }, null, 2),
                      'diff'
                    )
                  }
                  className="text-sm text-blue-600 hover:text-blue-700 transition-colors flex items-center gap-1"
                >
                  {copiedField === 'diff' ? (
                    <>
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M5 13l4 4L19 7"
                        />
                      </svg>
                      Copied!
                    </>
                  ) : (
                    <>
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M8 5H6a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2v-1M8 5a2 2 0 002 2h2a2 2 0 002-2M8 5a2 2 0 012-2h2a2 2 0 012 2m0 0h2a2 2 0 012 2v3m2 4H10m0 0l3-3m-3 3l3 3"
                        />
                      </svg>
                      Copy JSON
                    </>
                  )}
                </button>
              </div>

              <div className="space-y-4">
                {Array.from(changed).map((key) => (
                  <div key={key} className="bg-gray-50 rounded-lg p-4">
                    <h4 className="text-sm font-semibold text-gray-900 mb-2">{key}</h4>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <div>
                        <div className="text-xs font-medium text-red-700 mb-1">Old Value</div>
                        <pre className="text-xs bg-red-50 border border-red-200 rounded p-2 overflow-x-auto">
                          {formatJsonValue(old[key])}
                        </pre>
                      </div>
                      <div>
                        <div className="text-xs font-medium text-green-700 mb-1">New Value</div>
                        <pre className="text-xs bg-green-50 border border-green-200 rounded p-2 overflow-x-auto">
                          {formatJsonValue(newObj[key])}
                        </pre>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Raw JSON (if no diff available) */}
          {(!old || !newObj) && (auditLog.oldValues || auditLog.newValues) && (
            <div className="border-t border-gray-200 pt-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">Raw Data</h3>
              {auditLog.oldValues && (
                <div className="mb-4">
                  <div className="flex items-center justify-between mb-2">
                    <h4 className="text-sm font-medium text-gray-700">Old Values</h4>
                    <button
                      onClick={() => handleCopyToClipboard(auditLog.oldValues!, 'old')}
                      className="text-sm text-blue-600 hover:text-blue-700 transition-colors"
                    >
                      {copiedField === 'old' ? 'Copied!' : 'Copy'}
                    </button>
                  </div>
                  <pre className="text-xs bg-gray-50 border border-gray-200 rounded p-3 overflow-x-auto">
                    {auditLog.oldValues}
                  </pre>
                </div>
              )}
              {auditLog.newValues && (
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <h4 className="text-sm font-medium text-gray-700">New Values</h4>
                    <button
                      onClick={() => handleCopyToClipboard(auditLog.newValues!, 'new')}
                      className="text-sm text-blue-600 hover:text-blue-700 transition-colors"
                    >
                      {copiedField === 'new' ? 'Copied!' : 'Copy'}
                    </button>
                  </div>
                  <pre className="text-xs bg-gray-50 border border-gray-200 rounded p-3 overflow-x-auto">
                    {auditLog.newValues}
                  </pre>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-gray-200 flex justify-end">
          <Button onClick={onClose} variant="primary">
            Close
          </Button>
        </div>
      </div>
    </div>
  );
}
