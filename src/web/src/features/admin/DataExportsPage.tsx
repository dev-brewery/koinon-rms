/**
 * DataExportsPage Component
 * Allows users to create and download data exports in CSV or Excel format
 */

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { get, post } from '@/services/api/client';
import { Loading, EmptyState, ErrorState } from '@/components/ui';
import { Button } from '@/components/ui/Button';
import { useToast } from '@/contexts/ToastContext';
import type {
  ApiResponse,
  DataExportJobDto,
  CreateDataExportRequest,
  ExportFieldDto,
  DataExportType,
  DataExportFormat,
  DataExportStatus,
} from '@/services/api/types';

// ============================================================================
// API Functions
// ============================================================================

async function fetchExportJobs(): Promise<DataExportJobDto[]> {
  const response = await get<ApiResponse<DataExportJobDto[]>>('/exports');
  return response.data;
}

async function fetchExportFields(exportType: DataExportType): Promise<ExportFieldDto[]> {
  const response = await get<ApiResponse<ExportFieldDto[]>>(`/exports/fields/${exportType}`);
  return response.data;
}

async function createExport(request: CreateDataExportRequest): Promise<DataExportJobDto> {
  const response = await post<ApiResponse<DataExportJobDto>>('/exports', request);
  return response.data;
}

function getDownloadUrl(idKey: string): string {
  const baseUrl = import.meta.env.VITE_API_URL || 'http://localhost:5000/api/v1';
  return `${baseUrl}/exports/${idKey}/download`;
}

// ============================================================================
// Status Badge Component
// ============================================================================

interface StatusBadgeProps {
  status: DataExportStatus;
}

function StatusBadge({ status }: StatusBadgeProps) {
  const colors = {
    Pending: 'bg-yellow-100 text-yellow-800',
    Processing: 'bg-blue-100 text-blue-800',
    Completed: 'bg-green-100 text-green-800',
    Failed: 'bg-red-100 text-red-800',
  };

  return (
    <span className={`px-2 py-1 text-xs font-medium rounded-full ${colors[status]}`}>
      {status}
    </span>
  );
}

// ============================================================================
// New Export Modal Component
// ============================================================================

interface NewExportModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (request: CreateDataExportRequest) => Promise<void>;
}

