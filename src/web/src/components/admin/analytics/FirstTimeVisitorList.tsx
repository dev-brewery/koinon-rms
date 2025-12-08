/**
 * First-Time Visitor List Component
 * Full page/modal component displaying all first-time visitors with filtering and sorting
 */

import { useState } from 'react';
import { useFirstTimeVisitorsByDateRange } from '@/hooks/useAnalytics';
import { DateRangePicker } from './DateRangePicker';
import type { DateRange } from './DateRangePicker';

export interface FirstTimeVisitorListProps {
  defaultDateRange?: DateRange;
  campusFilter?: string;
  onCampusFilterChange?: (campusIdKey: string) => void;
}

type SortField = 'personName' | 'checkInDateTime' | 'groupName' | 'campusName';
type SortDirection = 'asc' | 'desc';

export function FirstTimeVisitorList({
  defaultDateRange,
  campusFilter = '',
  onCampusFilterChange,
}: FirstTimeVisitorListProps) {
  // Calculate default date range (last 7 days)
  const getDefaultDateRange = (): DateRange => {
    const today = new Date();
    const endDate = today.toISOString().split('T')[0];
    const startDate = new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000)
      .toISOString()
      .split('T')[0];
    return { startDate, endDate };
  };

  const [dateRange, setDateRange] = useState<DateRange>(
    defaultDateRange || getDefaultDateRange()
  );
  const [sortField, setSortField] = useState<SortField>('checkInDateTime');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');

  const {
    data: visitors,
    isLoading,
    error,
  } = useFirstTimeVisitorsByDateRange(
    dateRange.startDate,
    dateRange.endDate,
    campusFilter || undefined
  );

  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortDirection(field === 'checkInDateTime' ? 'desc' : 'asc');
    }
  };

  const sortedVisitors = visitors
    ? [...visitors].sort((a, b) => {
        let aValue: string;
        let bValue: string;

        switch (sortField) {
          case 'personName':
            aValue = a.personName;
            bValue = b.personName;
            break;
          case 'checkInDateTime':
            aValue = a.checkInDateTime;
            bValue = b.checkInDateTime;
            break;
          case 'groupName':
            aValue = a.groupName;
            bValue = b.groupName;
            break;
          case 'campusName':
            aValue = a.campusName || '';
            bValue = b.campusName || '';
            break;
          default:
            return 0;
        }

        const comparison = aValue.localeCompare(bValue);
        return sortDirection === 'asc' ? comparison : -comparison;
      })
    : [];

  const formatDateTime = (dateTime: string): string => {
    const date = new Date(dateTime);
    return date.toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  };

  const SortIcon = ({ field }: { field: SortField }) => {
    if (sortField !== field) {
      return (
        <svg
          className="w-4 h-4 text-gray-400"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M7 16V4m0 0L3 8m4-4l4 4m6 0v12m0 0l4-4m-4 4l-4-4"
          />
        </svg>
      );
    }

    return sortDirection === 'asc' ? (
      <svg
        className="w-4 h-4 text-blue-600"
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
    ) : (
      <svg
        className="w-4 h-4 text-blue-600"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
        aria-hidden="true"
      >
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth={2}
          d="M19 9l-7 7-7-7"
        />
      </svg>
    );
  };

  if (error) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">First-Time Visitors</h1>
          <p className="mt-2 text-gray-600">
            Track and manage first-time visitor check-ins
          </p>
        </div>

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
              <p className="text-red-700 font-medium">Failed to load visitor data</p>
              <p className="text-red-600 text-sm mt-1">
                {error instanceof Error ? error.message : 'An unknown error occurred'}
              </p>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Page header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">First-Time Visitors</h1>
        <p className="mt-2 text-gray-600">
          Track and manage first-time visitor check-ins
        </p>
      </div>

      {/* Date Range Picker */}
      <DateRangePicker value={dateRange} onChange={setDateRange} />

      {/* Campus Filter */}
      {onCampusFilterChange && (
        <div className="bg-white rounded-lg border border-gray-200 p-4">
          <label
            htmlFor="campus-filter"
            className="block text-sm font-medium text-gray-700 mb-2"
          >
            Filter by Campus
          </label>
          <select
            id="campus-filter"
            value={campusFilter}
            onChange={(e) => onCampusFilterChange(e.target.value)}
            className="w-full md:w-64 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">All Campuses</option>
            {/* Campus options would be populated from API */}
          </select>
        </div>
      )}

      {/* Visitor Table */}
      {isLoading ? (
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <div className="flex items-center justify-center py-12">
            <div className="text-center">
              <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
              <p className="mt-2 text-sm text-gray-500">Loading visitors...</p>
            </div>
          </div>
        </div>
      ) : !visitors || visitors.length === 0 ? (
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <div className="flex items-center justify-center py-12">
            <div className="text-center text-gray-500">
              <svg
                className="w-16 h-16 mx-auto mb-4 text-gray-300"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
                />
              </svg>
              <p>No first-time visitors for selected date range</p>
            </div>
          </div>
        </div>
      ) : (
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-gray-900">
              {sortedVisitors.length} Visitor{sortedVisitors.length !== 1 ? 's' : ''}
            </h3>
          </div>

          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th
                    scope="col"
                    className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100 transition-colors"
                    onClick={() => handleSort('personName')}
                  >
                    <div className="flex items-center gap-2">
                      <span>Name</span>
                      <SortIcon field="personName" />
                    </div>
                  </th>
                  <th
                    scope="col"
                    className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100 transition-colors"
                    onClick={() => handleSort('checkInDateTime')}
                  >
                    <div className="flex items-center gap-2">
                      <span>Check-in Time</span>
                      <SortIcon field="checkInDateTime" />
                    </div>
                  </th>
                  <th
                    scope="col"
                    className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100 transition-colors"
                    onClick={() => handleSort('groupName')}
                  >
                    <div className="flex items-center gap-2">
                      <span>Group</span>
                      <SortIcon field="groupName" />
                    </div>
                  </th>
                  <th
                    scope="col"
                    className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100 transition-colors"
                    onClick={() => handleSort('campusName')}
                  >
                    <div className="flex items-center gap-2">
                      <span>Campus</span>
                      <SortIcon field="campusName" />
                    </div>
                  </th>
                  <th
                    scope="col"
                    className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                  >
                    Follow-up Status
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {sortedVisitors.map((visitor) => (
                  <tr
                    key={visitor.personIdKey}
                    className="hover:bg-gray-50 transition-colors"
                  >
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm font-medium text-gray-900">
                        {visitor.personName}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-500">
                        {formatDateTime(visitor.checkInDateTime)}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-900">{visitor.groupName}</div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-500">
                        {visitor.campusName || '-'}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {visitor.hasFollowUp ? (
                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                          Follow-up Created
                        </span>
                      ) : (
                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
                          No Follow-up
                        </span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
