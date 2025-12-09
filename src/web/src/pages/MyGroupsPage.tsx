/**
 * My Groups Page
 * Dashboard for group leaders to manage their groups and members
 */

import { useState } from 'react';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { GroupMemberList } from '@/components/mygroups/GroupMemberList';
import { TakeAttendanceModal } from '@/components/mygroups/TakeAttendanceModal';
import {
  useMyGroups,
  useMyGroupMembers,
  useUpdateGroupMember,
  useRemoveGroupMember,
  useRecordAttendance,
} from '@/hooks/useMyGroups';
import { formatDate } from '@/lib/utils';
import type { MyGroupDto } from '@/services/api/types';

export function MyGroupsPage() {
  const [expandedGroupId, setExpandedGroupId] = useState<string | null>(null);
  const [attendanceGroupId, setAttendanceGroupId] = useState<string | null>(null);

  const { data: groups = [], isLoading, error } = useMyGroups();

  const { data: members = [] } = useMyGroupMembers(expandedGroupId || undefined);

  const updateMemberMutation = useUpdateGroupMember(expandedGroupId || '');
  const removeMemberMutation = useRemoveGroupMember(expandedGroupId || '');
  const recordAttendanceMutation = useRecordAttendance(attendanceGroupId || '');

  const handleToggleGroup = (groupIdKey: string) => {
    setExpandedGroupId((prev) => (prev === groupIdKey ? null : groupIdKey));
  };

  const handleTakeAttendance = (groupIdKey: string) => {
    setAttendanceGroupId(groupIdKey);
  };

  const handleSubmitAttendance = async (
    occurrenceDate: string,
    attendedPersonIds: string[]
  ) => {
    await recordAttendanceMutation.mutateAsync({
      occurrenceDate,
      attendedPersonIds,
    });
    setAttendanceGroupId(null);
  };

  const handleUpdateMember = async (
    memberIdKey: string,
    data: {
      roleId?: string;
      status?: string;
      note?: string;
    }
  ) => {
    await updateMemberMutation.mutateAsync({ memberIdKey, data });
  };

  const handleRemoveMember = async (memberIdKey: string) => {
    await removeMemberMutation.mutateAsync(memberIdKey);
  };

  const getCapacityColor = (group: MyGroupDto) => {
    if (!group.groupCapacity) return 'text-gray-600';
    const percentage = (group.memberCount / group.groupCapacity) * 100;
    if (percentage >= 90) return 'text-red-600';
    if (percentage >= 75) return 'text-yellow-600';
    return 'text-green-600';
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
          <p className="text-gray-600">Loading your groups...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <p className="text-red-600 mb-4">Failed to load groups</p>
          <Button onClick={() => window.location.reload()}>Retry</Button>
        </div>
      </div>
    );
  }

  const attendanceMembers = attendanceGroupId === expandedGroupId ? members : [];

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900">My Groups</h1>
          <p className="mt-2 text-gray-600">
            Manage your groups and track member attendance
          </p>
        </div>

        {/* Groups List */}
        {groups.length === 0 ? (
          <Card>
            <div className="text-center py-12">
              <svg
                className="mx-auto h-12 w-12 text-gray-400"
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
              <h3 className="mt-2 text-sm font-medium text-gray-900">No groups</h3>
              <p className="mt-1 text-sm text-gray-500">
                You are not currently a leader of any groups.
              </p>
            </div>
          </Card>
        ) : (
          <div className="space-y-4">
            {groups.map((group) => (
              <Card key={group.idKey}>
                <div>
                  {/* Group Header */}
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <h2 className="text-xl font-semibold text-gray-900">
                        {group.name}
                      </h2>
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
                          <span className={getCapacityColor(group)}>
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
                        onClick={() => handleTakeAttendance(group.idKey)}
                      >
                        Take Attendance
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => handleToggleGroup(group.idKey)}
                      >
                        {expandedGroupId === group.idKey ? 'Hide' : 'View'} Members
                      </Button>
                    </div>
                  </div>

                  {/* Expanded Members List */}
                  {expandedGroupId === group.idKey && (
                    <div className="mt-6 pt-6 border-t border-gray-200">
                      <h3 className="text-lg font-medium text-gray-900 mb-4">
                        Group Members
                      </h3>
                      <GroupMemberList
                        members={members}
                        onUpdateMember={handleUpdateMember}
                        onRemoveMember={handleRemoveMember}
                        isUpdating={updateMemberMutation.isPending}
                        isRemoving={removeMemberMutation.isPending}
                      />
                    </div>
                  )}
                </div>
              </Card>
            ))}
          </div>
        )}
      </div>

      {/* Take Attendance Modal */}
      <TakeAttendanceModal
        isOpen={!!attendanceGroupId}
        onClose={() => setAttendanceGroupId(null)}
        members={attendanceMembers}
        onSubmit={handleSubmitAttendance}
        isSubmitting={recordAttendanceMutation.isPending}
      />
    </div>
  );
}
