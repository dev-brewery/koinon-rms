/**
 * Audit Logs Page
 * Admin page for viewing and exporting audit logs
 */

import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuditLogs, useExportAuditLogs } from '@/hooks/useAuditLogs';
import {
  AuditLogFilters,
  AuditLogTable,
  AuditLogDetailModal,
} from '@/components/admin/audit';
import type { AuditLogSearchParams, AuditLogDto } from '@/services/api/types';
import { ExportFormat } from '@/services/api/types';

export function AuditLogsPage() {
  // State
  const [filters, setFilters] = useState<AuditLogSearchParams>({
    page: 1,
    pageSize: 20,
  });
  const [selectedLog, setSelectedLog] = useState<AuditLogDto | null>(null);

  // Queries
  const { data, isLoading, error } = useAuditLogs(filters);
  const exportMutation = useExportAuditLogs();

  // Handlers
  const handleFiltersChange = (newFilters: AuditLogSearchParams) => {
    setFilters({
      ...newFilters,
      page: 1, // Reset to first page when filters change
    });
  };

  const handleViewDetails = (auditLog: AuditLogDto) => {
    setSelectedLog(auditLog);
  };

  const handleCloseDetailModal = () => {
    setSelectedLog(null);
  };

  const handleExport = async () => {
    try {
      await exportMutation.mutateAsync({
        startDate: filters.startDate,
        endDate: filters.endDate,
        entityType: filters.entityType,
        actionType: filters.actionType,
        personIdKey: filters.personIdKey,
        format: ExportFormat.Csv,
      });
    } catch (err) {
      // Error is handled by mutation's onError
      console.error('Export failed:', err);
    }
  };

  const handlePageChange = (newPage: number) => {
    setFilters((prev) => ({
      ...prev,
      page: newPage,
    }));
  };

  const auditLogs = data?.data || [];
  const totalPages = data?.meta?.totalPages || 1;
  const currentPage = data?.meta?.page || 1;
  const totalCount = data?.meta?.totalCount || 0;

  return (
    <div className="space-y-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-gray-500">
        <Link to="/admin/settings" className="hover:text-gray-700">
          Settings
        </Link>
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
            d="M9 5l7 7-7 7"
          />
        </svg>
        <span className="text-gray-900">Audit Logs</span>
      </div>

      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Audit Logs</h1>
          <p className="mt-2 text-gray-600">
            View system activity and track changes to records
          </p>
        </div>
        <button
          onClick={handleExport}
          disabled={exportMutation.isPending || auditLogs.length === 0}
          className="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {exportMutation.isPending ? (
            <>
              <div className="inline-block w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
              Exporting...
            </>
          ) : (
            <>
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
                  d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                />
              </svg>
              Export CSV
            </>
          )}
        </button>
      </div>

      {/* Error State */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex items-center gap-2">
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
            <p className="text-sm font-medium text-red-800">Failed to load audit logs</p>
          </div>
        </div>
      )}

      {/* Export Success Message */}
      {exportMutation.isSuccess && (
        <div className="bg-green-50 border border-green-200 rounded-lg p-4">
          <div className="flex items-center gap-2">
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
            <p className="text-sm font-medium text-green-800">
              Audit logs exported successfully
            </p>
          </div>
        </div>
      )}

      {/* Export Error Message */}
      {exportMutation.isError && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex items-center gap-2">
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
            <p className="text-sm font-medium text-red-800">Failed to export audit logs</p>
          </div>
        </div>
      )}

      {/* Main Content Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Filters Sidebar */}
        <div className="lg:col-span-1">
          <AuditLogFilters filters={filters} onFiltersChange={handleFiltersChange} />
        </div>

        {/* Table Section */}
        <div className="lg:col-span-3 space-y-4">
          {/* Results Summary */}
          {!isLoading && !error && (
            <div className="text-sm text-gray-600">
              Showing {auditLogs.length > 0 ? ((currentPage - 1) * (filters.pageSize || 20)) + 1 : 0}
              {' - '}
              {Math.min(currentPage * (filters.pageSize || 20), totalCount)} of {totalCount} logs
            </div>
          )}

          {/* Table */}
          <AuditLogTable
            data={auditLogs}
            loading={isLoading}
            onViewDetails={handleViewDetails}
          />

          {/* Pagination */}
          {!isLoading && !error && totalPages > 1 && (
            <div className="bg-white rounded-lg border border-gray-200 p-4">
              <div className="flex items-center justify-between">
                <button
                  onClick={() => handlePageChange(currentPage - 1)}
                  disabled={currentPage <= 1}
                  className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
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
                      d="M15 19l-7-7 7-7"
                    />
                  </svg>
                  Previous
                </button>

                <span className="text-sm text-gray-700">
                  Page {currentPage} of {totalPages}
                </span>

                <button
                  onClick={() => handlePageChange(currentPage + 1)}
                  disabled={currentPage >= totalPages}
                  className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Next
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
                      d="M9 5l7 7-7 7"
                    />
                  </svg>
                </button>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Detail Modal */}
      <AuditLogDetailModal
        isOpen={selectedLog !== null}
        onClose={handleCloseDetailModal}
        auditLog={selectedLog}
      />
    </div>
  );
}
