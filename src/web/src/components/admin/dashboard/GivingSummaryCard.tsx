/**
 * Giving Summary Card Component
 * Displays month-to-date and year-to-date giving totals
 */

export interface GivingSummaryCardProps {
  monthToDateTotal: number;
  yearToDateTotal: number;
}

export function GivingSummaryCard({
  monthToDateTotal,
  yearToDateTotal,
}: GivingSummaryCardProps) {
  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(amount);
  };

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6" data-testid="giving-summary-card">
      <div className="flex items-center justify-between">
        <div className="flex-1">
          <p className="text-sm font-medium text-gray-600">Giving Summary</p>
          <div className="mt-3 space-y-2">
            <div>
              <p className="text-xs text-gray-500">Month to Date</p>
              <p className="text-2xl font-bold text-gray-900">{formatCurrency(monthToDateTotal)}</p>
            </div>
            <div>
              <p className="text-xs text-gray-500">Year to Date</p>
              <p className="text-2xl font-bold text-gray-900">{formatCurrency(yearToDateTotal)}</p>
            </div>
          </div>
        </div>
        <div className="p-3 rounded-lg bg-green-50 text-green-600">
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
              d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
        </div>
      </div>
    </div>
  );
}