function NewExportModal({ isOpen, onClose, onSubmit }: NewExportModalProps) {
  const [exportType, setExportType] = useState<DataExportType>('People');
  const [outputFormat, setOutputFormat] = useState<DataExportFormat>('CSV');
  const [selectedFields, setSelectedFields] = useState<string[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { data: fields, isLoading: fieldsLoading } = useQuery({
    queryKey: ['exportFields', exportType],
    queryFn: () => fetchExportFields(exportType),
    enabled: isOpen,
  });

  // Auto-select required fields
  useState(() => {
    if (fields) {
      const requiredFields = fields.filter(f => f.isRequired).map(f => f.fieldName);
      setSelectedFields(prev => {
        const newSelection = new Set([...prev, ...requiredFields]);
        return Array.from(newSelection);
      });
    }
  });

  const handleExportTypeChange = (newType: DataExportType) => {
    setExportType(newType);
    setSelectedFields([]);
  };

  const handleFieldToggle = (fieldName: string) => {
    const field = fields?.find(f => f.fieldName === fieldName);
    if (field?.isRequired) return; // Don't allow deselecting required fields

    setSelectedFields(prev =>
      prev.includes(fieldName)
        ? prev.filter(f => f !== fieldName)
        : [...prev, fieldName]
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (selectedFields.length === 0) {
      return;
    }

    setIsSubmitting(true);
    try {
      await onSubmit({
        exportType,
        outputFormat,
        selectedFields,
      });
      onClose();
      // Reset form
      setExportType('People');
      setOutputFormat('CSV');
      setSelectedFields([]);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:p-0">
        {/* Backdrop */}
        <div
          className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"
          onClick={onClose}
        />

        {/* Modal */}
        <div className="relative inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-2xl sm:w-full">
          <form onSubmit={handleSubmit}>
            <div className="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
              <div className="flex items-start justify-between mb-4">
                <div>
                  <h3 className="text-lg font-medium text-gray-900">New Export</h3>
                  <p className="text-sm text-gray-500 mt-1">
                    Configure and start a new data export
                  </p>
                </div>
                <button
                  type="button"
                  onClick={onClose}
                  disabled={isSubmitting}
                  className="text-gray-400 hover:text-gray-500 disabled:opacity-50"
                  aria-label="Close"
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
                      d="M6 18L18 6M6 6l12 12"
                    />
                  </svg>
                </button>
              </div>

              {/* Export Type */}
              <div className="mb-4">
                <label htmlFor="exportType" className="block text-sm font-medium text-gray-700 mb-1">
                  Export Type
                </label>
                <select
                  id="exportType"
                  value={exportType}
                  onChange={(e) => handleExportTypeChange(e.target.value as DataExportType)}
                  disabled={isSubmitting}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 disabled:bg-gray-100"
                >
                  <option value="People">People</option>
                  <option value="Families">Families</option>
                  <option value="Groups">Groups</option>
                  <option value="Contributions">Contributions</option>
                  <option value="Attendance">Attendance</option>
                </select>
              </div>

              {/* Output Format */}
              <div className="mb-4">
                <label htmlFor="outputFormat" className="block text-sm font-medium text-gray-700 mb-1">
                  Output Format
                </label>
                <select
                  id="outputFormat"
                  value={outputFormat}
                  onChange={(e) => setOutputFormat(e.target.value as DataExportFormat)}
                  disabled={isSubmitting}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 disabled:bg-gray-100"
                >
                  <option value="CSV">CSV</option>
                  <option value="Excel">Excel</option>
                </select>
              </div>

              {/* Field Selection */}
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Select Fields
                </label>
                {fieldsLoading ? (
                  <div className="p-4">
                    <Loading size="sm" text="Loading fields..." />
                  </div>
                ) : (
                  <div className="border border-gray-300 rounded-lg p-3 max-h-64 overflow-y-auto">
                    {fields && fields.length > 0 ? (
                      <div className="space-y-2">
                        {fields.map(field => (
                          <label
                            key={field.fieldName}
                            className="flex items-center gap-2 p-2 hover:bg-gray-50 rounded cursor-pointer"
                          >
                            <input
                              type="checkbox"
                              checked={selectedFields.includes(field.fieldName)}
                              onChange={() => handleFieldToggle(field.fieldName)}
                              disabled={field.isRequired || isSubmitting}
                              className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded disabled:opacity-50"
                            />
                            <div className="flex-1">
                              <span className="text-sm font-medium text-gray-900">
                                {field.displayName}
                                {field.isRequired && (
                                  <span className="text-red-500 ml-1">*</span>
                                )}
                              </span>
                              <span className="text-xs text-gray-500 ml-2">
                                ({field.dataType})
                              </span>
                            </div>
                          </label>
                        ))}
                      </div>
                    ) : (
                      <p className="text-sm text-gray-500 text-center py-4">
                        No fields available
                      </p>
                    )}
                  </div>
                )}
                {selectedFields.length > 0 && (
                  <p className="text-xs text-gray-500 mt-1">
                    {selectedFields.length} field{selectedFields.length !== 1 ? 's' : ''} selected
                  </p>
                )}
              </div>
            </div>

            {/* Actions */}
            <div className="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse gap-3">
              <button
                type="submit"
                disabled={isSubmitting || selectedFields.length === 0}
                className="w-full sm:w-auto px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {isSubmitting ? 'Starting Export...' : 'Start Export'}
              </button>
              <button
                type="button"
                onClick={onClose}
                disabled={isSubmitting}
                className="w-full sm:w-auto mt-3 sm:mt-0 px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 transition-colors"
              >
                Cancel
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

// ============================================================================
// Main Page Component
// ============================================================================

export function DataExportsPage() {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const queryClient = useQueryClient();
  const toast = useToast();

  const { data: exports, isLoading, error, refetch } = useQuery({
    queryKey: ['dataExports'],
    queryFn: fetchExportJobs,
    refetchInterval: (query) => {
      // Auto-refresh if any exports are pending or processing
      const hasActiveExports = query.state.data?.some((exp: DataExportJobDto) =>
        exp.status === 'Pending' || exp.status === 'Processing'
      );
      return hasActiveExports ? 5000 : false; // Refresh every 5 seconds
    },
  });

  const createMutation = useMutation({
    mutationFn: createExport,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dataExports'] });
      toast.success('Export Started', 'Your export has been queued for processing');
    },
    onError: (error: Error) => {
      toast.error('Export Failed', error.message || 'Failed to start export');
    },
  });

  const handleCreateExport = async (request: CreateDataExportRequest) => {
    await createMutation.mutateAsync(request);
  };

  const handleDownload = (idKey: string, fileName: string) => {
    const url = getDownloadUrl(idKey);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Data Exports</h1>
          <p className="mt-2 text-gray-600">Export data in CSV or Excel format</p>
        </div>
        <Button
          variant="primary"
          onClick={() => setIsModalOpen(true)}
        >
          New Export
        </Button>
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="bg-white rounded-lg border border-gray-200">
          <Loading text="Loading exports..." />
        </div>
      )}

      {/* Error State */}
      {error && (
        <div className="bg-white rounded-lg border border-gray-200">
          <ErrorState
            title="Failed to load exports"
            message={error instanceof Error ? error.message : 'Unknown error'}
            onRetry={() => refetch()}
          />
        </div>
      )}

      {/* Results */}
      {!isLoading && !error && (
        <div className="bg-white rounded-lg border border-gray-200">
          {!exports || exports.length === 0 ? (
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
                    d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                  />
                </svg>
              }
              title="No exports yet"
              description="Get started by creating your first data export"
              action={{
                label: "New Export",
                onClick: () => setIsModalOpen(true)
              }}
            />
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Name / Type
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Status
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Created Date
                    </th>
                    <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Records
                    </th>
                    <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {exports.map((exportJob) => (
                    <tr key={exportJob.idKey} className="hover:bg-gray-50">
                      <td className="px-6 py-4">
                        <div>
                          <div className="text-sm font-medium text-gray-900">
                            {exportJob.exportType}
                          </div>
                          <div className="text-sm text-gray-500">
                            {exportJob.outputFormat}
                          </div>
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <StatusBadge status={exportJob.status} />
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        {new Date(exportJob.createdDateTime).toLocaleString()}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right">
                        {exportJob.recordCount !== undefined ? exportJob.recordCount.toLocaleString() : '-'}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                        {exportJob.status === 'Completed' && exportJob.fileName ? (
                          <button
                            onClick={() => handleDownload(exportJob.idKey, exportJob.fileName!)}
                            className="text-primary-600 hover:text-primary-700"
                          >
                            Download
                          </button>
                        ) : exportJob.status === 'Failed' ? (
                          <span className="text-red-600" title={exportJob.errorMessage}>
                            Failed
                          </span>
                        ) : (
                          <span className="text-gray-400">
                            {exportJob.status}
                          </span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* New Export Modal */}
      <NewExportModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSubmit={handleCreateExport}
      />
    </div>
  );
}
