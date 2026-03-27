/**
 * CheckinDashboardPage
 * Real-time operations view for children's ministry coordinators.
 * Shows all check-in rooms simultaneously with live roster data.
 */

import { useState, useMemo } from 'react';
import { useCheckinConfiguration } from '@/hooks/useCheckin';
import { useMultipleRoomRosters } from '@/hooks/useRoomRoster';
import { useToggleRoomStatus } from '@/hooks/useLocations';
import {
  DashboardSummaryBar,
  DashboardSearch,
  RoomCard,
} from '@/components/admin/checkin-dashboard';
import { Button } from '@/components/ui';
import { useAuth } from '@/hooks/useAuth';
import { getAccessToken } from '@/services/api/client';
import type { CheckinLocationDto, RoomRosterDto } from '@/services/api/types';

// ── Role guard helpers ──────────────────────────────────────────────────────

/**
 * Decode the roles claim from the current JWT access token without an external
 * library.  Returns an empty array if the token is absent or malformed.
 */
function getRolesFromToken(): string[] {
  try {
    const token = getAccessToken();
    if (!token) return [];
    const payloadBase64 = token.split('.')[1];
    if (!payloadBase64) return [];
    // Replace URL-safe chars and pad to a multiple of 4
    const padded = payloadBase64.replace(/-/g, '+').replace(/_/g, '/');
    const decoded = atob(padded);
    const payload: unknown = JSON.parse(decoded);
    if (typeof payload !== 'object' || payload === null) return [];
    // ASP.NET Core emits roles as the standard ClaimTypes role URI or the
    // shorthand "role" key; handle both forms.
    const p = payload as Record<string, unknown>;
    const raw =
      p['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
      p['role'] ??
      p['roles'];
    if (Array.isArray(raw)) return raw.filter((r): r is string => typeof r === 'string');
    if (typeof raw === 'string') return [raw];
    return [];
  } catch {
    return [];
  }
}

function useIsAdmin(): boolean {
  const { isAuthenticated } = useAuth();
  return isAuthenticated && getRolesFromToken().includes('Admin');
}

export function CheckinDashboardPage() {
  const isAdmin = useIsAdmin();
  const [autoRefresh, setAutoRefresh] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedRoomIdKey, setSelectedRoomIdKey] = useState('');

  // Fetch all check-in configuration to get areas + locations
  const {
    data: config,
    isLoading: isConfigLoading,
    error: configError,
  } = useCheckinConfiguration();

  // Flatten all locations from all areas
  const allLocations = useMemo<CheckinLocationDto[]>(() => {
    if (!config) return [];
    return config.areas.flatMap((area) => area.locations);
  }, [config]);

  const locationIdKeys = useMemo(
    () => allLocations.map((l) => l.idKey),
    [allLocations]
  );

  // Fetch rosters for all locations
  const {
    data: rosters,
    isLoading: isRostersLoading,
    refetch,
  } = useMultipleRoomRosters(
    locationIdKeys.length > 0 ? locationIdKeys : undefined,
    autoRefresh
  );

  const toggleRoom = useToggleRoomStatus();

  // Build a lookup map: locationIdKey -> RoomRosterDto
  const rosterByLocation = useMemo<Map<string, RoomRosterDto>>(() => {
    const map = new Map<string, RoomRosterDto>();
    if (!rosters) return map;
    for (const r of rosters) {
      map.set(r.locationIdKey, r);
    }
    return map;
  }, [rosters]);

  // Room options for the filter dropdown
  const roomOptions = useMemo(
    () => allLocations.map((l) => ({ idKey: l.idKey, name: l.name })),
    [allLocations]
  );

  // Which locations to display (after room filter)
  const displayedLocations = useMemo(() => {
    if (!selectedRoomIdKey) return allLocations;
    return allLocations.filter((l) => l.idKey === selectedRoomIdKey);
  }, [allLocations, selectedRoomIdKey]);

  const closedCount = allLocations.filter((l) => !l.isActive || !l.isOpen).length;

  const isLoading = isConfigLoading || isRostersLoading;

  const handleToggleRoom = (idKey: string, isActive: boolean) => {
    toggleRoom.mutate({ idKey, isActive });
  };

  // ── Role guard — Admin only ─────────────────────────────────────────────────
  if (!isAdmin) {
    return (
      <div className="flex flex-col items-center justify-center min-h-64 gap-4">
        <div className="bg-red-50 border border-red-200 rounded-lg p-8 max-w-md text-center">
          <svg
            className="w-12 h-12 text-red-400 mx-auto mb-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 15v2m0 0v2m0-2h2m-2 0H10m2-11a7 7 0 100 14 7 7 0 000-14z"
            />
          </svg>
          <p className="text-red-700 font-semibold text-lg">Access Denied</p>
          <p className="text-red-600 text-sm mt-2">
            The check-in dashboard is restricted to Admin users. Contact your
            system administrator if you need access.
          </p>
        </div>
      </div>
    );
  }

  // ── Loading skeleton ────────────────────────────────────────────────────────
  if (isLoading && allLocations.length === 0) {
    return (
      <div className="space-y-6">
        <PageHeader
          autoRefresh={autoRefresh}
          onAutoRefreshChange={setAutoRefresh}
          onRefresh={() => void refetch()}
          isLoading={true}
        />
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div
              key={i}
              className="bg-white rounded-lg shadow border p-4 animate-pulse h-48"
            >
              <div className="h-4 bg-gray-200 rounded w-2/3 mb-2" />
              <div className="h-3 bg-gray-200 rounded w-1/3 mb-4" />
              <div className="space-y-2">
                <div className="h-3 bg-gray-200 rounded" />
                <div className="h-3 bg-gray-200 rounded w-5/6" />
                <div className="h-3 bg-gray-200 rounded w-4/6" />
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  // ── Error state ─────────────────────────────────────────────────────────────
  if (configError) {
    return (
      <div className="space-y-6">
        <PageHeader
          autoRefresh={autoRefresh}
          onAutoRefreshChange={setAutoRefresh}
          onRefresh={() => void refetch()}
          isLoading={false}
        />
        <div className="bg-red-50 border border-red-200 rounded-lg p-6">
          <p className="text-red-700 font-medium">Failed to load check-in configuration</p>
          <p className="text-red-600 text-sm mt-1">
            {configError instanceof Error
              ? configError.message
              : 'An unknown error occurred'}
          </p>
        </div>
      </div>
    );
  }

  // ── Empty state — no areas/locations configured ─────────────────────────────
  if (!isLoading && allLocations.length === 0) {
    return (
      <div className="space-y-6">
        <PageHeader
          autoRefresh={autoRefresh}
          onAutoRefreshChange={setAutoRefresh}
          onRefresh={() => void refetch()}
          isLoading={false}
        />
        <div className="bg-white rounded-lg border border-gray-200 p-12 text-center">
          <svg
            className="w-16 h-16 text-gray-300 mx-auto mb-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
            />
          </svg>
          <p className="text-gray-600 font-medium">No check-in rooms configured</p>
          <p className="text-gray-500 text-sm mt-1">
            Set up check-in areas and locations in Settings first.
          </p>
        </div>
      </div>
    );
  }

  // ── Main dashboard ──────────────────────────────────────────────────────────
  return (
    <div className="space-y-6" data-testid="checkin-dashboard">
      <PageHeader
        autoRefresh={autoRefresh}
        onAutoRefreshChange={setAutoRefresh}
        onRefresh={() => void refetch()}
        isLoading={isLoading}
      />

      {/* Summary stats */}
      <DashboardSummaryBar
        rosters={rosters ?? []}
        locationCount={allLocations.length}
        closedCount={closedCount}
      />

      {/* Search + room filter */}
      <DashboardSearch
        searchQuery={searchQuery}
        onSearchChange={setSearchQuery}
        selectedRoomIdKey={selectedRoomIdKey}
        onRoomChange={setSelectedRoomIdKey}
        roomOptions={roomOptions}
      />

      {/* Room grid */}
      {displayedLocations.length === 0 ? (
        <div className="bg-white rounded-lg border border-gray-200 p-8 text-center">
          <p className="text-gray-500 text-sm">No rooms match the current filter.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {displayedLocations.map((location) => (
            <RoomCard
              key={location.idKey}
              location={location}
              roster={rosterByLocation.get(location.idKey)}
              searchQuery={searchQuery}
              onToggleRoom={handleToggleRoom}
              isToggling={
                toggleRoom.isPending &&
                toggleRoom.variables?.idKey === location.idKey
              }
            />
          ))}
        </div>
      )}
    </div>
  );
}

// ── Sub-component: page header ─────────────────────────────────────────────

interface PageHeaderProps {
  autoRefresh: boolean;
  onAutoRefreshChange: (value: boolean) => void;
  onRefresh: () => void;
  isLoading: boolean;
}

function PageHeader({
  autoRefresh,
  onAutoRefreshChange,
  onRefresh,
  isLoading,
}: PageHeaderProps) {
  return (
    <div className="flex items-start justify-between gap-4 flex-wrap">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Check-in Dashboard</h1>
        <p className="mt-1 text-gray-600">
          Live view of all check-in rooms — auto-refreshes every 30 seconds
        </p>
      </div>

      <div className="flex items-center gap-4">
        {/* Auto-refresh toggle */}
        <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
          <input
            type="checkbox"
            checked={autoRefresh}
            onChange={(e) => onAutoRefreshChange(e.target.checked)}
            className="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
            data-testid="auto-refresh-toggle"
          />
          <span className="flex items-center gap-1">
            {autoRefresh && (
              <span className="inline-block w-2 h-2 rounded-full bg-green-500 animate-pulse" />
            )}
            Auto-refresh
          </span>
        </label>

        {/* Manual refresh */}
        <Button
          variant="secondary"
          onClick={onRefresh}
          disabled={isLoading}
          data-testid="refresh-button"
        >
          <svg
            className="w-4 h-4 mr-1.5"
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
          Refresh Now
        </Button>
      </div>
    </div>
  );
}
