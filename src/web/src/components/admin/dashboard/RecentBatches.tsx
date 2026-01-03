/**
 * Recent Batches Component
 * Displays list of recent giving batches with status indicators
 */

import { Link } from 'react-router-dom';
import type { BatchSummary } from '@/types';

export interface RecentBatchesProps {
  batches: BatchSummary[];
}

export function RecentBatches({ batches }: RecentBatchesProps) {
  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(amount);
  };

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  const getStatusColor = (status: BatchSummary['status']): string => {
    switch (status) {
      case 'Open':
        return 'text-blue-700 bg-blue-50 border-blue-200';
      case 'Pending':
        return 'text-yellow-700 bg-yellow-50 border-yellow-200';
      case 'Closed':
        return 'text-green-700 bg-green-50 border-green-200';
      default:
        return 'text-gray-700 bg-gray-50 border-gray-200';
    }
  };

  if (batches.length === 0) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Recent Batches</h2>
        <div className="text-center py-12">
          <svg
            className="w-12 h-12 text-gray-400 mx-auto mb-4"
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
          <p className="text-gray-500">No recent batches</p>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <h2 className="text-lg font-semibold text-gray-900 mb-4">Recent Batches</h2>
      <div className="space-y-3">
        {batches.slice(0, 5).map((batch) => (
          <Link
            key={batch.idKey}
            to={`/admin/giving/batches/${batch.idKey}`}
            className="flex items-center justify-between p-4 rounded-lg border border-gray-200 hover:border-primary-300 hover:bg-primary-50 transition-all group"
          >
            <div className="flex-1 min-w-0">
              <h3 className="text-sm font-semibold text-gray-900 group-hover:text-primary-700">
                {batch.name}
              </h3>
              <p className="mt-1 text-sm text-gray-500">
                {formatDate(batch.batchDate)} • {formatCurrency(batch.total)}
              </p>
            </div>
            <div className={`px-3 py-1 text-xs font-medium rounded-full border ${getStatusColor(batch.status)}`}>
              {batch.status}
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
