/**
 * Group Member List Component
 * Displays and manages members of a group
 */

import { useState } from 'react';
import { EditMemberModal } from './EditMemberModal';
import { formatDate } from '@/lib/utils';
import type { MyGroupMemberDetailDto } from '@/services/api/types';

interface GroupMemberListProps {
  members: MyGroupMemberDetailDto[];
  onUpdateMember: (
    memberIdKey: string,
    data: {
      roleId?: string;
      status?: string;
      note?: string;
    }
  ) => Promise<void>;
  onRemoveMember: (memberIdKey: string) => Promise<void>;
  isUpdating: boolean;
  isRemoving: boolean;
}

export function GroupMemberList({
  members,
  onUpdateMember,
  onRemoveMember,
  isUpdating,
  isRemoving,
}: GroupMemberListProps) {
  const [editingMember, setEditingMember] = useState<MyGroupMemberDetailDto | null>(null);
  const [removingMemberId, setRemovingMemberId] = useState<string | null>(null);

  const handleEditClick = (member: MyGroupMemberDetailDto) => {
    setEditingMember(member);
  };

  const handleRemoveClick = async (member: MyGroupMemberDetailDto) => {
    if (
      window.confirm(
        `Are you sure you want to remove ${member.firstName} ${member.lastName} from this group?`
      )
    ) {
      setRemovingMemberId(member.idKey);
      try {
        await onRemoveMember(member.idKey);
      } finally {
        setRemovingMemberId(null);
      }
    }
  };

  const handleSaveEdit = async (data: {
    roleId?: string;
    status?: string;
    note?: string;
  }) => {
    if (!editingMember) return;

    await onUpdateMember(editingMember.idKey, data);
    setEditingMember(null);
  };

  const getStatusBadgeClass = (status: string) => {
    switch (status) {
      case 'Active':
        return 'bg-green-100 text-green-800';
      case 'Inactive':
        return 'bg-gray-100 text-gray-800';
      case 'Pending':
        return 'bg-yellow-100 text-yellow-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  if (members.length === 0) {
    return (
      <div className="text-center py-12 bg-gray-50 rounded-lg">
        <p className="text-gray-500">No members in this group yet.</p>
      </div>
    );
  }

  return (
    <>
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Name
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Email
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Phone
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Role
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Status
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Joined
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {members.map((member) => (
              <tr key={member.idKey} className="hover:bg-gray-50">
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm font-medium text-gray-900">
                    {member.firstName} {member.lastName}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm text-gray-500">{member.email || '-'}</div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm text-gray-500">{member.phone || '-'}</div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  {member.role.isLeader && (
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                      {member.role.name}
                    </span>
                  )}
                  {!member.role.isLeader && (
                    <span className="text-sm text-gray-500">{member.role.name}</span>
                  )}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span
                    className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getStatusBadgeClass(
                      member.status
                    )}`}
                  >
                    {member.status}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  {member.dateTimeAdded ? formatDate(member.dateTimeAdded) : '-'}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                  <div className="flex justify-end gap-2">
                    <button
                      onClick={() => handleEditClick(member)}
                      disabled={isUpdating || isRemoving}
                      className="text-blue-600 hover:text-blue-900 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => handleRemoveClick(member)}
                      disabled={isRemoving || removingMemberId === member.idKey}
                      className="text-red-600 hover:text-red-900 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {removingMemberId === member.idKey ? 'Removing...' : 'Remove'}
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <EditMemberModal
        isOpen={!!editingMember}
        onClose={() => setEditingMember(null)}
        member={editingMember}
        onSave={handleSaveEdit}
        isSaving={isUpdating}
      />
    </>
  );
}
