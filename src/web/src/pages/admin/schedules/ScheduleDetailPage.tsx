/**
 * Schedule Detail Page
 * View and manage a single schedule
 */

import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useSchedule, useScheduleOccurrences, useDeleteSchedule } from '@/hooks/useSchedules';
import { DAYS_OF_WEEK, formatTime12Hour, formatDateTime } from '@/utils/dateFormatters';

export function ScheduleDetailPage() {
  const { idKey } = useParams<{ idKey: string }>();
  const navigate = useNavigate();
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  const { data: schedule, isLoading, error } = useSchedule(idKey);
  const { data: occurrences } = useScheduleOccurrences(idKey, undefined, 10);
  const deleteSchedule = useDeleteSchedule();

  const handleDelete = async () => {
    if (!idKey) return;

    try {
      await deleteSchedule.mutateAsync(idKey);
      navigate('/admin/schedules');
    } catch (error) {
      console.error('Failed to delete schedule:', error);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="inline-block w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
      </div>
    );
  }

  if (error || !schedule) {
    return (
      <div className="text-center py-12">
        <svg
          className="w-12 h-12 text-red-400 mx-auto mb-4"
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
        <p className="text-red-600">Failed to load schedule</p>
        <Link
          to="/admin/schedules"
          className="inline-block mt-4 px-4 py-2 text-primary-600 hover:text-primary-700"
        >
          Back to Schedules
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link
            to="/admin/schedules"
            className="p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100 transition-colors"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
            </svg>
          </Link>
          <div>
            <h1 className="text-3xl font-bold text-gray-900">{schedule.name}</h1>
            {schedule.description && (
              <p className="mt-1 text-gray-600">{schedule.description}</p>
            )}
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Link
            to={`/admin/schedules/${idKey}/edit`}
            className="px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
          >
            Edit
          </Link>
          <button
            onClick={() => setShowDeleteConfirm(true)}
            className="px-4 py-2 text-red-600 bg-white border border-red-300 rounded-lg hover:bg-red-50 transition-colors"
          >
            Delete
          </button>
        </div>
      </div>

      {/* Status Banner */}
      {!schedule.isActive && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
          <div className="flex items-center gap-2">
            <svg
              className="w-5 h-5 text-yellow-600"
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
            <p className="text-sm font-medium text-yellow-800">
              This schedule is inactive
            </p>
          </div>
        </div>
      )}

      {/* Main Content */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Details */}
        <div className="lg:col-span-2 space-y-6">
          {/* Schedule Info */}
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Schedule Details</h2>
            <dl className="space-y-4">
              {schedule.weeklyDayOfWeek !== undefined && schedule.weeklyTimeOfDay && (
                <div>
                  <dt className="text-sm font-medium text-gray-500">Day and Time</dt>
                  <dd className="mt-1 text-sm text-gray-900">
                    {DAYS_OF_WEEK[schedule.weeklyDayOfWeek]} at {formatTime12Hour(schedule.weeklyTimeOfDay)}
                  </dd>
                </div>
              )}

              {(schedule.checkInStartOffsetMinutes !== undefined ||
                schedule.checkInEndOffsetMinutes !== undefined) && (
                <div>
                  <dt className="text-sm font-medium text-gray-500">Check-in Window</dt>
                  <dd className="mt-1 text-sm text-gray-900">
                    Opens {schedule.checkInStartOffsetMinutes || 0} minutes before, closes{' '}
                    {schedule.checkInEndOffsetMinutes || 0} minutes after
                  </dd>
                </div>
              )}

              {schedule.effectiveStartDate && (
                <div>
                  <dt className="text-sm font-medium text-gray-500">Effective Start Date</dt>
                  <dd className="mt-1 text-sm text-gray-900">
                    {new Date(schedule.effectiveStartDate).toLocaleDateString()}
                  </dd>
                </div>
              )}

              {schedule.effectiveEndDate && (
                <div>
                  <dt className="text-sm font-medium text-gray-500">Effective End Date</dt>
                  <dd className="mt-1 text-sm text-gray-900">
                    {new Date(schedule.effectiveEndDate).toLocaleDateString()}
                  </dd>
                </div>
              )}
            </dl>
          </div>

          {/* Upcoming Occurrences */}
          {occurrences && occurrences.length > 0 && (
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">
                Next 10 Occurrences
              </h2>
              <div className="space-y-2">
                {occurrences.map((occurrence, index) => (
                  <div
                    key={index}
                    className="flex items-center justify-between p-3 hover:bg-gray-50 rounded-lg transition-colors"
                  >
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 rounded bg-blue-100 text-blue-600 flex items-center justify-center">
                        <svg
                          className="w-4 h-4"
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
                      </div>
                      <div>
                        <div className="text-sm font-medium text-gray-900">
                          {occurrence.dayOfWeekName} - {occurrence.formattedTime}
                        </div>
                        <div className="text-xs text-gray-500">
                          {formatDateTime(occurrence.occurrenceDateTime)}
                        </div>
                      </div>
                    </div>
                    {occurrence.isCheckInWindowOpen && (
                      <span className="px-2 py-1 text-xs font-medium bg-green-100 text-green-800 rounded">
                        Open Now
                      </span>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Status */}
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Status</h2>
            <dl className="space-y-3">
              <div>
                <dt className="text-sm text-gray-500">Active</dt>
                <dd>
                  <span
                    className={`inline-block mt-1 px-2 py-0.5 text-xs font-medium rounded ${
                      schedule.isActive
                        ? 'bg-green-100 text-green-800'
                        : 'bg-gray-100 text-gray-800'
                    }`}
                  >
                    {schedule.isActive ? 'Active' : 'Inactive'}
                  </span>
                </dd>
              </div>
              <div>
                <dt className="text-sm text-gray-500">Check-in Active</dt>
                <dd>
                  <span
                    className={`inline-block mt-1 px-2 py-0.5 text-xs font-medium rounded ${
                      schedule.isCheckinActive
                        ? 'bg-green-100 text-green-800'
                        : 'bg-gray-100 text-gray-800'
                    }`}
                  >
                    {schedule.isCheckinActive ? 'Yes' : 'No'}
                  </span>
                </dd>
              </div>
              <div>
                <dt className="text-sm text-gray-500">Public</dt>
                <dd>
                  <span
                    className={`inline-block mt-1 px-2 py-0.5 text-xs font-medium rounded ${
                      schedule.isPublic
                        ? 'bg-blue-100 text-blue-800'
                        : 'bg-gray-100 text-gray-800'
                    }`}
                  >
                    {schedule.isPublic ? 'Yes' : 'No'}
                  </span>
                </dd>
              </div>
            </dl>
          </div>

          {/* Metadata */}
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Metadata</h2>
            <dl className="space-y-3 text-sm">
              <div>
                <dt className="text-gray-500">Created</dt>
                <dd className="text-gray-900">
                  {new Date(schedule.createdDateTime).toLocaleDateString()}
                </dd>
              </div>
              {schedule.modifiedDateTime && (
                <div>
                  <dt className="text-gray-500">Last Modified</dt>
                  <dd className="text-gray-900">
                    {new Date(schedule.modifiedDateTime).toLocaleDateString()}
                  </dd>
                </div>
              )}
              <div>
                <dt className="text-gray-500">Order</dt>
                <dd className="text-gray-900">{schedule.order}</dd>
              </div>
            </dl>
          </div>
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      {showDeleteConfirm && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Delete Schedule</h3>
            <p className="text-gray-600 mb-6">
              Are you sure you want to delete "{schedule.name}"? This action cannot be undone.
            </p>
            <div className="flex items-center gap-3 justify-end">
              <button
                onClick={() => setShowDeleteConfirm(false)}
                className="px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={handleDelete}
                disabled={deleteSchedule.isPending}
                className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50"
              >
                {deleteSchedule.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
