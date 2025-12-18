/**
 * Template Manager Component
 * Card-based list for managing saved import templates with delete confirmation
 */

import { useState, useEffect } from 'react';
import { Card } from '../ui/Card';
import { Button } from '../ui/Button';
import { EmptyState } from '../ui/EmptyState';
import { ConfirmDialog } from '../ui/ConfirmDialog';
import type { ImportTemplate } from '@/types/template';
import {
  loadTemplates,
  deleteTemplate,
  formatRelativeTime,
  getImportTypeLabel,
} from '@/types/template';

export interface TemplateManagerProps {
  onTemplateDeleted?: () => void;
}

export function TemplateManager({ onTemplateDeleted }: TemplateManagerProps) {
  const [templates, setTemplates] = useState<ImportTemplate[]>([]);
  const [deleteTarget, setDeleteTarget] = useState<ImportTemplate | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  useEffect(() => {
    refreshTemplates();
  }, []);

  const refreshTemplates = () => {
    const loaded = loadTemplates();
    setTemplates(loaded);
  };

  const handleDeleteClick = (template: ImportTemplate) => {
    setDeleteTarget(template);
  };

  const handleDeleteConfirm = () => {
    if (!deleteTarget) return;

    setIsDeleting(true);
    const success = deleteTemplate(deleteTarget.id);

    if (success) {
      refreshTemplates();
      onTemplateDeleted?.();
    }

    setIsDeleting(false);
    setDeleteTarget(null);
  };

  const handleDeleteCancel = () => {
    setDeleteTarget(null);
  };

  if (templates.length === 0) {
    return (
      <Card className="p-0">
        <EmptyState
          icon={
            <svg
              className="w-12 h-12 text-gray-400"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
              />
            </svg>
          }
          title="No templates saved"
          description="Save your field mappings as templates during import to reuse them later."
        />
      </Card>
    );
  }

  return (
    <>
      <div className="space-y-4">
        {templates.map(template => {
          const mappedCount = template.mappings.filter(m => m.targetField !== null).length;
          const totalCount = template.mappings.length;

          return (
            <Card
              key={template.id}
              className="p-4"
            >
              <div className="flex items-start justify-between gap-4">
                <div className="flex-1 min-w-0">
                  <h3 className="text-base font-semibold text-gray-900 truncate">
                    {template.name}
                  </h3>
                  <div className="mt-1 flex flex-wrap gap-x-4 gap-y-1 text-sm text-gray-500">
                    <span className="inline-flex items-center gap-1">
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
                      {getImportTypeLabel(template.importType)}
                    </span>
                    <span className="inline-flex items-center gap-1">
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
                          d="M4 6h16M4 10h16M4 14h16M4 18h16"
                        />
                      </svg>
                      {mappedCount} of {totalCount} columns mapped
                    </span>
                    <span className="inline-flex items-center gap-1">
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
                          d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
                        />
                      </svg>
                      {formatRelativeTime(template.createdAt)}
                    </span>
                  </div>
                </div>

                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleDeleteClick(template)}
                  aria-label={`Delete template ${template.name}`}
                  className="text-red-600 hover:text-red-700 hover:bg-red-50"
                >
                  <svg
                    className="w-5 h-5"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                    />
                  </svg>
                </Button>
              </div>
            </Card>
          );
        })}
      </div>

      <ConfirmDialog
        isOpen={deleteTarget !== null}
        onClose={handleDeleteCancel}
        onConfirm={handleDeleteConfirm}
        title="Delete Template"
        description={`Are you sure you want to delete "${deleteTarget?.name}"? This action cannot be undone.`}
        confirmLabel="Delete"
        cancelLabel="Cancel"
        variant="danger"
        isLoading={isDeleting}
      />
    </>
  );
}
