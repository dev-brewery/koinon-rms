import type { CheckinOperationsSummaryDto, CheckinOperationsRoomDto } from '@/services/api/checkinOperations';

interface SummaryStatsProps {
  summary: CheckinOperationsSummaryDto;
  rooms: CheckinOperationsRoomDto[];
}

export function SummaryStats({ summary, rooms }: SummaryStatsProps) {
  const roomsOpen = rooms.filter((r) => r.isOpen).length;
  const atCapacity = rooms.filter((r) => r.capacityPillColor === 'red' || r.capacityPillColor === 'yellow').length;
  const roomsClosed = rooms.filter((r) => !r.isOpen).length;

  return (
    <div
      data-testid="checkin-ops-summary"
      className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm"
    >
      <div className="grid grid-cols-3 gap-4">
        <div className="text-center">
          <p className="text-xs font-medium uppercase tracking-wide text-gray-500">
            Total checked in
          </p>
          <p
            className="mt-1 text-3xl font-bold text-gray-900"
            data-testid="checkin-ops-summary-total"
          >
            {summary.totalCheckedIn}
          </p>
        </div>
        <div className="text-center">
          <p className="text-xs font-medium uppercase tracking-wide text-gray-500">
            Currently present
          </p>
          <p
            className="mt-1 text-3xl font-bold text-indigo-600"
            data-testid="checkin-ops-summary-present"
          >
            {summary.currentlyPresent}
          </p>
        </div>
        <div className="text-center">
          <p className="text-xs font-medium uppercase tracking-wide text-gray-500">
            Checked out
          </p>
          <p
            className="mt-1 text-3xl font-bold text-gray-500"
            data-testid="checkin-ops-summary-checkedout"
          >
            {summary.checkedOut}
          </p>
        </div>
      </div>

      <p
        data-testid="checkin-ops-rooms-status"
        className="mt-3 border-t border-gray-100 pt-2 text-center text-xs text-gray-500"
      >
        Rooms: <span className="font-medium text-gray-700">{roomsOpen} open</span>
        {' · '}
        <span className="font-medium text-amber-700">{atCapacity} near/at capacity</span>
        {' · '}
        <span className="font-medium text-gray-700">{roomsClosed} closed</span>
      </p>
    </div>
  );
}
