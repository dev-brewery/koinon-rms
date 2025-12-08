/**
 * Date Range Picker Component
 * Allows selection of predefined or custom date ranges
 */

import { useState } from 'react';

export interface DateRange {
  startDate: string;
  endDate: string;
}

export interface DateRangePickerProps {
  value: DateRange;
  onChange: (range: DateRange) => void;
}

type PresetOption = 'last7' | 'last30' | 'last90' | 'thisYear' | 'custom';

export function DateRangePicker({ value, onChange }: DateRangePickerProps) {
  const [selectedPreset, setSelectedPreset] = useState<PresetOption>('last30');
  const [isCustom, setIsCustom] = useState(false);

  const handlePresetChange = (preset: PresetOption) => {
    setSelectedPreset(preset);

    if (preset === 'custom') {
      setIsCustom(true);
      return;
    }

    setIsCustom(false);

    const today = new Date();
    const endDate = today.toISOString().split('T')[0];
    let startDate: string;

    switch (preset) {
      case 'last7':
        startDate = new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000)
          .toISOString()
          .split('T')[0];
        break;
      case 'last30':
        startDate = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000)
          .toISOString()
          .split('T')[0];
        break;
      case 'last90':
        startDate = new Date(today.getTime() - 90 * 24 * 60 * 60 * 1000)
          .toISOString()
          .split('T')[0];
        break;
      case 'thisYear':
        startDate = `${today.getFullYear()}-01-01`;
        break;
      default:
        return;
    }

    onChange({ startDate, endDate });
  };

  const handleCustomDateChange = (field: 'startDate' | 'endDate', dateValue: string) => {
    onChange({
      ...value,
      [field]: dateValue,
    });
  };

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-4 space-y-4">
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Date Range
        </label>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => handlePresetChange('last7')}
            className={`px-4 py-2 text-sm font-medium rounded-md transition-colors ${
              selectedPreset === 'last7' && !isCustom
                ? 'bg-blue-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            }`}
          >
            Last 7 Days
          </button>
          <button
            type="button"
            onClick={() => handlePresetChange('last30')}
            className={`px-4 py-2 text-sm font-medium rounded-md transition-colors ${
              selectedPreset === 'last30' && !isCustom
                ? 'bg-blue-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            }`}
          >
            Last 30 Days
          </button>
          <button
            type="button"
            onClick={() => handlePresetChange('last90')}
            className={`px-4 py-2 text-sm font-medium rounded-md transition-colors ${
              selectedPreset === 'last90' && !isCustom
                ? 'bg-blue-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            }`}
          >
            Last 90 Days
          </button>
          <button
            type="button"
            onClick={() => handlePresetChange('thisYear')}
            className={`px-4 py-2 text-sm font-medium rounded-md transition-colors ${
              selectedPreset === 'thisYear' && !isCustom
                ? 'bg-blue-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            }`}
          >
            This Year
          </button>
          <button
            type="button"
            onClick={() => handlePresetChange('custom')}
            className={`px-4 py-2 text-sm font-medium rounded-md transition-colors ${
              isCustom
                ? 'bg-blue-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            }`}
          >
            Custom
          </button>
        </div>
      </div>

      {isCustom && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label
              htmlFor="startDate"
              className="block text-sm font-medium text-gray-700 mb-1"
            >
              Start Date
            </label>
            <input
              type="date"
              id="startDate"
              value={value.startDate}
              onChange={(e) => handleCustomDateChange('startDate', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label
              htmlFor="endDate"
              className="block text-sm font-medium text-gray-700 mb-1"
            >
              End Date
            </label>
            <input
              type="date"
              id="endDate"
              value={value.endDate}
              onChange={(e) => handleCustomDateChange('endDate', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>
      )}
    </div>
  );
}
