/**
 * Room Roster Page
 * Real-time view of who is currently checked into each room
 */

import { useState } from 'react';
import { useRoomRoster } from '@/hooks/useRoomRoster';
import { RosterList } from '@/components/admin/roster/RosterList';
import { Button, Card } from '@/components/ui';

export function RosterPage() {
  const [selectedLocationIdKey, setSelectedLocationIdKey] = useState<string>('');
  const [autoRefresh, setAutoRefresh] = useState(true);

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

      {/* Location selector placeholder - TODO(#120): Implement location picker */}
      <Card className="p-4">
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Select Room
        </label>
        <input
          type="text"
          value={selectedLocationIdKey}
          onChange={(e) => setSelectedLocationIdKey(e.target.value)}
          placeholder="Enter location IdKey (temporary - will be a picker)"
          className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
        />
        <p className="mt-1 text-xs text-gray-500">
          TODO(#120): Replace with a proper location picker component
        </p>
      </Card>

      {/* Error state */}
      {error && (
        <Card className="p-6 bg-red-50 border border-red-200">
          <p className="text-red-700 font-medium">Failed to load roster</p>
          <p className="text-red-600 text-sm mt-1">
            {error instanceof Error ? error.message : 'An unknown error occurred'}
          </p>
        </Card>
      )}

      {/* Roster display */}
      {selectedLocationIdKey && !error && (
        <RosterList roster={roster} isLoading={isLoading} />
      )}

      {/* Empty state */}
      {!selectedLocationIdKey && (
        <Card className="p-12 text-center">
          <svg
            className="w-16 h-16 text-gray-400 mx-auto mb-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
            />
          </svg>
          <p className="text-gray-600">Select a room to view the roster</p>
        </Card>
      )}
    </div>
  );
}
