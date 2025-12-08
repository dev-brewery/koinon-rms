/**
 * Check-in Window Preview Component
 * Visual preview of schedule time and check-in window
 */

import { DAYS_OF_WEEK, formatTime12Hour } from '@/utils/dateFormatters';

interface CheckinWindowPreviewProps {
  dayOfWeek?: number;
  timeOfDay?: string;
  checkInStartOffsetMinutes?: number;
  checkInEndOffsetMinutes?: number;
}

function addMinutesToTime(time24: string, minutesToAdd: number): string {
  const [hours, minutes] = time24.split(':').map(Number);
  const totalMinutes = hours * 60 + minutes + minutesToAdd;
  const newHours = Math.floor(totalMinutes / 60) % 24;
  const newMinutes = totalMinutes % 60;
  return `${String(newHours).padStart(2, '0')}:${String(newMinutes).padStart(2, '0')}:00`;
}

export function CheckinWindowPreview({
  dayOfWeek,
  timeOfDay,
  checkInStartOffsetMinutes = 0,
  checkInEndOffsetMinutes = 0,
}: CheckinWindowPreviewProps) {
  if (dayOfWeek === undefined || !timeOfDay) {
    return (
      <div className="p-4 bg-gray-50 border border-gray-200 rounded-lg">
        <p className="text-sm text-gray-500 text-center">
          Select a day and time to preview check-in window
        </p>
      </div>
    );
  }

  const scheduleTime = formatTime12Hour(timeOfDay);
  const checkInStart = addMinutesToTime(timeOfDay, -checkInStartOffsetMinutes);
  const checkInEnd = addMinutesToTime(timeOfDay, checkInEndOffsetMinutes);

  return (
    <div className="p-4 bg-white border border-gray-200 rounded-lg space-y-4">
      <h3 className="text-sm font-semibold text-gray-900">Check-in Window Preview</h3>

      {/* Day and Time */}
      <div className="flex items-center gap-2">
        <svg
          className="w-5 h-5 text-gray-400"
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
        <div>
          <div className="text-sm font-medium text-gray-900">
            {DAYS_OF_WEEK[dayOfWeek]} at {scheduleTime}
          </div>
        </div>
      </div>

      {/* Timeline */}
      <div className="space-y-2">
        {/* Check-in Start */}
        <div className="flex items-center gap-3">
          <div className="w-24 text-xs text-gray-500 text-right">
            {formatTime12Hour(checkInStart)}
          </div>
          <div className="flex-1">
            <div className="h-1 bg-green-200 rounded-l" />
          </div>
          <div className="w-32 text-xs text-gray-600">Check-in opens</div>
        </div>

        {/* Schedule Time */}
        <div className="flex items-center gap-3">
          <div className="w-24 text-xs font-medium text-primary-600 text-right">
            {scheduleTime}
          </div>
          <div className="flex-1 relative">
            <div className="h-1 bg-primary-200" />
            <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2">
              <div className="w-3 h-3 bg-primary-600 rounded-full border-2 border-white" />
            </div>
          </div>
          <div className="w-32 text-xs font-medium text-primary-600">Service time</div>
        </div>

        {/* Check-in End */}
        <div className="flex items-center gap-3">
          <div className="w-24 text-xs text-gray-500 text-right">
            {formatTime12Hour(checkInEnd)}
          </div>
          <div className="flex-1">
            <div className="h-1 bg-red-200 rounded-r" />
          </div>
          <div className="w-32 text-xs text-gray-600">Check-in closes</div>
        </div>
      </div>

      {/* Window Duration */}
      <div className="pt-3 border-t border-gray-200">
        <div className="text-xs text-gray-600">
          <span className="font-medium">Window duration:</span>{' '}
          {checkInStartOffsetMinutes + checkInEndOffsetMinutes} minutes total
          <span className="text-gray-400">
            {' '}
            ({checkInStartOffsetMinutes} min before, {checkInEndOffsetMinutes} min after)
          </span>
        </div>
      </div>
    </div>
  );
}
