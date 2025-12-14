/**
 * Schedule List Page
 * Search and filter schedules
 */

import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useSchedules, useUpdateSchedule } from '@/hooks/useSchedules';
import { DAYS_OF_WEEK, formatTime12Hour } from '@/utils/dateFormatters';
import { Loading, EmptyState, ErrorState } from '@/components/ui';
import { useToast } from '@/contexts/ToastContext';

export function ScheduleListPage() {
  const navigate = useNavigate();
  const toast = useToast();
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedDay, setSelectedDay] = useState<number | undefined>();
  const [includeInactive, setIncludeInactive] = useState(false);

  const { data, isLoading, error, refetch } = useSchedules({
    query: searchQuery || undefined,
    dayOfWeek: selectedDay,
    includeInactive,
  });

  const updateSchedule = useUpdateSchedule();

  const schedules = data?.data || [];

  const handleToggleActive = async (idKey: string, currentStatus: boolean) => {
    try {
      await updateSchedule.mutateAsync({
        idKey,
        request: { isActive: !currentStatus },
      });
    } catch (error) {
      toast.error('Failed to update schedule status', 'Please try again later');
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Schedules</h1>
          <p className="mt-2 text-gray-600">Manage service times and check-in schedules</p>
        </div>
        <Link
          to="/admin/schedules/new"
          className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
        >
          Create Schedule
        </Link>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {/* Search */}
          <div className="md:col-span-2">
            <label htmlFor="search" className="block text-sm font-medium text-gray-700 mb-1">
              Search
            </label>
            <input
              type="text"
              id="search"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search schedules..."
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>

          {/* Day Filter */}
          <div>
            <label htmlFor="dayFilter" className="block text-sm font-medium text-gray-700 mb-1">
              Day of Week
            </label>
            <select
              id="dayFilter"
              value={selectedDay ?? ''}
              onChange={(e) => setSelectedDay(e.target.value ? Number(e.target.value) : undefined)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            >
              <option value="">All days</option>
              {DAYS_OF_WEEK.map((day, index) => (
                <option key={index} value={index}>
                  {day}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Include Inactive Toggle */}
        <div className="mt-4 flex items-center">
          <input
            type="checkbox"
            id="includeInactive"
            checked={includeInactive}
            onChange={(e) => setIncludeInactive(e.target.checked)}
            className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
          />
          <label htmlFor="includeInactive" className="ml-2 text-sm text-gray-700">
            Include inactive schedules
          </label>
        </div>
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="bg-white rounded-lg border border-gray-200">
          <Loading text="Loading schedules..." />
        </div>
      )}

      {/* Error State */}
      {error && (
        <div className="bg-white rounded-lg border border-gray-200">
          <ErrorState
            title="Failed to load schedules"
            message={error instanceof Error ? error.message : 'Unknown error'}
            onRetry={() => refetch()}
          />
        </div>
      )}

      {/* Schedules List */}
      {!isLoading && !error && (
        <div className="space-y-4">
          {schedules.length === 0 ? (
            <div className="bg-white rounded-lg border border-gray-200">
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
                title={searchQuery || selectedDay !== undefined ? "No schedules found" : "No schedules yet"}
                description={searchQuery || selectedDay !== undefined
                  ? "Try adjusting your filters"
                  : "Get started by creating your first schedule"}
                action={!searchQuery && selectedDay === undefined ? {
                  label: "Create Schedule",
                  onClick: () => navigate('/admin/schedules/new')
                } : undefined}
              />
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {schedules.map((schedule) => (
                <div
                  key={schedule.idKey}
                  className="bg-white rounded-lg border border-gray-200 p-5 hover:shadow-md transition-shadow"
                >
                  <div className="flex items-start justify-between mb-3">
                    <div className="flex-1">
                      <Link
                        to={`/admin/schedules/${schedule.idKey}`}
                        className="text-lg font-semibold text-gray-900 hover:text-primary-600"
                      >
                        {schedule.name}
                      </Link>
                      {schedule.description && (
                        <p className="mt-1 text-sm text-gray-600 line-clamp-2">
                          {schedule.description}
                        </p>
                      )}
                    </div>
                  </div>

                  {/* Day and Time */}
                  {schedule.weeklyDayOfWeek !== undefined && schedule.weeklyTimeOfDay && (
                    <div className="flex items-center gap-2 mb-3">
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
                          d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
                        />
                      </svg>
                      <span className="text-sm text-gray-700">
                        {DAYS_OF_WEEK[schedule.weeklyDayOfWeek]} at{' '}
                        {formatTime12Hour(schedule.weeklyTimeOfDay)}
                      </span>
                    </div>
                  )}

                  {/* Status and Actions */}
                  <div className="flex items-center justify-between pt-3 border-t border-gray-200">
                    <button
                      onClick={() => handleToggleActive(schedule.idKey, schedule.isActive)}
                      disabled={updateSchedule.isPending}
                      className={`px-3 py-1 text-xs font-medium rounded-full transition-colors ${
                        schedule.isActive
                          ? 'bg-green-100 text-green-800 hover:bg-green-200'
                          : 'bg-gray-100 text-gray-800 hover:bg-gray-200'
                      }`}
                    >
                      {schedule.isActive ? 'Active' : 'Inactive'}
                    </button>

                    <Link
                      to={`/admin/schedules/${schedule.idKey}`}
                      className="text-sm text-primary-600 hover:text-primary-700 font-medium"
                    >
                      View Details
                    </Link>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
