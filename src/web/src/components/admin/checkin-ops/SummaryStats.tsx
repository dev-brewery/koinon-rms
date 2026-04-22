import type { CheckinOperationsSummaryDto } from '@/services/api/checkinOperations';

interface SummaryStatsProps {
  summary: CheckinOperationsSummaryDto;
}

export function SummaryStats({ summary }: SummaryStatsProps) {
  return (
    <div
      data-testid="checkin-ops-summary"
      className="grid grid-cols-3 gap-4 rounded-lg border border-gray-200 bg-white p-4 shadow-sm"
    >
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
  );
}
