/**
 * BatchSummaryCard Component
 * Displays summary information for a contribution batch including
 * control amounts, actual amounts, variances, and balance status
 */

import { cn } from '@/lib/utils';
import { BatchStatusBadge } from './BatchStatusBadge';
import type { ContributionBatchDto, BatchSummaryDto } from '@/types/giving';

export interface BatchSummaryCardProps {
  batch: ContributionBatchDto;
  summary: BatchSummaryDto;
  onOpen: () => void;
  onClose: () => void;
  isOpenPending: boolean;
  isClosePending: boolean;
}

export function BatchSummaryCard({
  batch,
  summary,
  onOpen,
  onClose,
  isOpenPending,
  isClosePending,
}: BatchSummaryCardProps) {
  // Format currency with 2 decimals or '-' if undefined
  const formatCurrency = (amount: number | undefined): string => {
    if (amount === undefined) return '-';
    return `$${amount.toFixed(2)}`;
  };

  // Calculate variance (actualAmount - controlAmount)
  const variance = summary.variance;
  const varianceColor = variance < 0 ? 'text-red-600' : variance > 0 ? 'text-green-600' : 'text-gray-900';
  const variancePrefix = variance > 0 ? '+' : '';

  // Format batch date
  const formattedDate = new Date(batch.batchDate).toLocaleDateString();

  // Determine which button to show
  const showOpenButton = batch.status === 'Closed';
  const showCloseButton = batch.status === 'Open';
  const showNoButton = batch.status === 'Posted';

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      {/* Header */}
      <div className="flex items-start justify-between mb-4">
        <div>
          <h3 className="text-lg font-semibold text-gray-900">{batch.name}</h3>
          <p className="text-sm text-gray-600 mt-1">{formattedDate}</p>
        </div>
        <BatchStatusBadge status={batch.status} />
      </div>

      {/* Control Amounts */}
      <div className="space-y-3 mb-4">
        <div className="flex justify-between items-center">
          <span className="text-sm font-medium text-gray-500">Control Amount</span>
          <span className="text-sm text-gray-900">{formatCurrency(batch.controlAmount)}</span>
        </div>
        <div className="flex justify-between items-center">
          <span className="text-sm font-medium text-gray-500">Control Item Count</span>
          <span className="text-sm text-gray-900">
            {batch.controlItemCount !== undefined ? batch.controlItemCount : '-'}
          </span>
        </div>
      </div>

      {/* Divider */}
      <div className="border-t border-gray-200 my-4" />

      {/* Actual Amounts */}
      <div className="space-y-3 mb-4">
        <div className="flex justify-between items-center">
          <span className="text-sm font-medium text-gray-500">Actual Amount</span>
          <span className="text-sm font-semibold text-gray-900">{formatCurrency(summary.actualAmount)}</span>
        </div>
        <div className="flex justify-between items-center">
          <span className="text-sm font-medium text-gray-500">Contribution Count</span>
          <span className="text-sm font-semibold text-gray-900">{summary.contributionCount}</span>
        </div>
      </div>

      {/* Divider */}
      <div className="border-t border-gray-200 my-4" />

      {/* Variance */}
      <div className="space-y-3 mb-4">
        <div className="flex justify-between items-center">
          <span className="text-sm font-medium text-gray-500">Variance</span>
          <span className={cn('text-sm font-semibold', varianceColor)}>
            {variancePrefix}{formatCurrency(variance)}
          </span>
        </div>
        {summary.itemCountVariance !== undefined && (
          <div className="flex justify-between items-center">
            <span className="text-sm font-medium text-gray-500">Item Count Variance</span>
            <span className={cn('text-sm font-semibold', varianceColor)}>
              {variancePrefix}{summary.itemCountVariance}
            </span>
          </div>
        )}
      </div>

      {/* Balance Indicator */}
      <div className="flex items-center gap-2 mb-6">
        {summary.isBalanced ? (
          <>
            <svg
              className="w-5 h-5 text-green-600"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M5 13l4 4L19 7"
              />
            </svg>
            <span className="text-sm font-medium text-green-600">Balanced</span>
          </>
        ) : (
          <>
            <svg
              className="w-5 h-5 text-red-600"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            <span className="text-sm font-medium text-red-600">Unbalanced</span>
          </>
        )}
      </div>

      {/* Action Button */}
      {showOpenButton && (
        <button
          onClick={onOpen}
          disabled={isOpenPending}
          className="w-full px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isOpenPending ? (
            <div className="flex items-center justify-center gap-2">
              <svg
                className="animate-spin h-4 w-4"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  className="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  strokeWidth="4"
                />
                <path
                  className="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                />
              </svg>
              <span>Opening...</span>
            </div>
          ) : (
            'Open'
          )}
        </button>
      )}

      {showCloseButton && (
        <button
          onClick={onClose}
          disabled={isClosePending}
          className="w-full px-4 py-2 text-white bg-blue-600 rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isClosePending ? (
            <div className="flex items-center justify-center gap-2">
              <svg
                className="animate-spin h-4 w-4"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  className="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  strokeWidth="4"
                />
                <path
                  className="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                />
              </svg>
              <span>Closing...</span>
            </div>
          ) : (
            'Close'
          )}
        </button>
      )}

      {showNoButton && (
        <div className="text-center text-sm text-gray-500 py-2">
          Posted batches cannot be modified
        </div>
      )}
    </div>
  );
}
