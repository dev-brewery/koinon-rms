/**
 * Pickup History Panel Component
 * Shows historical pickup records for a child
 */

import { useState } from 'react';
import { usePickupHistory } from './hooks';
import type { IdKey, DateTime } from '@/services/api/types';

export interface PickupHistoryPanelProps {
  childIdKey: IdKey;
  childName: string;
}

export function PickupHistoryPanel({
  childIdKey,
  childName,
}: PickupHistoryPanelProps) {
  const [fromDate, setFromDate] = useState<DateTime | undefined>(
    // Default to 30 days ago
    new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString()
  );
  const [toDate, setToDate] = useState<DateTime | undefined>(
    new Date().toISOString()
  );

  const { data: history, isLoading, error } = usePickupHistory(
    childIdKey,
    fromDate,
    toDate
  );

  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    }).format(date);
  };

  const handleDateRangeChange = (days: number) => {
    const to = new Date();
    const from = new Date(Date.now() - days * 24 * 60 * 60 * 1000);
    setFromDate(from.toISOString());
    setToDate(to.toISOString());
  };

  if (error) {
    return (
      <div className="rounded-md bg-red-50 p-4">
        <div className="flex">
          <div className="ml-3">
            <h3 className="text-sm font-medium text-red-800">
              Error loading pickup history
            </h3>
            <p className="mt-2 text-sm text-red-700">
              {error instanceof Error ? error.message : 'Unknown error occurred'}
            </p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-medium text-gray-900">
          Pickup History for {childName}
        </h3>
      </div>

      {/* Date range filter */}
      <div className="flex items-center gap-2">
        <label className="text-sm font-medium text-gray-700">Show:</label>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => handleDateRangeChange(7)}
            className="inline-flex items-center px-3 py-1.5 border border-gray-300 shadow-sm text-xs font-medium rounded text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            Last 7 days
          </button>
          <button
            type="button"
            onClick={() => handleDateRangeChange(30)}
            className="inline-flex items-center px-3 py-1.5 border border-gray-300 shadow-sm text-xs font-medium rounded text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            Last 30 days
          </button>
          <button
            type="button"
            onClick={() => handleDateRangeChange(90)}
            className="inline-flex items-center px-3 py-1.5 border border-gray-300 shadow-sm text-xs font-medium rounded text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            Last 90 days
          </button>
        </div>
      </div>

      {/* Loading state */}
      {isLoading && (
        <div className="text-center py-8">
          <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900"></div>
          <p className="mt-2 text-sm text-gray-500">Loading history...</p>
        </div>
      )}

      {/* Empty state */}
      {!isLoading && history && history.length === 0 && (
        <div className="text-center py-8 bg-gray-50 rounded-lg">
          <svg
            className="mx-auto h-12 w-12 text-gray-400"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
            />
          </svg>
          <h3 className="mt-2 text-sm font-medium text-gray-900">
            No pickup history
          </h3>
          <p className="mt-1 text-sm text-gray-500">
            No pickups found in the selected date range.
          </p>
        </div>
      )}

      {/* History table */}
      {!isLoading && history && history.length > 0 && (
        <div className="bg-white shadow overflow-hidden sm:rounded-md">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th
                    scope="col"
                    className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                  >
                    Date & Time
                  </th>
                  <th
                    scope="col"
                    className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                  >
                    Picked Up By
                  </th>
                  <th
                    scope="col"
                    className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                  >
                    Status
                  </th>
                  <th
                    scope="col"
                    className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                  >
                    Notes
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {history.map((log) => (
                  <tr key={log.idKey} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {formatDate(log.checkoutDateTime)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {log.pickupPersonName}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center gap-2">
                        {log.wasAuthorized ? (
                          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                            <svg
                              className="mr-1.5 h-3 w-3 text-green-500"
                              fill="currentColor"
                              viewBox="0 0 8 8"
                            >
                              <circle cx={4} cy={4} r={3} />
                            </svg>
                            Authorized
                          </span>
                        ) : (
                          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800">
                            <svg
                              className="mr-1.5 h-3 w-3 text-red-500"
                              fill="currentColor"
                              viewBox="0 0 8 8"
                            >
                              <circle cx={4} cy={4} r={3} />
                            </svg>
                            Not Authorized
                          </span>
                        )}
                        {log.supervisorOverride && (
                          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
                            Supervisor Override
                          </span>
                        )}
                      </div>
                      {log.supervisorOverride && log.supervisorName && (
                        <div className="mt-1 text-xs text-gray-500">
                          By: {log.supervisorName}
                        </div>
                      )}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-500">
                      {log.notes || '-'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Summary */}
          <div className="bg-gray-50 px-6 py-3 border-t border-gray-200">
            <p className="text-sm text-gray-700">
              Showing{' '}
              <span className="font-medium">{history.length}</span>{' '}
              {history.length === 1 ? 'pickup' : 'pickups'}
            </p>
          </div>
        </div>
      )}
    </div>
  );
}
