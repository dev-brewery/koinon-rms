/**
 * Room Roster Page
 * Real-time view of who is currently checked into each room
 */

import { useState, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useRoomRoster } from '@/hooks/useRoomRoster';
import { RosterList } from '@/components/admin/roster/RosterList';
import { Button, Card, EmptyState, ErrorState } from '@/components/ui';
import { LocationPicker } from '@/components/LocationPicker';
import { getErrorMessage } from '@/lib/errorMessages';
import * as referenceApi from '@/services/api/reference';
import { STORAGE_KEYS } from '@/lib/storageKeys';

export function RosterPage() {
  const [selectedLocationIdKey, setSelectedLocationIdKey] = useState<string>('');
  const [autoRefresh, setAutoRefresh] = useState(true);
  const [campusIdKey, setCampusIdKey] = useState<string>('');

  // Fetch active campuses
  const { data: campuses } = useQuery({
    queryKey: ['campuses'],
    queryFn: () => referenceApi.getCampuses({ includeInactive: false }),
    staleTime: 10 * 60 * 1000, // 10 minutes
  });

  // Auto-select first active campus
  useEffect(() => {
    if (campuses && campuses.length > 0 && !campusIdKey) {
      setCampusIdKey(campuses[0].idKey);
    }
  }, [campuses, campusIdKey]);

  // Load saved location from localStorage on mount (CRITICAL-1 FIX: Single source of truth)
  useEffect(() => {
    const savedLocationIdKey = localStorage.getItem(STORAGE_KEYS.SELECTED_LOCATION_ID_KEY);
    if (savedLocationIdKey) {
      setSelectedLocationIdKey(savedLocationIdKey);
    }
  }, []);

  // Persist location selection to localStorage whenever it changes
  useEffect(() => {
    if (selectedLocationIdKey) {
      localStorage.setItem(STORAGE_KEYS.SELECTED_LOCATION_ID_KEY, selectedLocationIdKey);
    }
  }, [selectedLocationIdKey]);

  const { data: roster, isLoading, error, refetch } = useRoomRoster(
    selectedLocationIdKey || undefined,
    autoRefresh
  );

  return (
    <div className="space-y-6">
      {/* Page header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Room Roster</h1>
          <p className="mt-2 text-gray-600">
            Real-time view of children currently checked into rooms
          </p>
        </div>

        <div className="flex items-center gap-4">
          {/* Auto-refresh toggle */}
          <label className="flex items-center gap-2 text-sm text-gray-700">
            <input
              type="checkbox"
              checked={autoRefresh}
              onChange={(e) => setAutoRefresh(e.target.checked)}
              className="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
              data-testid="auto-refresh-checkbox"
            />
            Auto-refresh (30s)
          </label>

          {/* Manual refresh button */}
          <Button
            onClick={() => refetch()}
            variant="secondary"
            disabled={!selectedLocationIdKey || isLoading}
          >
            <svg
              className="w-4 h-4 mr-2"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
              />
            </svg>
            Refresh
          </Button>
        </div>
      </div>

      {/* Location selector */}
      <Card className="p-4">
        <LocationPicker
          value={selectedLocationIdKey}
          onChange={setSelectedLocationIdKey}
          campusIdKey={campusIdKey}
          data-testid="location-picker"
        />
        {!campusIdKey && (
          <div className="mt-4 p-3 bg-yellow-50 border border-yellow-200 rounded-md">
            <p className="text-sm text-yellow-800">
              <strong>Note:</strong> Campus configuration is required to load rooms.
              This should be automatically loaded from your user context or organization settings.
            </p>
          </div>
        )}
      </Card>

      {/* Error state */}
      {error && (
        <div data-testid="roster-error">
          <Card>
            <ErrorState
              title="Failed to load roster"
              message={getErrorMessage(error).message}
              onRetry={() => refetch()}
            />
          </Card>
        </div>
      )}

      {/* Roster display */}
      {selectedLocationIdKey && !error && (
        <RosterList roster={roster} isLoading={isLoading} data-testid="roster-list" />
      )}

      {/* Empty state */}
      {!selectedLocationIdKey && (
        <Card>
          <EmptyState
            title="Select a room to view the roster"
            description="Pick a location above to see who is currently checked in. Toggle auto-refresh to keep the roster live during check-in."
          />
        </Card>
      )}
    </div>
  );
}
