/**
 * GroupMembershipsSection Component
 * Displays a person's group memberships in a table layout.
 */

import { Link } from 'react-router-dom';
import type { GroupMembershipDto, GroupMemberStatus } from '@/services/api/types';

interface GroupMembershipsSectionProps {
  memberships: GroupMembershipDto[];
  isLoading: boolean;
}

function StatusBadge({ status }: { status: GroupMemberStatus }) {
  const styles: Record<GroupMemberStatus, string> = {
    Active: 'bg-green-100 text-green-800',
    Inactive: 'bg-gray-100 text-gray-700',
    Pending: 'bg-yellow-100 text-yellow-800',
  };

  return (
    <span className={`inline-flex items-center px-2 py-0.5 text-xs font-medium rounded-full ${styles[status]}`}>
      {status}
    </span>
  );
}

function formatJoinDate(dateAdded?: string): string {
  if (!dateAdded) return '\u2014';
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(dateAdded));
}

export function GroupMembershipsSection({ memberships, isLoading }: GroupMembershipsSectionProps) {
  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <h2 className="text-lg font-semibold text-gray-900 mb-4">Groups</h2>

      {isLoading ? (
        <div className="flex items-center justify-center py-8">
          <div className="w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
        </div>
      ) : memberships.length === 0 ? (
        <p className="text-sm text-gray-500">Not a member of any groups.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-200">
                <th className="text-left font-medium text-gray-700 pb-2 pr-4">Group</th>
                <th className="text-left font-medium text-gray-700 pb-2 pr-4">Type</th>
                <th className="text-left font-medium text-gray-700 pb-2 pr-4">Role</th>
                <th className="text-left font-medium text-gray-700 pb-2 pr-4">Status</th>
                <th className="text-left font-medium text-gray-700 pb-2">Joined</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {memberships.map((membership) => (
                <tr key={membership.group.idKey} className="hover:bg-gray-50">
                  <td className="py-3 pr-4">
                    <Link
                      to={`/admin/groups/${membership.group.idKey}`}
                      className="text-primary-600 hover:text-primary-700 font-medium"
                    >
                      {membership.group.name}
                    </Link>
                  </td>
                  <td className="py-3 pr-4 text-gray-600">{membership.group.groupTypeName}</td>
                  <td className="py-3 pr-4 text-gray-600">{membership.role.name}</td>
                  <td className="py-3 pr-4"><StatusBadge status={membership.status} /></td>
                  <td className="py-3 text-gray-600">{formatJoinDate(membership.dateAdded)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
