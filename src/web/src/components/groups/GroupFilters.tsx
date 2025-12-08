/**
 * GroupFilters Component
 * Filter controls for public group search
 */

import { useQuery } from '@tanstack/react-query';
import type { PublicGroupSearchParams } from '@/services/api/publicGroups';
import { getCampuses } from '@/services/api/reference';

export interface GroupFiltersProps {
  filters: PublicGroupSearchParams;
  onFiltersChange: (filters: PublicGroupSearchParams) => void;
}

const DAYS_OF_WEEK = [
  { value: 0, label: 'Sunday' },
  { value: 1, label: 'Monday' },
  { value: 2, label: 'Tuesday' },
  { value: 3, label: 'Wednesday' },
  { value: 4, label: 'Thursday' },
  { value: 5, label: 'Friday' },
  { value: 6, label: 'Saturday' },
];

const TIMES_OF_DAY = [
  { value: 0, label: 'Morning (6AM-12PM)' },
  { value: 1, label: 'Afternoon (12PM-5PM)' },
  { value: 2, label: 'Evening (5PM-10PM)' },
] as const;

export function GroupFilters({ filters, onFiltersChange }: GroupFiltersProps) {
  const { data: campuses = [] } = useQuery({
    queryKey: ['campuses', { public: true }],
    queryFn: () => getCampuses({ includeInactive: false }),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onFiltersChange({ ...filters, searchTerm: e.target.value || undefined });
  };

  const handleCampusChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    onFiltersChange({ ...filters, campusIdKey: e.target.value || undefined });
  };

  const handleDayChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const value = e.target.value;
    onFiltersChange({
      ...filters,
      dayOfWeek: value ? Number(value) : undefined
    });
  };

  const handleTimeChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const value = e.target.value;
    onFiltersChange({
      ...filters,
      timeOfDay: value ? (Number(value) as 0 | 1 | 2) : undefined
    });
  };

  const handleOpeningsChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onFiltersChange({ ...filters, hasOpenings: e.target.checked || undefined });
  };

  const handleClearFilters = () => {
    onFiltersChange({});
  };

  const hasActiveFilters =
    filters.searchTerm ||
    filters.campusIdKey ||
    filters.dayOfWeek !== undefined ||
    filters.timeOfDay ||
    filters.hasOpenings;

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <div className="space-y-4">
        {/* Search Input */}
        <div>
          <label htmlFor="search" className="block text-sm font-medium text-gray-700 mb-2">
            Search Groups
          </label>
          <input
            id="search"
            type="text"
            value={filters.searchTerm || ''}
            onChange={handleSearchChange}
            placeholder="Search by name or description..."
            className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Filter Row */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {/* Campus Filter */}
          <div>
            <label htmlFor="campus" className="block text-sm font-medium text-gray-700 mb-2">
              Campus
            </label>
            <select
              id="campus"
              value={filters.campusIdKey || ''}
              onChange={handleCampusChange}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="">All Campuses</option>
              {campuses.map((campus) => (
                <option key={campus.idKey} value={campus.idKey}>
                  {campus.name}
                </option>
              ))}
            </select>
          </div>

          {/* Day of Week Filter */}
          <div>
            <label htmlFor="dayOfWeek" className="block text-sm font-medium text-gray-700 mb-2">
              Day of Week
            </label>
            <select
              id="dayOfWeek"
              value={filters.dayOfWeek !== undefined ? String(filters.dayOfWeek) : ''}
              onChange={handleDayChange}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="">Any Day</option>
              {DAYS_OF_WEEK.map((day) => (
                <option key={day.value} value={day.value}>
                  {day.label}
                </option>
              ))}
            </select>
          </div>

          {/* Time of Day Filter */}
          <div>
            <label htmlFor="timeOfDay" className="block text-sm font-medium text-gray-700 mb-2">
              Time of Day
            </label>
            <select
              id="timeOfDay"
              value={filters.timeOfDay !== undefined ? String(filters.timeOfDay) : ''}
              onChange={handleTimeChange}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="">Any Time</option>
              {TIMES_OF_DAY.map((time) => (
                <option key={time.value} value={time.value}>
                  {time.label}
                </option>
              ))}
            </select>
          </div>

          {/* Has Openings Checkbox */}
          <div className="flex items-end">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={filters.hasOpenings || false}
                onChange={handleOpeningsChange}
                className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-2 focus:ring-blue-500"
              />
              <span className="text-sm font-medium text-gray-700">
                Has openings only
              </span>
            </label>
          </div>
        </div>

        {/* Clear Filters Button */}
        {hasActiveFilters && (
          <div className="flex justify-end">
            <button
              onClick={handleClearFilters}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200 transition-colors"
            >
              Clear Filters
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
