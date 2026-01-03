/**
 * Audit Log Table Component
 * Displays audit log entries in a sortable table format
 */

import { useState } from 'react';
import { Link } from 'react-router-dom';
import { cn } from '@/lib/utils';
import { formatDateTime } from '@/lib/utils';
import { EmptyState } from '@/components/ui/EmptyState';
import { Loading } from '@/components/ui/Loading';
import type { AuditLogDto, AuditAction } from '@/services/api/types';

// ============================================================================
// Types
// ============================================================================

export interface AuditLogTableProps {
  data: AuditLogDto[];
  loading?: boolean;
  onViewDetails: (auditLog: AuditLogDto) => void;
}

type SortDirection = 'asc' | 'desc';

// ============================================================================
// Helper Functions
// ============================================================================

function getActionBadgeColor(action: AuditAction): string {
  switch (action) {
    case 'Create':
      return 'bg-green-100 text-green-800';
    case 'Update':
      return 'bg-blue-100 text-blue-800';
    case 'Delete':
      return 'bg-red-100 text-red-800';
    case 'View':
      return 'bg-gray-100 text-gray-800';
    case 'Export':
      return 'bg-purple-100 text-purple-800';
    case 'Login':
      return 'bg-indigo-100 text-indigo-800';
    case 'Logout':
      return 'bg-indigo-100 text-indigo-800';
    case 'Search':
      return 'bg-yellow-100 text-yellow-800';
    case 'Other':
      return 'bg-gray-100 text-gray-800';
    default:
      return 'bg-gray-100 text-gray-800';
  }
}

// ============================================================================
// Component
// ============================================================================

export function AuditLogTable({ data, loading = false, onViewDetails }: AuditLogTableProps) {
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');

  const handleSort = () => {
    setSortDirection((current) => (current === 'asc' ? 'desc' : 'asc'));
  };

  const sortedData = [...data].sort((a, b) => {
    const aValue = new Date(a.timestamp).getTime();
    const bValue = new Date(b.timestamp).getTime();

    if (sortDirection === 'asc') {
      return aValue - bValue;
    } else {
      return bValue - aValue;
    }
  });

  // Loading state
  if (loading) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-12">
        <Loading />
      </div>
    );
  }

  // Empty state
  if (data.length === 0) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-12">
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
                d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"
              />
            </svg>
          }
          title="No audit logs found"
          description="Try adjusting your filters to see more results"
        />
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100 transition-colors"
                onClick={handleSort}
              >
                <div className="flex items-center gap-2">
                  <span>Timestamp</span>
                  <svg
                    className={cn(
                      'w-4 h-4 transition-transform',
                      sortDirection === 'desc' && 'rotate-180'
                    )}
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M5 15l7-7 7 7"
                    />
                  </svg>
                </div>
              </th>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
              >
                Action
              </th>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
              >
                Entity Type
              </th>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
              >
                Entity ID
              </th>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
              >
                User
              </th>
              <th
                scope="col"
                className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
              >
                Changes
              </th>
              <th scope="col" className="relative px-6 py-3">
                <span className="sr-only">Actions</span>
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {sortedData.map((log) => (
              <tr
                key={log.idKey}
                className="hover:bg-gray-50 cursor-pointer transition-colors"
                onClick={() => onViewDetails(log)}
              >
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {formatDateTime(log.timestamp)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span
                    className={cn(
                      'inline-flex px-2 py-1 text-xs font-semibold rounded-full',
                      getActionBadgeColor(log.actionType)
                    )}
                  >
                    {log.actionType}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {log.entityType}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-600">
                  {log.entityIdKey}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm">
                  {log.personIdKey ? (
                    <Link
                      to={`/admin/people/${log.personIdKey}`}
                      className="text-blue-600 hover:text-blue-700 transition-colors"
                      onClick={(e) => e.stopPropagation()}
                    >
                      {log.personName || 'Unknown'}
                    </Link>
                  ) : (
                    <span className="text-gray-500 italic">System</span>
                  )}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {log.changedProperties && log.changedProperties.length > 0 ? (
                    <span className="inline-flex items-center gap-1">
                      <span className="font-semibold">{log.changedProperties.length}</span>
                      <span className="text-gray-500">
                        {log.changedProperties.length === 1 ? 'property' : 'properties'}
                      </span>
                    </span>
                  ) : (
                    <span className="text-gray-500">-</span>
                  )}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      onViewDetails(log);
                    }}
                    className="text-blue-600 hover:text-blue-700 transition-colors"
                  >
                    View Details
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
