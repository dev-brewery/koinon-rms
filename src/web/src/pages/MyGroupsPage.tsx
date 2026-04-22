/**
 * My Groups Page
 * Dashboard for group leaders to manage their groups and members
 */

import { useState } from 'react';
import { Card } from '@/components/ui/Card';
import { Loading, EmptyState, ErrorState } from '@/components/ui';
import { GroupMemberList } from '@/components/mygroups/GroupMemberList';
import { TakeAttendanceModal } from '@/components/mygroups/TakeAttendanceModal';
import { GroupCardWithRequests } from '@/components/mygroups/GroupCardWithRequests';
import { getErrorMessage } from '@/lib/errorMessages';
import {
  useMyGroups,
  useMyGroupMembers,
  useUpdateGroupMember,
  useRemoveGroupMember,
  useRecordAttendance,
} from '@/hooks/useMyGroups';

export function MyGroupsPage() {
  const [expandedGroupId, setExpandedGroupId] = useState<string | null>(null);
  const [attendanceGroupId, setAttendanceGroupId] = useState<string | null>(null);

  const { data: groups = [], isLoading, error, refetch } = useMyGroups();

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

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <Loading text="Loading your groups..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <ErrorState
          title="Failed to load your groups"
          message={getErrorMessage(error).message}
          onRetry={() => refetch()}
        />
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
            <EmptyState
              title="You don't lead any groups yet"
              description="When you're assigned as a leader of a group, it will appear here so you can manage members and record attendance."
            />
          </Card>
        ) : (
          <div className="space-y-4">
            {groups.map((group) => (
              <Card key={group.idKey}>
                <GroupCardWithRequests
                  group={group}
                  onTakeAttendance={() => handleTakeAttendance(group.idKey)}
                  onToggleMembers={() => handleToggleGroup(group.idKey)}
                  isMembersExpanded={expandedGroupId === group.idKey}
                >
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
                </GroupCardWithRequests>
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
