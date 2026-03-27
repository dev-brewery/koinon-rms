/**
 * Attendance History Section
 * Displays a person's check-in history with date range filtering and pagination.
 */

import { useState } from 'react';
import { usePersonAttendance } from '@/hooks/usePeople';
import { Skeleton } from '@/components/ui/Skeleton';
import { EmptyState } from '@/components/ui/EmptyState';
import { Button } from '@/components/ui/Button';

const PAGE_SIZE = 10;

const DATE_RANGE_OPTIONS = [
  { label: 'Last 30 days', days: 30 },
  { label: 'Last 90 days', days: 90 },
  { label: 'Last 6 months', days: 180 },
  { label: 'Last year', days: 365 },
] as const;

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}

function formatTime(iso: string): string {
  return new Date(iso).toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}

interface AttendanceHistorySectionProps {
  personIdKey: string;
}

export function AttendanceHistorySection({ personIdKey }: AttendanceHistorySectionProps) {
  const [days, setDays] = useState<number>(90);
  const [page, setPage] = useState(1);

  const { data: records, isLoading } = usePersonAttendance(personIdKey, days);

  // Reset to page 1 when the date range changes
  function handleDaysChange(newDays: number) {
    setDays(newDays);
    setPage(1);
  }

  const totalRecords = records?.length ?? 0;
  const totalPages = Math.ceil(totalRecords / PAGE_SIZE);
  const pagedRecords = records?.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE) ?? [];

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      {/* Section header */}
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">Attendance History</h2>

        <select
          value={days}
          onChange={(e) => handleDaysChange(Number(e.target.value))}
          className="text-sm border border-gray-300 rounded-md px-3 py-1.5 text-gray-700 bg-white focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          aria-label="Date range filter"
        >
          {DATE_RANGE_OPTIONS.map((opt) => (
            <option key={opt.days} value={opt.days}>
              {opt.label}
            </option>
          ))}
        </select>
      </div>

      {/* Loading skeleton */}
      {isLoading && (
        <div className="space-y-3" role="status" aria-label="Loading attendance history">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="flex gap-4">
              <Skeleton variant="text" height={16} width="20%" />
              <Skeleton variant="text" height={16} width="30%" />
              <Skeleton variant="text" height={16} width="15%" />
              <Skeleton variant="text" height={16} width="15%" />
              <Skeleton variant="text" height={16} width="10%" />
            </div>
          ))}
        </div>
      )}

      {/* Empty state */}
      {!isLoading && totalRecords === 0 && (
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
                d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
              />
            </svg>
          }
          title="No attendance records"
          description={`No check-ins found for the selected time period.`}
          className="py-8"
        />
      )}

      {/* Table */}
      {!isLoading && totalRecords > 0 && (
        <>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200">
                  <th className="text-left py-2 pr-4 font-medium text-gray-700">Date</th>
                  <th className="text-left py-2 pr-4 font-medium text-gray-700">Location</th>
                  <th className="text-left py-2 pr-4 font-medium text-gray-700">Check-in</th>
                  <th className="text-left py-2 pr-4 font-medium text-gray-700">Check-out</th>
                  <th className="text-left py-2 font-medium text-gray-700">Security Code</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {pagedRecords.map((record) => (
                  <tr key={record.idKey} className="hover:bg-gray-50">
                    <td className="py-3 pr-4 text-gray-900 whitespace-nowrap">
                      <div className="flex items-center gap-2">
                        {formatDate(record.startDateTime)}
                        {record.isFirstTime && (
                          <span className="inline-flex items-center px-1.5 py-0.5 text-xs font-medium rounded-full bg-green-100 text-green-800">
                            First Time
                          </span>
                        )}
                      </div>
                    </td>
                    <td className="py-3 pr-4 text-gray-900">
                      <span title={record.location.fullPath}>{record.location.name}</span>
                    </td>
                    <td className="py-3 pr-4 text-gray-600 whitespace-nowrap">
                      {formatTime(record.startDateTime)}
                    </td>
                    <td className="py-3 pr-4 text-gray-600 whitespace-nowrap">
                      {record.endDateTime ? formatTime(record.endDateTime) : (
                        <span className="text-gray-400">—</span>
                      )}
                    </td>
                    <td className="py-3 text-gray-600 font-mono">
                      {record.securityCode ?? <span className="text-gray-400">—</span>}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between mt-4 pt-4 border-t border-gray-200">
              <p className="text-sm text-gray-500">
                Showing {(page - 1) * PAGE_SIZE + 1}–{Math.min(page * PAGE_SIZE, totalRecords)} of{' '}
                {totalRecords} records
              </p>
              <div className="flex gap-2">
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setPage((p) => p - 1)}
                  disabled={page === 1}
                >
                  Previous
                </Button>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setPage((p) => p + 1)}
                  disabled={page === totalPages}
                >
                  Next
                </Button>
              </div>
            </div>
          )}
        </>
      )}

    </div>
  );
}
