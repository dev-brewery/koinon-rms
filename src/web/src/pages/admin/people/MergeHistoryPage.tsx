/**
 * Merge History Page
 * View historical record of person merges
 */

import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useMergeHistory } from '@/hooks/usePersonMerge';
import { Loading, EmptyState, ErrorState } from '@/components/ui';

export function MergeHistoryPage() {
  const navigate = useNavigate();
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const { data, isLoading, error, refetch } = useMergeHistory(currentPage, pageSize);

  const history = data?.data || [];
  const meta = data?.meta;

  const formatDateTime = (dateTime: string): string => {
    const date = new Date(dateTime);
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    }).format(date);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Merge History</h1>
          <p className="mt-2 text-gray-600">
            Historical record of completed person merges
          </p>
        </div>
        <button
          onClick={() => navigate('/admin/people/duplicates')}
          className="px-4 py-2 text-primary-600 border border-primary-600 rounded-lg hover:bg-primary-50 transition-colors"
        >
          Review Duplicates
        </button>
      </div>

      {/* Table */}
      <div className="bg-white rounded-lg border border-gray-200">
        {isLoading ? (
          <Loading text="Loading merge history..." />
        ) : error ? (
          <ErrorState
            title="Failed to load merge history"
            message={error instanceof Error ? error.message : 'Unknown error'}
            onRetry={() => refetch()}
          />
        ) : history.length === 0 ? (
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
                  d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            }
            title="No merge history"
            description="No person merges have been performed yet"
          />
        ) : (
          <>
            {/* Table */}
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Date
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Survivor Person
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Merged Person
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Merged By
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Notes
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {history.map((record) => (
                    <tr key={record.idKey} className="hover:bg-gray-50">
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        {formatDateTime(record.mergedDateTime)}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <Link
                          to={`/admin/people/${record.survivorIdKey}`}
                          className="text-sm font-medium text-primary-600 hover:text-primary-800"
                        >
                          {record.survivorName}
                        </Link>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {record.mergedName}
                        <span className="ml-2 text-xs text-gray-400">(merged)</span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        {record.mergedByName || <span className="text-gray-400 italic">Unknown</span>}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-500">
                        {record.notes || (
                          <span className="text-gray-400 italic">No notes</span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {meta && meta.totalPages > 1 && (
              <div className="px-4 py-3 border-t border-gray-200 flex items-center justify-between">
                <div className="flex items-center gap-2 text-sm text-gray-700">
                  <span>Show</span>
                  <select
                    value={pageSize}
                    onChange={(e) => {
                      setPageSize(Number(e.target.value));
                      setCurrentPage(1);
                    }}
                    className="px-2 py-1 border border-gray-300 rounded focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  >
                    <option value={25}>25</option>
                    <option value={50}>50</option>
                    <option value={100}>100</option>
                  </select>
                  <span>of {meta.totalCount} total</span>
                </div>

                <div className="flex items-center gap-2">
                  <button
                    onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                    disabled={currentPage === 1}
                    className="px-3 py-1 border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Previous
                  </button>
                  <span className="text-sm text-gray-700">
                    Page {currentPage} of {meta.totalPages}
                  </span>
                  <button
                    onClick={() => setCurrentPage((p) => Math.min(meta.totalPages, p + 1))}
                    disabled={currentPage === meta.totalPages}
                    className="px-3 py-1 border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Next
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
