/**
 * Group Schedules Section
 * Displays and manages schedules associated with a group
 */

import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useGroupSchedules, useAddGroupSchedule, useRemoveGroupSchedule } from '@/hooks/useGroups';
import { useSchedules } from '@/hooks/useSchedules';
import { DAYS_OF_WEEK, formatTime12Hour } from '@/utils/dateFormatters';
import type { GroupScheduleDto } from '@/services/api/types';

interface GroupSchedulesSectionProps {
  groupIdKey: string;
}

export function GroupSchedulesSection({ groupIdKey }: GroupSchedulesSectionProps) {
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [removingScheduleId, setRemovingScheduleId] = useState<string | null>(null);

  const { data: groupSchedules, isLoading } = useGroupSchedules(groupIdKey);
  const addSchedule = useAddGroupSchedule();
  const removeSchedule = useRemoveGroupSchedule();

  const handleRemoveSchedule = async (scheduleIdKey: string) => {
    const confirmed = window.confirm('Remove this schedule from the group?');
    if (!confirmed) return;

    setRemovingScheduleId(scheduleIdKey);
    try {
      await removeSchedule.mutateAsync({ groupIdKey, scheduleIdKey });
    } catch {
      // Error handled by TanStack Query mutation error state
    } finally {
      setRemovingScheduleId(null);
    }
  };

  const handleAddSchedule = async (scheduleIdKey: string) => {
    try {
      await addSchedule.mutateAsync({
        groupIdKey,
        request: { scheduleIdKey, order: (groupSchedules?.length || 0) + 1 },
      });
      setIsAddModalOpen(false);
    } catch {
      // Error handled by TanStack Query mutation error state
    }
  };

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">
          Schedules ({groupSchedules?.length || 0})
        </h2>
        <button
          onClick={() => setIsAddModalOpen(true)}
          className="text-sm text-primary-600 hover:text-primary-700 font-medium"
        >
          Add Schedule
        </button>
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-8">
          <div className="inline-block w-6 h-6 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
        </div>
      ) : !groupSchedules || groupSchedules.length === 0 ? (
        <div className="text-center py-8 text-gray-500">
          <svg
            className="w-12 h-12 text-gray-300 mx-auto mb-3"
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
          <p>No schedules assigned</p>
          <p className="text-sm mt-1">Add schedules to control when this group is available for check-in</p>
        </div>
      ) : (
        <div className="space-y-2">
          {groupSchedules.map((gs: GroupScheduleDto) => (
            <div
              key={gs.idKey}
              className="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
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
                  <Link
                    to={`/admin/schedules/${gs.schedule.idKey}`}
                    className="text-sm font-medium text-gray-900 hover:text-primary-600"
                  >
                    {gs.schedule.name}
                  </Link>
                  {gs.schedule.weeklyDayOfWeek !== undefined && gs.schedule.weeklyTimeOfDay && (
                    <p className="text-xs text-gray-500">
                      {DAYS_OF_WEEK[gs.schedule.weeklyDayOfWeek]} at{' '}
                      {formatTime12Hour(gs.schedule.weeklyTimeOfDay)}
                    </p>
                  )}
                </div>
              </div>
              <button
                onClick={() => handleRemoveSchedule(gs.schedule.idKey)}
                disabled={removingScheduleId === gs.schedule.idKey}
                className="text-gray-400 hover:text-red-600 disabled:opacity-50"
                aria-label="Remove schedule"
              >
                <svg
                  className="w-5 h-5"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              </button>
            </div>
          ))}
        </div>
      )}

      {/* Add Schedule Modal */}
      {isAddModalOpen && (
        <AddScheduleModal
          groupIdKey={groupIdKey}
          existingScheduleIds={groupSchedules?.map((gs) => gs.schedule.idKey) || []}
          onAdd={handleAddSchedule}
          onClose={() => setIsAddModalOpen(false)}
          isAdding={addSchedule.isPending}
        />
      )}
    </div>
  );
}

interface AddScheduleModalProps {
  groupIdKey: string;
  existingScheduleIds: string[];
  onAdd: (scheduleIdKey: string) => void;
  onClose: () => void;
  isAdding: boolean;
}

function AddScheduleModal({
  existingScheduleIds,
  onAdd,
  onClose,
  isAdding,
}: AddScheduleModalProps) {
  const { data: schedulesData, isLoading } = useSchedules({ includeInactive: false });
  const schedules = schedulesData?.data || [];

  // Filter out already-assigned schedules
  const availableSchedules = schedules.filter(
    (s) => !existingScheduleIds.includes(s.idKey)
  );

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:p-0">
        <div
          className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"
          onClick={onClose}
        />

        <div className="relative inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
          <div className="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
            <div className="flex items-start justify-between mb-4">
              <h3 className="text-lg font-medium text-gray-900">Add Schedule</h3>
              <button
                onClick={onClose}
                className="text-gray-400 hover:text-gray-500"
                aria-label="Close"
              >
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
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              </button>
            </div>

            {isLoading ? (
              <div className="flex items-center justify-center py-8">
                <div className="inline-block w-6 h-6 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
              </div>
            ) : availableSchedules.length === 0 ? (
              <div className="text-center py-8 text-gray-500">
                <p>No available schedules to add</p>
                <Link
                  to="/admin/schedules/new"
                  className="mt-2 inline-block text-primary-600 hover:text-primary-700 font-medium"
                >
                  Create a new schedule
                </Link>
              </div>
            ) : (
              <div className="max-h-60 overflow-y-auto space-y-2">
                {availableSchedules.map((schedule) => (
                  <button
                    key={schedule.idKey}
                    onClick={() => onAdd(schedule.idKey)}
                    disabled={isAdding}
                    className="w-full p-3 text-left hover:bg-gray-50 rounded-lg border border-gray-200 transition-colors disabled:opacity-50"
                  >
                    <div className="font-medium text-gray-900">{schedule.name}</div>
                    {schedule.weeklyDayOfWeek !== undefined && schedule.weeklyTimeOfDay && (
                      <div className="text-sm text-gray-500">
                        {DAYS_OF_WEEK[schedule.weeklyDayOfWeek]} at{' '}
                        {formatTime12Hour(schedule.weeklyTimeOfDay)}
                      </div>
                    )}
                  </button>
                ))}
              </div>
            )}
          </div>

          <div className="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
            <button
              onClick={onClose}
              className="w-full sm:w-auto px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
