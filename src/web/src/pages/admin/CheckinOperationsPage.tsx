/**
 * Live Check-in Operations Dashboard (#482).
 *
 * Coordinator-facing page that shows a real-time view of each check-in room,
 * currently checked-in attendees, and today's running totals. Polls the
 * /checkin-operations/dashboard endpoint every 5 seconds via React Query.
 */

import { useState } from 'react';
import { useCheckinOperationsDashboard, useToggleCheckinOperationsRoom } from '@/hooks/useCheckinOperations';
import { SummaryStats } from '@/components/admin/checkin-ops/SummaryStats';
import { RoomList } from '@/components/admin/checkin-ops/RoomList';
import { AttendeeSearch } from '@/components/admin/checkin-ops/AttendeeSearch';

export function CheckinOperationsPage() {
  const { data, isLoading, isError, error, dataUpdatedAt } = useCheckinOperationsDashboard();
  const toggleMutation = useToggleCheckinOperationsRoom();
  const [togglingLocationIdKey, setTogglingLocationIdKey] = useState<string | undefined>();

  const handleToggle = (locationIdKey: string) => {
    setTogglingLocationIdKey(locationIdKey);
    toggleMutation.mutate(locationIdKey, {
      onSettled: () => setTogglingLocationIdKey(undefined),
    });
  };

  const lastUpdated = dataUpdatedAt
    ? new Date(dataUpdatedAt).toLocaleTimeString([], {
        hour: 'numeric',
        minute: '2-digit',
        second: '2-digit',
      })
    : '—';

  return (
    <div data-testid="checkin-ops-page" className="space-y-6">
      <header className="flex flex-col gap-1">
        <h1 className="text-3xl font-bold text-gray-900">Check-in Operations</h1>
        <p className="text-sm text-gray-600">
          Live view of rooms and check-ins. Updates every 5 seconds.
          <span className="ml-2 text-xs text-gray-400" data-testid="checkin-ops-last-updated">
            Last updated: {lastUpdated}
          </span>
        </p>
      </header>

      {isLoading && !data ? (
        <div
          data-testid="checkin-ops-loading"
          className="rounded-lg border border-gray-200 bg-white p-8 text-center text-sm text-gray-500"
        >
          Loading live dashboard...
        </div>
      ) : isError ? (
        <div
          data-testid="checkin-ops-error"
          className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700"
        >
          Failed to load dashboard.{' '}
          {error instanceof Error ? error.message : 'Please try again.'}
        </div>
      ) : data ? (
        <>
          <SummaryStats summary={data.summary} />
          <section>
            <h2 className="mb-3 text-lg font-semibold text-gray-900">Rooms</h2>
            <RoomList
              rooms={data.rooms}
              onToggle={handleToggle}
              togglingLocationIdKey={togglingLocationIdKey}
            />
          </section>
          <AttendeeSearch attendees={data.attendees} />
        </>
      ) : null}
    </div>
  );
}
