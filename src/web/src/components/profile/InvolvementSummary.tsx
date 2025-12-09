/**
 * InvolvementSummary
 * Display user's group memberships and attendance summary
 */

import type { MyInvolvementDto } from '@/types/profile';

interface InvolvementSummaryProps {
  involvement: MyInvolvementDto;
}

export function InvolvementSummary({ involvement }: InvolvementSummaryProps) {
  const formatDate = (dateValue: string | Date | undefined | null): string => {
    if (!dateValue) return 'N/A';
    const date = typeof dateValue === 'string' ? new Date(dateValue) : dateValue;
    if (isNaN(date.getTime())) return 'N/A';
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  return (
    <div className="space-y-6">
      {/* Attendance Summary */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Attendance Summary</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="bg-primary-50 rounded-lg p-4">
            <div className="text-3xl font-bold text-primary-700">
              {involvement.recentAttendanceCount}
            </div>
            <div className="text-sm text-gray-600 mt-1">Check-ins (Last 30 days)</div>
          </div>

          <div className="bg-gray-50 rounded-lg p-4">
            <div className="text-3xl font-bold text-gray-900">
              {involvement.totalGroupsCount}
            </div>
            <div className="text-sm text-gray-600 mt-1">Total Groups</div>
          </div>
        </div>
      </div>

      {/* Groups */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Group Memberships</h3>

        {involvement.groups.length === 0 ? (
          <div className="text-center py-8">
            <svg
              className="w-12 h-12 text-gray-400 mx-auto mb-3"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
              />
            </svg>
            <p className="text-gray-500">You're not a member of any groups yet</p>
            <a
              href="/groups"
              className="inline-block mt-3 text-sm text-primary-600 hover:text-primary-700 font-medium"
            >
              Browse Groups
            </a>
          </div>
        ) : (
          <div className="space-y-3">
            {involvement.groups.map((group) => (
              <div
                key={group.idKey}
                className="border border-gray-200 rounded-lg p-4 hover:border-primary-300 transition-colors"
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="flex-1 min-w-0">
                    <h4 className="text-base font-semibold text-gray-900 mb-1">
                      {group.groupName}
                    </h4>
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800">
                        {group.groupTypeName}
                      </span>
                      <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-800">
                        {group.role}
                      </span>
                    </div>
                  </div>

                  <div className="text-right text-sm text-gray-600">
                    <div className="text-xs text-gray-500">Joined</div>
                    <div>{formatDate(group.joinedDate)}</div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
