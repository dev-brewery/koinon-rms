/**
 * CommunicationStatisticsCard Component
 * Displays delivery, open, and failure statistics for communication messages
 * with visual indicators and percentage breakdowns
 */

export interface CommunicationStatisticsCardProps {
  recipientCount: number;
  deliveredCount: number;
  failedCount: number;
  openedCount: number;
}

export function CommunicationStatisticsCard({
  recipientCount,
  deliveredCount,
  failedCount,
  openedCount,
}: CommunicationStatisticsCardProps) {
  // Calculate pending count
  const pendingCount = recipientCount - deliveredCount - failedCount;

  // Calculate percentages
  const deliveredPercent = recipientCount > 0 ? (deliveredCount / recipientCount) * 100 : 0;
  const openedPercent = deliveredCount > 0 ? (openedCount / deliveredCount) * 100 : 0;
  const failedPercent = recipientCount > 0 ? (failedCount / recipientCount) * 100 : 0;
  const pendingPercent = recipientCount > 0 ? (pendingCount / recipientCount) * 100 : 0;

  // Format percentage
  const formatPercent = (value: number): string => {
    return `${value.toFixed(1)}%`;
  };

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      {/* Header */}
      <h3 className="text-lg font-semibold text-gray-900 mb-6">Delivery Statistics</h3>

      {/* Total Recipients */}
      <div className="mb-6">
        <div className="flex justify-between items-center mb-2">
          <span className="text-sm font-medium text-gray-700">Total Recipients</span>
          <span className="text-2xl font-bold text-gray-900">{recipientCount}</span>
        </div>
      </div>

      {/* Divider */}
      <div className="border-t border-gray-200 my-4" />

      {/* Statistics */}
      <div className="space-y-4">
        {/* Delivered */}
        <div>
          <div className="flex justify-between items-center mb-2">
            <span className="text-sm font-medium text-gray-700">Delivered</span>
            <div className="text-right">
              <span className="text-sm font-semibold text-green-600">{deliveredCount}</span>
              <span className="text-xs text-gray-500 ml-2">({formatPercent(deliveredPercent)})</span>
            </div>
          </div>
          <div className="w-full bg-gray-200 rounded-full h-2">
            <div
              className="bg-green-600 h-2 rounded-full transition-all duration-300"
              style={{ width: `${deliveredPercent}%` }}
              role="progressbar"
              aria-valuenow={deliveredPercent}
              aria-valuemin={0}
              aria-valuemax={100}
              aria-label={`${formatPercent(deliveredPercent)} delivered`}
            />
          </div>
        </div>

        {/* Opened (only shown if there are delivered messages) */}
        {deliveredCount > 0 && (
          <div>
            <div className="flex justify-between items-center mb-2">
              <span className="text-sm font-medium text-gray-700">Opened</span>
              <div className="text-right">
                <span className="text-sm font-semibold text-green-700">{openedCount}</span>
                <span className="text-xs text-gray-500 ml-2">({formatPercent(openedPercent)})</span>
              </div>
            </div>
            <div className="w-full bg-gray-200 rounded-full h-2">
              <div
                className="bg-green-700 h-2 rounded-full transition-all duration-300"
                style={{ width: `${openedPercent}%` }}
                role="progressbar"
                aria-valuenow={openedPercent}
                aria-valuemin={0}
                aria-valuemax={100}
                aria-label={`${formatPercent(openedPercent)} opened`}
              />
            </div>
          </div>
        )}

        {/* Failed */}
        {failedCount > 0 && (
          <div>
            <div className="flex justify-between items-center mb-2">
              <span className="text-sm font-medium text-gray-700">Failed</span>
              <div className="text-right">
                <span className="text-sm font-semibold text-red-600">{failedCount}</span>
                <span className="text-xs text-gray-500 ml-2">({formatPercent(failedPercent)})</span>
              </div>
            </div>
            <div className="w-full bg-gray-200 rounded-full h-2">
              <div
                className="bg-red-600 h-2 rounded-full transition-all duration-300"
                style={{ width: `${failedPercent}%` }}
                role="progressbar"
                aria-valuenow={failedPercent}
                aria-valuemin={0}
                aria-valuemax={100}
                aria-label={`${formatPercent(failedPercent)} failed`}
              />
            </div>
          </div>
        )}

        {/* Pending */}
        {pendingCount > 0 && (
          <div>
            <div className="flex justify-between items-center mb-2">
              <span className="text-sm font-medium text-gray-700">Pending</span>
              <div className="text-right">
                <span className="text-sm font-semibold text-gray-600">{pendingCount}</span>
                <span className="text-xs text-gray-500 ml-2">({formatPercent(pendingPercent)})</span>
              </div>
            </div>
            <div className="w-full bg-gray-200 rounded-full h-2">
              <div
                className="bg-gray-400 h-2 rounded-full transition-all duration-300"
                style={{ width: `${pendingPercent}%` }}
                role="progressbar"
                aria-valuenow={pendingPercent}
                aria-valuemin={0}
                aria-valuemax={100}
                aria-label={`${formatPercent(pendingPercent)} pending`}
              />
            </div>
          </div>
        )}
      </div>

      {/* Summary Indicator */}
      {recipientCount > 0 && (
        <>
          <div className="border-t border-gray-200 my-4" />
          <div className="flex items-center gap-2">
            {failedCount === 0 && deliveredCount === recipientCount ? (
              <>
                <svg
                  className="w-5 h-5 text-green-600"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M5 13l4 4L19 7"
                  />
                </svg>
                <span className="text-sm font-medium text-green-600">All messages delivered</span>
              </>
            ) : failedCount > 0 ? (
              <>
                <svg
                  className="w-5 h-5 text-amber-600"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                  />
                </svg>
                <span className="text-sm font-medium text-amber-600">Some messages failed</span>
              </>
            ) : pendingCount > 0 ? (
              <>
                <svg
                  className="w-5 h-5 text-gray-500"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
                  />
                </svg>
                <span className="text-sm font-medium text-gray-600">Delivery in progress</span>
              </>
            ) : null}
          </div>
        </>
      )}
    </div>
  );
}
