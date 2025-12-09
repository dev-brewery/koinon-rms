import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getCheckinConfiguration } from '@/services/api/checkin';
import type { CheckinLocationDto, CheckinAreaDto } from '@/services/api/types';

export interface LocationPickerProps {
  value: string;
  onChange: (locationIdKey: string) => void;
  campusIdKey?: string;
  className?: string;
  disabled?: boolean;
}

/**
 * LocationPicker Component
 * Displays a dropdown of check-in enabled locations with room capacity
 *
 * IMPORTANT: This is a pure controlled component. The parent component
 * (RosterPage) is responsible for localStorage persistence to avoid
 * race conditions and maintain single source of truth.
 *
 * TODO(#149): Extract hardcoded localStorage key 'selectedLocationIdKey'
 * to a shared constant or configuration file when implementing multi-location
 * selection or reusable location pickers. For now, this component is
 * single-purpose for the Room Roster page.
 */
export function LocationPicker({
  value,
  onChange,
  campusIdKey,
  className = '',
  disabled = false,
}: LocationPickerProps) {
  const [locations, setLocations] = useState<CheckinLocationDto[]>([]);

  // Fetch check-in configuration to get available locations
  const { data: config, isLoading, error } = useQuery({
    queryKey: ['checkin', 'configuration', campusIdKey],
    queryFn: () => getCheckinConfiguration({ campusId: campusIdKey }),
    enabled: !!campusIdKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  // Extract and flatten all check-in enabled locations from areas
  useEffect(() => {
    if (config?.areas) {
      const allLocations: CheckinLocationDto[] = [];

      config.areas.forEach((area: CheckinAreaDto) => {
        // Areas contain groups, groups contain locations
        area.groups.forEach((group) => {
          // CRITICAL-2 FIX: Null coalescing prevents crash if locations is undefined/null
          const activeLocations = (group.locations || []).filter((loc: CheckinLocationDto) => loc.isOpen);
          allLocations.push(...activeLocations);
        });
      });

      // Sort by name for better UX
      allLocations.sort((a, b) => a.name.localeCompare(b.name));

      setLocations(allLocations);
    }
  }, [config]);

  if (error) {
    return (
      <div className="text-red-600 text-sm">
        Failed to load locations: {error instanceof Error ? error.message : 'Unknown error'}
      </div>
    );
  }

  return (
    <div className={className}>
      <label htmlFor="location-picker" className="block text-sm font-medium text-gray-700 mb-2">
        Select Room
      </label>

      <select
        id="location-picker"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        disabled={disabled || isLoading}
        className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
      >
        <option value="">
          {isLoading ? 'Loading locations...' : 'Select a room'}
        </option>

        {locations.map((location) => (
          <option key={location.idKey} value={location.idKey}>
            {location.name}
            {location.softThreshold !== undefined && location.softThreshold !== null
              ? ` (Capacity: ${location.softThreshold})`
              : ''}
          </option>
        ))}
      </select>

      {!campusIdKey && (
        <p className="mt-1 text-xs text-gray-500">
          Please configure a campus to load available rooms
        </p>
      )}
    </div>
  );
}
