/**
 * Upcoming Schedules Component
 * Displays upcoming check-in schedules with status indicators
 */

import { Link } from 'react-router-dom';
import type { UpcomingSchedule } from '@/services/api/dashboard';

export interface UpcomingSchedulesProps {
  schedules: UpcomingSchedule[];
  isLoading: boolean;
}

export function UpcomingSchedules({ schedules, isLoading }: UpcomingSchedulesProps) {
  if (isLoading) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Upcoming Schedules</h2>
        <div className="flex items-center justify-center py-12">
          <div className="flex items-center gap-3 text-gray-500">
            <svg className="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24" aria-hidden="true">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path
                className="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
              />
            </svg>
            <span>Loading schedules...</span>
          </div>
        </div>
      </div>
    );
  }

  if (schedules.length === 0) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Upcoming Schedules</h2>
        <div className="text-center py-12">
          <svg
            className="w-12 h-12 text-gray-400 mx-auto mb-4"
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
          <p className="text-gray-500">No upcoming schedules</p>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <h2 className="text-lg font-semibold text-gray-900 mb-4">Upcoming Schedules</h2>
      <div className="space-y-3">
        {schedules.map((schedule) => (
          <ScheduleItem key={schedule.idKey} schedule={schedule} />
        ))}
      </div>
    </div>
  );
}

interface ScheduleItemProps {
  schedule: UpcomingSchedule;
}

function ScheduleItem({ schedule }: ScheduleItemProps) {
  const isOpen = schedule.minutesUntilCheckIn <= 0;
  const formattedTime = new Date(schedule.nextOccurrence).toLocaleString('en-US', {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });

  // Calculate status text and color
  let statusText: string;
  let statusColor: string;

  if (isOpen) {
    statusText = 'Open';
    statusColor = 'text-green-700 bg-green-50 border-green-200';
  } else {
    const minutes = schedule.minutesUntilCheckIn;
    if (minutes < 60) {
      statusText = `Opens in ${minutes} min`;
      statusColor = 'text-yellow-700 bg-yellow-50 border-yellow-200';
    } else {
      const hours = Math.floor(minutes / 60);
      statusText = `Opens in ${hours} hr${hours > 1 ? 's' : ''}`;
      statusColor = 'text-gray-700 bg-gray-50 border-gray-200';
    }
  }

  return (
    <Link
      to={`/admin/schedules/${schedule.idKey}`}
      className="flex items-center justify-between p-4 rounded-lg border border-gray-200 hover:border-primary-300 hover:bg-primary-50 transition-all group"
    >
      <div className="flex-1 min-w-0">
        <h3 className="text-sm font-semibold text-gray-900 group-hover:text-primary-700">
          {schedule.name}
        </h3>
        <p className="mt-1 text-sm text-gray-500">
          <svg className="w-4 h-4 inline-block mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
          {formattedTime}
        </p>
      </div>
      <div className={`px-3 py-1 text-xs font-medium rounded-full border ${statusColor}`}>
        {statusText}
      </div>
    </Link>
  );
}
