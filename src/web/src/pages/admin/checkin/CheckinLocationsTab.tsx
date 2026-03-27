/**
 * Check-in Locations Tab
 * Manage location capacity settings for check-in
 */

import { useState } from 'react';
import { useLocations } from '@/hooks/useLocations';
import { Loading, EmptyState, ErrorState } from '@/components/ui';
import type { LocationSummaryDto } from '@/types/location';
import { LocationCapacityModal } from './LocationCapacityModal';

export function CheckinLocationsTab() {
  const [editingLocation, setEditingLocation] = useState<LocationSummaryDto | null>(null);

  const { data: locations, isLoading, error, refetch } = useLocations();

  const locationList = locations ?? [];

  const handleEditClose = () => {
    setEditingLocation(null);
  };

  if (isLoading) {
    return <Loading text="Loading locations..." />;
  }

  if (error) {
    return (
      <ErrorState
        title="Failed to load locations"
        message={error instanceof Error ? error.message : 'Unknown error'}
        onRetry={() => refetch()}
      />
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-gray-600">
          {locationList.length} {locationList.length === 1 ? 'location' : 'locations'}
        </p>
      </div>

      {locationList.length === 0 ? (
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
                d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"
              />
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"
              />
            </svg>
          }
          title="No locations configured"
          description="Locations are managed under Settings > Locations"
        />
      ) : (
        <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Location
                </th>
                <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Campus
                </th>
                <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Type
                </th>
                <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Status
                </th>
                <th scope="col" className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-100">
              {locationList.map((location) => (
                <LocationRow
                  key={location.idKey}
                  location={location}
                  onEdit={() => setEditingLocation(location)}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}

      {editingLocation && (
        <LocationCapacityModal
          locationIdKey={editingLocation.idKey}
          locationName={editingLocation.name}
          onClose={handleEditClose}
        />
      )}
    </div>
  );
}

// ============================================================================
// Location Row
// ============================================================================

interface LocationRowProps {
  location: LocationSummaryDto;
  onEdit: () => void;
}

function LocationRow({ location, onEdit }: LocationRowProps) {
  return (
    <tr className="hover:bg-gray-50">
      <td className="px-6 py-4 whitespace-nowrap">
        <div>
          <div className="text-sm font-medium text-gray-900">{location.name}</div>
          {location.parentLocationName && (
            <div className="text-xs text-gray-500">{location.parentLocationName}</div>
          )}
        </div>
      </td>
      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
        {location.campusName ?? <span className="text-gray-400">—</span>}
      </td>
      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
        {location.locationTypeName ?? <span className="text-gray-400">—</span>}
      </td>
      <td className="px-6 py-4 whitespace-nowrap">
        <span
          className={`inline-flex px-2 py-0.5 text-xs font-medium rounded-full ${
            location.isActive
              ? 'bg-green-100 text-green-800'
              : 'bg-gray-100 text-gray-600'
          }`}
        >
          {location.isActive ? 'Active' : 'Inactive'}
        </span>
      </td>
      <td className="px-6 py-4 whitespace-nowrap text-right">
        <button
          onClick={onEdit}
          className="text-sm font-medium text-primary-600 hover:text-primary-700"
        >
          Edit Capacity
        </button>
      </td>
    </tr>
  );
}
