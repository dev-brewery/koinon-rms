/**
 * GroupTypeCard Component
 * Displays a group type summary in a card layout
 */

import type { GroupTypeAdminDto } from '@/services/api/types';

interface GroupTypeCardProps {
  groupType: GroupTypeAdminDto;
  onEdit: () => void;
  onViewGroups: () => void;
}

export function GroupTypeCard({ groupType, onEdit, onViewGroups }: GroupTypeCardProps) {
  return (
    <div
      className="bg-white rounded-lg border border-gray-200 p-5 hover:shadow-md transition-shadow"
      style={{ borderLeftColor: groupType.color || '#6B7280', borderLeftWidth: '4px' }}
    >
      <div className="flex items-start justify-between mb-3">
        <div className="flex items-center gap-3">
          {groupType.iconCssClass && (
            <span
              className="text-2xl"
              style={{ color: groupType.color || '#6B7280' }}
            >
              <i className={groupType.iconCssClass} />
            </span>
          )}
          <div>
            <h3 className="text-lg font-semibold text-gray-900">{groupType.name}</h3>
            {groupType.description && (
              <p className="text-sm text-gray-600 line-clamp-2">{groupType.description}</p>
            )}
          </div>
        </div>
        {groupType.isSystem && (
          <span className="px-2 py-1 text-xs font-medium bg-gray-100 text-gray-600 rounded">
            System
          </span>
        )}
      </div>

      <div className="flex flex-wrap gap-2 mb-4">
        {groupType.takesAttendance && (
          <span className="px-2 py-1 text-xs bg-blue-100 text-blue-800 rounded">
            Attendance
          </span>
        )}
        {groupType.allowSelfRegistration && (
          <span className="px-2 py-1 text-xs bg-green-100 text-green-800 rounded">
            Self-Registration
          </span>
        )}
        {groupType.defaultIsPublic && (
          <span className="px-2 py-1 text-xs bg-purple-100 text-purple-800 rounded">
            Public
          </span>
        )}
      </div>

      <div className="flex items-center justify-between pt-3 border-t border-gray-200">
        <button
          onClick={onViewGroups}
          className="text-sm text-gray-600 hover:text-gray-900"
        >
          {groupType.groupCount} {groupType.groupCount === 1 ? groupType.groupTerm : `${groupType.groupTerm}s`}
        </button>
        <button
          onClick={onEdit}
          disabled={groupType.isArchived}
          className="text-sm text-primary-600 hover:text-primary-700 font-medium disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Edit
        </button>
      </div>
    </div>
  );
}
