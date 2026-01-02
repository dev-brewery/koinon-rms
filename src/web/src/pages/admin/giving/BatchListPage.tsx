/**
 * Batch List Page
 * Search and filter giving batches
 */

import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useBatches, useCampuses, useOpenBatch, useCloseBatch } from '@/hooks/useGiving';
import { Loading, EmptyState, ErrorState } from '@/components/ui';
import { BatchStatusBadge } from '@/components/giving';
import { useToast } from '@/contexts/ToastContext';
import type { BatchFilterParams } from '@/services/api/giving';

export function BatchListPage() {
  const navigate = useNavigate();
  const toast = useToast();
  const [status, setStatus] = useState<string | undefined>();
  const [startDate, setStartDate] = useState<string | undefined>();
  const [endDate, setEndDate] = useState<string | undefined>();
  const [campusIdKey, setCampusIdKey] = useState<string | undefined>();
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const params: BatchFilterParams = {
    status,
    campusIdKey,
    startDate,
    endDate,
    page: currentPage,
    pageSize,
  };

  const { data, isLoading, error, refetch } = useBatches(params);
  const { data: campusesData, isLoading: campusesLoading } = useCampuses();
  const openBatch = useOpenBatch();
  const closeBatch = useCloseBatch();

  const batches = data?.data || [];
  const meta = data?.meta;
  const campuses = campusesData || [];

  const handlePageSizeChange = (newSize: number) => {
    setPageSize(newSize);
    setCurrentPage(1);
  };

  const handleOpenBatch = async (idKey: string) => {
    try {
      await openBatch.mutateAsync(idKey);
      toast.success('Batch opened', 'The batch is now available for editing');
    } catch (error) {
      toast.error('Failed to open batch', 'Please try again later');
    }
  };

  const handleCloseBatch = async (idKey: string) => {
    try {
      await closeBatch.mutateAsync(idKey);
      toast.success('Batch closed', 'The batch has been closed');
    } catch (error) {
      toast.error('Failed to close batch', 'Please try again later');
    }
  };



  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Giving - Batches</h1>
          <p className="mt-2 text-gray-600">Manage contribution batches and reconciliation</p>
        </div>
        <Link
          to="/admin/giving/new"
          className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
        >
          Create Batch
        </Link>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          {/* Status Filter */}
          <div>
            <label htmlFor="statusFilter" className="block text-sm font-medium text-gray-700 mb-1">
              Status
            </label>
            <select
              id="statusFilter"
              value={status || ''}
              onChange={(e) => {
                setStatus(e.target.value || undefined);
                setCurrentPage(1);
              }}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            >
              <option value="">All</option>
              <option value="Open">Open</option>
              <option value="Closed">Closed</option>
              <option value="Posted">Posted</option>
            </select>
          </div>

          {/* Start Date Filter */}
          <div>
            <label htmlFor="startDate" className="block text-sm font-medium text-gray-700 mb-1">
              Start Date
            </label>
            <input
              type="date"
              id="startDate"
              value={startDate || ''}
              onChange={(e) => {
                setStartDate(e.target.value || undefined);
                setCurrentPage(1);
              }}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>

          {/* End Date Filter */}
          <div>
            <label htmlFor="endDate" className="block text-sm font-medium text-gray-700 mb-1">
              End Date
            </label>
            <input
              type="date"
              id="endDate"
              value={endDate || ''}
              onChange={(e) => {
                setEndDate(e.target.value || undefined);
                setCurrentPage(1);
              }}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>

          {/* Campus Filter */}
          <div>
            <label htmlFor="campusFilter" className="block text-sm font-medium text-gray-700 mb-1">
              Campus
            </label>
            <select
              id="campusFilter"
              value={campusIdKey || ''}
              onChange={(e) => {
                setCampusIdKey(e.target.value || undefined);
                setCurrentPage(1);
              }}
              disabled={campusesLoading}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 disabled:opacity-50"
            >
              <option value="">All</option>
              {campuses.map((campus) => (
                <option key={campus.idKey} value={campus.idKey}>
                  {campus.name}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="bg-white rounded-lg border border-gray-200">
          <Loading text="Loading batches..." />
        </div>
      )}

      {/* Error State */}
      {error && (
        <div className="bg-white rounded-lg border border-gray-200">
          <ErrorState
            title="Failed to load batches"
            message={error instanceof Error ? error.message : 'Unknown error'}
            onRetry={() => refetch()}
          />
        </div>
      )}

      {/* Results */}
      {!isLoading && !error && (
        <div className="bg-white rounded-lg border border-gray-200">
          {batches.length === 0 ? (
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
              title={status || startDate || endDate || campusIdKey ? "No batches found" : "No batches yet"}
              description={status || startDate || endDate || campusIdKey
                ? "Try adjusting your filters"
                : "Get started by creating your first contribution batch"}
              action={!status && !startDate && !endDate && !campusIdKey ? {
                label: "Create Batch",
                onClick: () => navigate('/admin/giving/new')
              } : undefined}
            />
          ) : (
            <>
              {/* Table */}
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Name
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Date
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Status
                      </th>
                      <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Control Amount
                      </th>
                      <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {batches.map((batch) => (
                        <tr key={batch.idKey} className="hover:bg-gray-50">
                          <td className="px-6 py-4 whitespace-nowrap">
                            <Link
                              to={`/admin/giving/${batch.idKey}`}
                              className="text-sm font-medium text-primary-600 hover:text-primary-700"
                            >
                              {batch.name}
                            </Link>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                            {new Date(batch.batchDate).toLocaleDateString()}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <BatchStatusBadge status={batch.status} />
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right">
                            {batch.controlAmount !== undefined && batch.controlAmount !== null
                              ? `${batch.controlAmount.toFixed(2)}`
                              : '-'}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                            <div className="flex items-center justify-end gap-2">
                              {batch.status === 'Closed' && (
                                <button
                                  onClick={() => handleOpenBatch(batch.idKey)}
                                  disabled={openBatch.isPending}
                                  className="text-primary-600 hover:text-primary-700 disabled:opacity-50"
                                >
                                  Open
                                </button>
                              )}
                              {batch.status === 'Open' && (
                                <button
                                  onClick={() => handleCloseBatch(batch.idKey)}
                                  disabled={closeBatch.isPending}
                                  className="text-primary-600 hover:text-primary-700 disabled:opacity-50"
                                >
                                  Close
                                </button>
                              )}
                              <Link
                                to={`/admin/giving/${batch.idKey}`}
                                className="text-gray-600 hover:text-gray-700"
                              >
                                View
                              </Link>
                            </div>
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
                      onChange={(e) => handlePageSizeChange(Number(e.target.value))}
                      className="px-2 py-1 border border-gray-300 rounded focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                    >
                      <option value={25}>25</option>
                      <option value={50}>50</option>
                      <option value={100}>100</option>
                    </select>
                    <span>
                      of {meta.totalCount} total
                    </span>
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
      )}
    </div>
  );
}
