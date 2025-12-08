/**
 * Analytics Summary Cards Component
 * Displays key attendance metrics in card format
 */

import type { AttendanceAnalytics } from '@/services/api/analytics';

export interface AnalyticsSummaryCardsProps {
  analytics: AttendanceAnalytics | undefined;
  isLoading: boolean;
}

export function AnalyticsSummaryCards({ analytics, isLoading }: AnalyticsSummaryCardsProps) {
  const formatNumber = (value: number | undefined): string => {
    if (isLoading || value === undefined) return '--';
    return value.toLocaleString();
  };

  const formatDecimal = (value: number | undefined): string => {
    if (isLoading || value === undefined) return '--';
    return value.toFixed(1);
  };

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
      {/* Total Attendance */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="flex items-center justify-between">
          <div className="flex-1">
            <p className="text-sm font-medium text-gray-600">Total Attendance</p>
            <p className="mt-2 text-3xl font-bold text-gray-900">
              {formatNumber(analytics?.totalAttendance)}
            </p>
          </div>
          <div className="p-3 rounded-lg bg-blue-50 text-blue-600">
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
                d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
          </div>
        </div>
      </div>

      {/* Unique Attendees */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="flex items-center justify-between">
          <div className="flex-1">
            <p className="text-sm font-medium text-gray-600">Unique Attendees</p>
            <p className="mt-2 text-3xl font-bold text-gray-900">
              {formatNumber(analytics?.uniqueAttendees)}
            </p>
          </div>
          <div className="p-3 rounded-lg bg-green-50 text-green-600">
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
                d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"
              />
            </svg>
          </div>
        </div>
      </div>

      {/* First-Time Visitors */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="flex items-center justify-between">
          <div className="flex-1">
            <p className="text-sm font-medium text-gray-600">First-Time Visitors</p>
            <p className="mt-2 text-3xl font-bold text-gray-900">
              {formatNumber(analytics?.firstTimeVisitors)}
            </p>
          </div>
          <div className="p-3 rounded-lg bg-purple-50 text-purple-600">
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
                d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z"
              />
            </svg>
          </div>
        </div>
      </div>

      {/* Returning Visitors */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="flex items-center justify-between">
          <div className="flex-1">
            <p className="text-sm font-medium text-gray-600">Returning Visitors</p>
            <p className="mt-2 text-3xl font-bold text-gray-900">
              {formatNumber(analytics?.returningVisitors)}
            </p>
          </div>
          <div className="p-3 rounded-lg bg-indigo-50 text-indigo-600">
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
                d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
              />
            </svg>
          </div>
        </div>
      </div>

      {/* Average Attendance */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="flex items-center justify-between">
          <div className="flex-1">
            <p className="text-sm font-medium text-gray-600">Average Attendance</p>
            <p className="mt-2 text-3xl font-bold text-gray-900">
              {formatDecimal(analytics?.averageAttendance)}
            </p>
          </div>
          <div className="p-3 rounded-lg bg-amber-50 text-amber-600">
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
                d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
              />
            </svg>
          </div>
        </div>
      </div>
    </div>
  );
}
