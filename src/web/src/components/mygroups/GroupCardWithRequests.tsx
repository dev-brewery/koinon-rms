/**
 * Group Card with Pending Requests
 * Displays a group card in MyGroups with pending requests support
 */

import { useState } from 'react';
import { Button } from '@/components/ui/Button';
import { PendingRequestsBadge } from '@/components/groups/PendingRequestsBadge';
import { PendingRequestsList } from '@/components/groups/PendingRequestsList';
import { usePendingRequests } from '@/hooks/useMembershipRequests';
import { formatDate } from '@/lib/utils';
import type { MyGroupDto } from '@/services/api/types';

interface GroupCardWithRequestsProps {
  group: MyGroupDto;
  children?: React.ReactNode;
  onTakeAttendance: () => void;
  onToggleMembers: () => void;
  isMembersExpanded: boolean;
}

export function GroupCardWithRequests({
  group,
  children,
  onTakeAttendance,
  onToggleMembers,
  isMembersExpanded,
}: GroupCardWithRequestsProps) {
  const [showRequests, setShowRequests] = useState(false);
  const { data: requests = [] } = usePendingRequests(group.idKey);

  const getCapacityColor = () => {
    if (!group.groupCapacity) return 'text-gray-600';
    const percentage = (group.memberCount / group.groupCapacity) * 100;
    if (percentage >= 90) return 'text-red-600';
    if (percentage >= 75) return 'text-yellow-600';
    return 'text-green-600';
  };

  return (
    <div>
      {/* Group Header */}
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-2">
            <h2 className="text-xl font-semibold text-gray-900">
              {group.name}
            </h2>
            {requests.length > 0 && (
              <PendingRequestsBadge count={requests.length} />
            )}
          </div>
          {group.description && (
            <p className="mt-1 text-sm text-gray-600">{group.description}</p>
          )}

          <div className="mt-3 flex flex-wrap gap-4 text-sm">
            <div className="flex items-center gap-2">
              <svg
                className="w-5 h-5 text-gray-400"
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
              <span className={getCapacityColor()}>
                {group.memberCount}
                {group.groupCapacity && ` / ${group.groupCapacity}`} members
              </span>
            </div>

            {group.lastMeetingDate && (
              <div className="flex items-center gap-2">
                <svg
                  className="w-5 h-5 text-gray-400"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
                  />
                </svg>
                <span className="text-gray-600">
                  Last met: {formatDate(group.lastMeetingDate)}
                </span>
              </div>
            )}
          </div>
        </div>

        <div className="flex gap-2 ml-4">
          <Button
            size="sm"
            variant="outline"
            onClick={onTakeAttendance}
          >
            Take Attendance
          </Button>
          <Button
            size="sm"
            variant="ghost"
            onClick={onToggleMembers}
          >
            {isMembersExpanded ? 'Hide' : 'View'} Members
          </Button>
        </div>
      </div>

      {/* Pending Requests Section */}
      {requests.length > 0 && (
        <div className="mt-4 pt-4 border-t border-gray-200">
          <button
            onClick={() => setShowRequests(!showRequests)}
            className="flex items-center justify-between w-full text-left group"
          >
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-medium text-gray-900">
                Pending Requests
              </h3>
              <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-red-100 text-red-800">
                {requests.length}
              </span>
            </div>
            <svg
              className={`w-5 h-5 text-gray-400 transition-transform ${showRequests ? 'rotate-180' : ''}`}
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M19 9l-7 7-7-7"
              />
            </svg>
          </button>

          {showRequests && (
            <div className="mt-3">
              <PendingRequestsList groupIdKey={group.idKey} />
            </div>
          )}
        </div>
      )}

      {/* Expanded Members List */}
      {children}
    </div>
  );
}
