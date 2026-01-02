/**
 * Communication Templates Page
 * List and manage communication templates
 */

import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  useCommunicationTemplates,
  useDeleteCommunicationTemplate,
} from '@/hooks/useCommunicationTemplates';
import { Loading, EmptyState, ErrorState, ConfirmDialog } from '@/components/ui';
import { cn } from '@/lib/utils';

const TYPE_OPTIONS = [
  { value: '', label: 'All Types' },
  { value: 'Email', label: 'Email' },
  { value: 'Sms', label: 'SMS' },
];

interface CommunicationTypeBadgeProps {
  type: string;
  className?: string;
}

function CommunicationTypeBadge({ type, className }: CommunicationTypeBadgeProps) {
  const getTypeClasses = () => {
    switch (type) {
      case 'Email':
        return 'bg-blue-100 text-blue-800';
      case 'Sms':
        return 'bg-purple-100 text-purple-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  return (
    <span
      className={cn(
        'inline-flex items-center px-3 py-1 text-xs font-medium rounded-full',
        getTypeClasses(),
        className
      )}
    >
      {type}
    </span>
  );
}

interface StatusBadgeProps {
  isActive: boolean;
  className?: string;
}

function StatusBadge({ isActive, className }: StatusBadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center px-3 py-1 text-xs font-medium rounded-full',
        isActive
          ? 'bg-green-100 text-green-800'
          : 'bg-gray-100 text-gray-800',
        className
      )}
    >
      {isActive ? 'Active' : 'Inactive'}
    </span>
  );
}

export function TemplatesPage() {
  const navigate = useNavigate();
  const [typeFilter, setTypeFilter] = useState('');
  const [activeFilter, setActiveFilter] = useState<boolean | undefined>(true);
  const [pageSize, setPageSize] = useState(25);
  const [currentPage, setCurrentPage] = useState(1);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [templateToDelete, setTemplateToDelete] = useState<{
    idKey: string;
    name: string;
  } | null>(null);

  const { data, isLoading, error, refetch } = useCommunicationTemplates({
    type: typeFilter || undefined,
    isActive: activeFilter,
    page: currentPage,
    pageSize,
  });

  const deleteMutation = useDeleteCommunicationTemplate();

  const templates = data?.data || [];
  const meta = data?.meta;

  const handleTypeChange = (value: string) => {
    setTypeFilter(value);
    setCurrentPage(1);
  };

  const handleActiveFilterChange = (value: string) => {
    if (value === 'all') {
      setActiveFilter(undefined);
    } else if (value === 'active') {
      setActiveFilter(true);
    } else {
      setActiveFilter(false);
    }
    setCurrentPage(1);
  };

  const handlePageSizeChange = (newSize: number) => {
    setPageSize(newSize);
    setCurrentPage(1);
  };

  const handleDeleteClick = (idKey: string, name: string) => {
    setTemplateToDelete({ idKey, name });
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (!templateToDelete) return;

    try {
      await deleteMutation.mutateAsync(templateToDelete.idKey);
      setDeleteDialogOpen(false);
      setTemplateToDelete(null);
    } catch (error) {
      // Error is handled by mutation
      if (import.meta.env.DEV) {
        console.error('Failed to delete template:', error);
      }
    }
  };

  const handleDeleteCancel = () => {
    setDeleteDialogOpen(false);
    setTemplateToDelete(null);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Communication Templates</h1>
          <p className="mt-2 text-gray-600">
            Manage reusable templates for emails and SMS messages
          </p>
        </div>
        <Link
          to="/admin/communications/templates/new"
          className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
        >
          Create Template
        </Link>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <div className="flex flex-wrap gap-4">
          <div className="w-48">
            <label htmlFor="type-filter" className="block text-sm font-medium text-gray-700 mb-1">
              Type
            </label>
            <select
              id="type-filter"
              value={typeFilter}
              onChange={(e) => handleTypeChange(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            >
              {TYPE_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>

          <div className="w-48">
            <label htmlFor="status-filter" className="block text-sm font-medium text-gray-700 mb-1">
              Status
            </label>
            <select
              id="status-filter"
              value={
                activeFilter === undefined ? 'all' : activeFilter ? 'active' : 'inactive'
              }
              onChange={(e) => handleActiveFilterChange(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            >
              <option value="all">All</option>
              <option value="active">Active Only</option>
              <option value="inactive">Inactive Only</option>
            </select>
          </div>
        </div>
      </div>

      {/* Results */}
      <div className="bg-white rounded-lg border border-gray-200">
        {isLoading ? (
          <Loading text="Loading templates..." />
        ) : error ? (
          <ErrorState
            title="Failed to load templates"
            message={error instanceof Error ? error.message : 'Unknown error'}
            onRetry={() => refetch()}
          />
        ) : templates.length === 0 ? (
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
            title="No templates found"
            description={
              typeFilter || activeFilter !== true
                ? 'Try adjusting your filters'
                : 'Get started by creating your first template'
            }
            action={
              !typeFilter && activeFilter === true
                ? {
                    label: 'Create Template',
                    onClick: () => navigate('/admin/communications/templates/new'),
                  }
                : undefined
            }
          />
        ) : (
          <>
            {/* Table */}
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Name
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Type
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Status
                    </th>
                    <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {templates.map((template) => (
                    <tr key={template.idKey} className="hover:bg-gray-50">
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="text-sm font-medium text-gray-900">
                          {template.name}
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <CommunicationTypeBadge type={template.communicationType} />
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <StatusBadge isActive={template.isActive} />
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                        <div className="flex items-center justify-end gap-2">
                          <Link
                            to={`/admin/communications/templates/${template.idKey}/edit`}
                            className="text-primary-600 hover:text-primary-900 transition-colors"
                          >
                            Edit
                          </Link>
                          <button
                            onClick={() => handleDeleteClick(template.idKey, template.name)}
                            className="text-red-600 hover:text-red-900 transition-colors"
                          >
                            Delete
                          </button>
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
                  <span>of {meta.totalCount} total</span>
                </div>

                <div className="flex items-center gap-2">
                  <button
                    onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                    disabled={currentPage === 1}
                    aria-label="Go to previous page"
                    className="px-3 py-1 border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Previous
                  </button>
                  <span className="text-sm text-gray-700">
                    Page {currentPage} of {meta.totalPages}
                  </span>
                  <button
                    onClick={() =>
                      setCurrentPage((p) => Math.min(meta.totalPages, p + 1))
                    }
                    disabled={currentPage === meta.totalPages}
                    aria-label="Go to next page"
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

      {/* Delete Confirmation Dialog */}
      <ConfirmDialog
        isOpen={deleteDialogOpen}
        onClose={handleDeleteCancel}
        onConfirm={handleDeleteConfirm}
        title="Delete Template"
        description={templateToDelete ? `Are you sure you want to delete "${templateToDelete.name}"? This action cannot be undone.` : ''}
        confirmLabel="Delete"
        variant="danger"
        isLoading={deleteMutation.isPending}
      />
    </div>
  );
}
