/**
 * DashboardSummaryBar
 * Stats row at the top of the check-in dashboard showing aggregate counts
 */

import type { RoomRosterDto } from '@/services/api/types';

interface DashboardSummaryBarProps {
  rosters: RoomRosterDto[];
  locationCount: number;
  closedCount: number;
}

interface StatTileProps {
  label: string;
  value: number;
  colorClass: string;
}

function StatTile({ label, value, colorClass }: StatTileProps) {
  return (
    <div className="bg-white rounded-lg border border-gray-200 shadow-sm px-5 py-4 flex flex-col gap-1 min-w-0">
      <span className={`text-3xl font-bold leading-none ${colorClass}`}>{value}</span>
      <span className="text-sm text-gray-500 truncate">{label}</span>
    </div>
  );
}

export function DashboardSummaryBar({
  rosters,
  locationCount,
  closedCount,
}: DashboardSummaryBarProps) {
  const totalCheckedIn = rosters.reduce((sum, r) => sum + r.totalCount, 0);
  const openRooms = locationCount - closedCount;
  const atCapacity = rosters.filter((r) => r.isAtCapacity).length;

  return (
    <div
      className="grid grid-cols-2 sm:grid-cols-4 gap-3"
      data-testid="dashboard-summary-bar"
    >
      <StatTile
        label="Total Checked In"
        value={totalCheckedIn}
        colorClass="text-indigo-700"
      />
      <StatTile
        label="Rooms Open"
        value={openRooms}
        colorClass="text-green-700"
      />
      <StatTile
        label="At Capacity"
        value={atCapacity}
        colorClass={atCapacity > 0 ? 'text-red-700' : 'text-gray-700'}
      />
      <StatTile
        label="Rooms Closed"
        value={closedCount}
        colorClass={closedCount > 0 ? 'text-yellow-700' : 'text-gray-700'}
      />
    </div>
  );
}
