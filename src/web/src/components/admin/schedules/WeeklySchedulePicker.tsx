/**
 * Weekly Schedule Picker Component
 * Allows selection of day of week and time
 */

import { DAYS_OF_WEEK_SHORT, formatTime12Hour, generateTimeOptions } from '@/utils/dateFormatters';

interface WeeklySchedulePickerProps {
  dayOfWeek?: number;
  timeOfDay?: string;
  onDayChange: (day: number | undefined) => void;
  onTimeChange: (time: string | undefined) => void;
}

export function WeeklySchedulePicker({
  dayOfWeek,
  timeOfDay,
  onDayChange,
  onTimeChange,
}: WeeklySchedulePickerProps) {
  const timeOptions = generateTimeOptions();

  return (
    <div className="space-y-4">
      {/* Day of Week Selector */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Day of Week
        </label>
        <div className="grid grid-cols-7 gap-2">
          {DAYS_OF_WEEK_SHORT.map((day) => (
            <button
              key={day.value}
              type="button"
              onClick={() => onDayChange(dayOfWeek === day.value ? undefined : day.value)}
              className={`px-3 py-2 text-sm font-medium rounded-lg border transition-colors ${
                dayOfWeek === day.value
                  ? 'bg-primary-600 text-white border-primary-600'
                  : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50'
              }`}
              title={day.fullLabel}
            >
              {day.label}
            </button>
          ))}
        </div>
      </div>

      {/* Time Picker */}
      <div>
        <label htmlFor="timeOfDay" className="block text-sm font-medium text-gray-700 mb-2">
          Time
        </label>
        <select
          id="timeOfDay"
          value={timeOfDay || ''}
          onChange={(e) => onTimeChange(e.target.value || undefined)}
          className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
        >
          <option value="">Select time...</option>
          {timeOptions.map((time) => (
            <option key={time} value={time}>
              {formatTime12Hour(time)}
            </option>
          ))}
        </select>
      </div>

      {/* Preview */}
      {dayOfWeek !== undefined && timeOfDay && (
        <div className="p-3 bg-blue-50 border border-blue-200 rounded-lg">
          <div className="text-sm font-medium text-blue-900">
            Selected: {DAYS_OF_WEEK_SHORT[dayOfWeek].fullLabel} at {formatTime12Hour(timeOfDay)}
          </div>
        </div>
      )}
    </div>
  );
}
