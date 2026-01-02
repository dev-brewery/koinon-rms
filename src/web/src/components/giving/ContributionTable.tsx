/**
 * Contribution Table Component
 * Displays contributions with expandable fund details and edit/delete actions
 */

import React, { useState } from 'react';
import { cn } from '@/lib/utils';
import { EmptyState, ConfirmDialog } from '@/components/ui';
import type { ContributionDto } from '@/types/giving';

// ============================================================================
// Types
// ============================================================================

export interface ContributionTableProps {
  contributions: ContributionDto[];
  batchStatus: string;
  onEdit: (contribution: ContributionDto) => void;
  onDelete: (contributionIdKey: string) => void;
  isDeleting?: boolean;
}

// ============================================================================
// Component
// ============================================================================

export function ContributionTable({
  contributions,
  batchStatus,
  onEdit,
  onDelete,
  isDeleting = false,
}: ContributionTableProps) {
  const [expandedRowId, setExpandedRowId] = useState<string | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [contributionToDelete, setContributionToDelete] = useState<string | null>(null);

  const isEditable = batchStatus === 'Open';

  const handleRowClick = (idKey: string) => {
    setExpandedRowId((current) => (current === idKey ? null : idKey));
  };

  const handleDeleteClick = (idKey: string, e: React.MouseEvent) => {
    e.stopPropagation(); // Prevent row expansion
    setContributionToDelete(idKey);
    setDeleteDialogOpen(true);
  };

  const handleConfirmDelete = () => {
    if (contributionToDelete) {
      onDelete(contributionToDelete);
      setDeleteDialogOpen(false);
      setContributionToDelete(null);
    }
  };

  const handleCancelDelete = () => {
    setDeleteDialogOpen(false);
    setContributionToDelete(null);
  };

  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  const formatDateTime = (dateTime: string): string => {
    return new Date(dateTime).toLocaleString();
  };

  const getFundsList = (contribution: ContributionDto): string => {
    return contribution.details.map((d) => d.fundName).join(', ');
  };

  // Empty state
  if (contributions.length === 0) {
    return (
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
              d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
        }
        title="No contributions yet"
        description="Add contributions to this batch to get started"
      />
    );
  }

  return (
    <>
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Contributor
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Date
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Funds
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Amount
              </th>
              {isEditable && (
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              )}
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {contributions.map((contribution) => {
              const isExpanded = expandedRowId === contribution.idKey;
              return (
                <React.Fragment key={contribution.idKey}>
                  {/* Main Row */}
                  <tr
                    onClick={() => handleRowClick(contribution.idKey)}
                    className={cn(
                      'cursor-pointer transition-colors',
                      isExpanded ? 'bg-blue-50' : 'hover:bg-gray-50'
                    )}
                  >
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center gap-2">
                        <svg
                          className={cn(
                            'w-4 h-4 text-gray-400 transition-transform',
                            isExpanded && 'rotate-90'
                          )}
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M9 5l7 7-7 7"
                          />
                        </svg>
                        <span className="text-sm font-medium text-gray-900">
                          {contribution.personName || 'Anonymous'}
                        </span>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {formatDateTime(contribution.transactionDateTime)}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-900">
                      <div className="max-w-xs truncate" title={getFundsList(contribution)}>
                        {getFundsList(contribution)}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right font-medium">
                      {formatCurrency(contribution.totalAmount)}
                    </td>
                    {isEditable && (
                      <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                        <div className="flex items-center justify-end gap-3">
                          <button
                            onClick={(e) => {
                              e.stopPropagation();
                              onEdit(contribution);
                            }}
                            className="text-primary-600 hover:text-primary-700 transition-colors"
                            aria-label={`Edit contribution for ${contribution.personName || 'Anonymous'}`}
                          >
                            Edit
                          </button>
                          <button
                            onClick={(e) => handleDeleteClick(contribution.idKey, e)}
                            disabled={isDeleting}
                            className="text-red-600 hover:text-red-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                            aria-label={`Delete contribution for ${contribution.personName || 'Anonymous'}`}
                          >
                            Delete
                          </button>
                        </div>
                      </td>
                    )}
                  </tr>

                  {/* Expanded Details Row */}
                  {isExpanded && (
                    <tr className="bg-blue-50">
                      <td colSpan={isEditable ? 5 : 4} className="px-6 py-4">
                        <div className="ml-10">
                          <h4 className="text-xs font-semibold text-gray-700 uppercase tracking-wider mb-2">
                            Fund Details
                          </h4>
                          <div className="space-y-2">
                            {contribution.details.map((detail) => (
                              <div
                                key={detail.idKey}
                                className="flex items-start justify-between py-2 px-3 bg-white rounded border border-gray-200"
                              >
                                <div className="flex-1">
                                  <div className="text-sm font-medium text-gray-900">
                                    {detail.fundName}
                                  </div>
                                  {detail.summary && (
                                    <div className="text-xs text-gray-600 mt-1">
                                      {detail.summary}
                                    </div>
                                  )}
                                </div>
                                <div className="text-sm font-medium text-gray-900 ml-4">
                                  {formatCurrency(detail.amount)}
                                </div>
                              </div>
                            ))}
                          </div>
                          {contribution.summary && (
                            <div className="mt-3 pt-3 border-t border-gray-200">
                              <div className="text-xs font-semibold text-gray-700 uppercase tracking-wider mb-1">
                                Contribution Note
                              </div>
                              <div className="text-sm text-gray-900">
                                {contribution.summary}
                              </div>
                            </div>
                          )}
                        </div>
                      </td>
                    </tr>
                  )}
                </React.Fragment>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Delete Confirmation Dialog */}
      <ConfirmDialog
        isOpen={deleteDialogOpen}
        onClose={handleCancelDelete}
        onConfirm={handleConfirmDelete}
        title="Delete Contribution?"
        description="This action cannot be undone. The contribution will be permanently removed from this batch."
        confirmLabel="Delete"
        cancelLabel="Cancel"
        variant="danger"
        isLoading={isDeleting}
      />
    </>
  );
}
