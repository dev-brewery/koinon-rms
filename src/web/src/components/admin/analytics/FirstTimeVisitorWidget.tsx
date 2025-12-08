/**
 * First-Time Visitor Widget Component
 * Dashboard widget showing today's first-time visitors
 */

import { useTodaysFirstTimeVisitors } from '@/hooks/useAnalytics';

export interface FirstTimeVisitorWidgetProps {
  campusIdKey?: string;
  onViewAll?: () => void;
}

export function FirstTimeVisitorWidget({
  campusIdKey,
  onViewAll,
}: FirstTimeVisitorWidgetProps) {
  const { data: visitors, isLoading, error } = useTodaysFirstTimeVisitors(campusIdKey);

  const formatTime = (dateTime: string): string => {
    const date = new Date(dateTime);
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  };

  if (error) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">
          First-Time Visitors Today
        </h3>
        <div className="flex items-center justify-center py-8">
          <div className="text-center text-red-600">
            <svg
              className="w-12 h-12 mx-auto mb-2 text-red-400"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            <p className="text-sm">Failed to load visitor data</p>
          </div>
        </div>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">
          First-Time Visitors Today
        </h3>
        <div className="flex items-center justify-center py-8">
          <div className="text-center">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
            <p className="mt-2 text-sm text-gray-500">Loading visitors...</p>
          </div>
        </div>
      </div>
    );
  }

  const visitorCount = visitors?.length || 0;
  const recentVisitors = visitors?.slice(0, 5) || [];

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      {/* Header with count */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-3">
          <div className="p-3 rounded-lg bg-purple-50 text-purple-600">
            <svg
              className="w-6 h-6"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z"
              />
            </svg>
          </div>
          <div>
            <h3 className="text-lg font-semibold text-gray-900">
              First-Time Visitors Today
            </h3>
            <p className="text-2xl font-bold text-purple-600">{visitorCount}</p>
          </div>
        </div>
      </div>

      {/* Visitor list or empty state */}
      {visitorCount === 0 ? (
        <div className="flex items-center justify-center py-8">
          <div className="text-center text-gray-500">
            <svg
              className="w-16 h-16 mx-auto mb-4 text-gray-300"
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
            <p>No first-time visitors yet today</p>
          </div>
        </div>
      ) : (
        <>
          {/* Recent visitors list */}
          <div className="space-y-3">
            {recentVisitors.map((visitor) => (
              <div
                key={visitor.personIdKey}
                className="flex items-start gap-3 p-3 rounded-lg hover:bg-gray-50 transition-colors"
              >
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <p className="text-sm font-medium text-gray-900 truncate">
                      {visitor.personName}
                    </p>
                    {visitor.hasFollowUp && (
                      <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800">
                        Follow-up
                      </span>
                    )}
                  </div>
                  <p className="text-xs text-gray-500 truncate">{visitor.groupName}</p>
                  <div className="flex items-center gap-2 mt-1">
                    <svg
                      className="w-3 h-3 text-gray-400"
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
                    <span className="text-xs text-gray-500">
                      {formatTime(visitor.checkInDateTime)}
                    </span>
                    {visitor.campusName && (
                      <>
                        <span className="text-gray-300">•</span>
                        <span className="text-xs text-gray-500">
                          {visitor.campusName}
                        </span>
                      </>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* View All button */}
          {onViewAll && visitorCount > 5 && (
            <div className="mt-4 pt-4 border-t border-gray-200">
              <button
                type="button"
                onClick={onViewAll}
                className="w-full px-4 py-2 text-sm font-medium text-blue-600 hover:text-blue-700 hover:bg-blue-50 rounded-md transition-colors"
              >
                View All {visitorCount} Visitors
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
