/**
 * Attendance Analytics Page
 * Dashboard for viewing attendance metrics, trends, and group breakdowns
 */

import { useState } from 'react';
import {
  DateRangePicker,
  AnalyticsSummaryCards,
  AttendanceTrendChart,
  AttendanceByGroupTable,
} from '@/components/admin/analytics';
import type { DateRange } from '@/components/admin/analytics';
import {
  useAttendanceAnalytics,
  useAttendanceTrends,
  useAttendanceByGroup,
} from '@/hooks/useAnalytics';

export function AnalyticsPage() {
  // Calculate default date range (last 30 days)
  const getDefaultDateRange = (): DateRange => {
    const today = new Date();
    const endDate = today.toISOString().split('T')[0];
    const startDate = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000)
      .toISOString()
      .split('T')[0];
    return { startDate, endDate };
  };

  const [dateRange, setDateRange] = useState<DateRange>(getDefaultDateRange());
  const [campusFilter, setCampusFilter] = useState<string>('');
  const [groupTypeFilter, setGroupTypeFilter] = useState<string>('');

  // Build params for API queries
  const queryParams = {
    startDate: dateRange.startDate,
    endDate: dateRange.endDate,
    ...(campusFilter && { campusIdKey: campusFilter }),
    ...(groupTypeFilter && { groupTypeIdKey: groupTypeFilter }),
  };

  // Fetch analytics data
  const {
    data: analytics,
    isLoading: isLoadingAnalytics,
    error: analyticsError,
  } = useAttendanceAnalytics(queryParams);

  const {
    data: trends,
    isLoading: isLoadingTrends,
    error: trendsError,
  } = useAttendanceTrends(queryParams);

  const {
    data: groupData,
    isLoading: isLoadingGroups,
    error: groupsError,
  } = useAttendanceByGroup(queryParams);

  // Combined error state
  const error = analyticsError || trendsError || groupsError;

  return (
    <div className="space-y-6">
      {/* Page header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Attendance Analytics</h1>
        <p className="mt-2 text-gray-600">
          View attendance metrics, trends, and group breakdowns
        </p>
      </div>

      {/* Error state */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-6">
          <div className="flex items-start gap-3">
            <svg
              className="w-6 h-6 text-red-400 flex-shrink-0 mt-0.5"
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
            <div>
              <p className="text-red-700 font-medium">Failed to load analytics data</p>
              <p className="text-red-600 text-sm mt-1">
                {error instanceof Error ? error.message : 'An unknown error occurred'}
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Date Range Picker */}
      <DateRangePicker value={dateRange} onChange={setDateRange} />

      {/* Filters */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <h3 className="text-sm font-medium text-gray-700 mb-3">Filters</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label
              htmlFor="campus-filter"
              className="block text-sm font-medium text-gray-700 mb-1"
            >
              Campus (Optional)
            </label>
            <select
              id="campus-filter"
              value={campusFilter}
              onChange={(e) => setCampusFilter(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">All Campuses</option>
              {/* Add campus options here - could be fetched from API */}
            </select>
          </div>
          <div>
            <label
              htmlFor="group-type-filter"
              className="block text-sm font-medium text-gray-700 mb-1"
            >
              Group Type (Optional)
            </label>
            <select
              id="group-type-filter"
              value={groupTypeFilter}
              onChange={(e) => setGroupTypeFilter(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">All Group Types</option>
              {/* Add group type options here - could be fetched from API */}
            </select>
          </div>
        </div>
      </div>

      {/* Summary Cards */}
      <AnalyticsSummaryCards analytics={analytics} isLoading={isLoadingAnalytics} />

      {/* Trend Chart */}
      <AttendanceTrendChart trends={trends} isLoading={isLoadingTrends} />

      {/* Group Table */}
      <AttendanceByGroupTable groups={groupData} isLoading={isLoadingGroups} />
    </div>
  );
}
