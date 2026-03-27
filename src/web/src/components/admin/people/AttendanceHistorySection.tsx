import { useState } from 'react';
import { usePersonAttendance } from '@/hooks/usePeople';
import { Skeleton } from '@/components/ui/Skeleton';
import { EmptyState } from '@/components/ui/EmptyState';
import { Button } from '@/components/ui/Button';
import type { AttendanceSummaryDto } from '@/services/api/types';

interface AttendanceHistorySectionProps {
  personIdKey: string;
}

const DATE_RANGE_OPTIONS = [
  { label: 'Last 30 days', value: 30 },
  { label: 'Last 90 days', value: 90 },
  { label: 'Last 180 days', value: 180 },
  { label: 'Last 365 days', value: 365 },
] as const;

const PAGE_SIZE = 10;

function formatTime(isoString: string): string {
  return new Date(isoString).toLocaleTimeString(undefined, {
    hour: 'numeric',
    minute: '2-digit',
  });
}

function AttendanceTableSkeleton() {
  return (
    <div className="space-y-2" role="status" aria-label="Loading attendance history">
      {Array.from({ length: 5 }).map((_, i) => (
        <div key={i} className="flex gap-4 py-3 border-b border-gray-100">
          <Skeleton variant="text" height={16} width="20%" />
          <Skeleton variant="text" height={16} width="30%" />
          <Skeleton variant="text" height={16} width="15%" />
          <Skeleton variant="text" height={16} width="15%" />
          <Skeleton variant="text" height={16} width="10%" />
        </div>
      ))}
    </div>
  );
}

interface AttendanceTableProps {
  records: AttendanceSummaryDto[];
}

function AttendanceTable({ records }: AttendanceTableProps) {
  const [currentPage, setCurrentPage] = useState(1);

  const totalPages = Math.ceil(records.length / PAGE_SIZE);
  const pageStart = (currentPage - 1) * PAGE_SIZE;
  const pageEnd = pageStart + PAGE_SIZE;
  const pageRecords = records.slice(pageStart, pageEnd);

  return (
    <div>
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-gray-200 text-left">
              <th className="pb-2 pr-4 font-medium text-gray-600">Date</th>
              <th className="pb-2 pr-4 font-medium text-gray-600">Location</th>
              <th className="pb-2 pr-4 font-medium text-gray-600">Check-in</th>
              <th className="pb-2 pr-4 font-medium text-gray-600">Check-out</th>
              <th className="pb-2 font-medium text-gray-600">Security Code</th>
            </tr>
          </thead>
          <tbody>
            {pageRecords.map((record) => (
              <tr key={record.idKey} className="border-b border-gray-100 hover:bg-gray-50">
                <td className="py-3 pr-4 text-gray-900">
                  <div className="flex items-center gap-2">
                    {new Date(record.startDateTime).toLocaleDateString(undefined, {
                      month: 'short',
                      day: 'numeric',
                      year: 'numeric',
                    })}
                    {record.isFirstTime && (
                      <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800">
                        First Time
                      </span>
                    )}
                  </div>
                </td>
                <td className="py-3 pr-4 text-gray-900">
                  <div className="font-medium">{record.location.name}</div>
                  <div className="text-xs text-gray-500">{record.location.fullPath}</div>
                </td>
                <td className="py-3 pr-4 text-gray-700">
                  {formatTime(record.startDateTime)}
                </td>
                <td className="py-3 pr-4 text-gray-700">
                  {record.endDateTime ? formatTime(record.endDateTime) : (
                    <span className="text-gray-400">—</span>
                  )}
                </td>
                <td className="py-3 text-gray-700">
                  {record.securityCode ?? <span className="text-gray-400">—</span>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-between mt-4 pt-2 border-t border-gray-100">
          <span className="text-sm text-gray-500">
            Showing {pageStart + 1}–{Math.min(pageEnd, records.length)} of {records.length}
          </span>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setCurrentPage((p) => p - 1)}
              disabled={currentPage === 1}
            >
              Previous
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setCurrentPage((p) => p + 1)}
              disabled={currentPage === totalPages}
            >
              Next
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}

export function AttendanceHistorySection({ personIdKey }: AttendanceHistorySectionProps) {
  const [days, setDays] = useState(90);
  const { data: records, isLoading, isError } = usePersonAttendance(personIdKey, days);

  return (
    <section className="bg-white rounded-lg border border-gray-200 p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">Attendance History</h2>
        <select
          value={days}
          onChange={(e) => setDays(Number(e.target.value))}
          className="text-sm border border-gray-300 rounded-md px-2 py-1 text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
          aria-label="Date range"
        >
          {DATE_RANGE_OPTIONS.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
      </div>

      {isLoading && <AttendanceTableSkeleton />}

      {isError && (
        <EmptyState
          title="Failed to load attendance history"
          description="There was a problem loading this person's attendance records. Please try again."
        />
      )}

      {!isLoading && !isError && records !== undefined && records.length === 0 && (
        <EmptyState
          title="No attendance records"
          description={`No check-ins found in the last ${days} days.`}
        />
      )}

      {!isLoading && !isError && records && records.length > 0 && (
        <AttendanceTable records={records} />
      )}
    </section>
  );
}
