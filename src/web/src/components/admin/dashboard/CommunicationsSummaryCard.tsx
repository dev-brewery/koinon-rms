/**
 * Communications Summary Card Component
 * Displays pending and sent communications counts
 */

export interface CommunicationsSummaryCardProps {
  pendingCount: number;
  sentThisWeekCount: number;
}

export function CommunicationsSummaryCard({
  pendingCount,
  sentThisWeekCount,
}: CommunicationsSummaryCardProps) {
  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6" data-testid="communications-summary-card">
      <div className="flex items-center justify-between">
        <div className="flex-1">
          <p className="text-sm font-medium text-gray-600">Communications</p>
          <div className="mt-3 space-y-2">
            <div>
              <p className="text-xs text-gray-500">Pending</p>
              <p className="text-2xl font-bold text-gray-900">{pendingCount}</p>
            </div>
            <div>
              <p className="text-xs text-gray-500">Sent This Week</p>
              <p className="text-2xl font-bold text-gray-900">{sentThisWeekCount}</p>
            </div>
          </div>
        </div>
        <div className="p-3 rounded-lg bg-purple-50 text-purple-600">
          <svg
            className="w-8 h-8"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
            />
          </svg>
        </div>
      </div>
    </div>
  );
}
