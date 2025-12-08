/**
 * Group Tree Component
 * Expandable/collapsible tree view for group hierarchy
 */

import { useState } from 'react';
import { Link } from 'react-router-dom';
import type { GroupSummaryDto } from '@/services/api/types';
import { useChildGroups } from '@/hooks/useGroups';

export interface GroupTreeProps {
  group: GroupSummaryDto;
  level?: number;
}

export function GroupTree({ group, level = 0 }: GroupTreeProps) {
  const [isExpanded, setIsExpanded] = useState(level === 0);
  const { data: childrenData, isLoading } = useChildGroups(
    isExpanded ? group.idKey : undefined
  );

  const hasChildren = childrenData && childrenData.data.length > 0;
  const children = childrenData?.data || [];

  return (
    <div>
      <div
        className={`flex items-center gap-2 py-2 px-3 hover:bg-gray-50 rounded-lg transition-colors ${
          level > 0 ? 'ml-6' : ''
        }`}
      >
        {/* Expand/Collapse Button */}
        <button
          onClick={() => setIsExpanded(!isExpanded)}
          className="flex-shrink-0 w-5 h-5 flex items-center justify-center text-gray-400 hover:text-gray-600"
          aria-label={isExpanded ? 'Collapse' : 'Expand'}
        >
          {isLoading ? (
            <svg className="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24" aria-hidden="true">
              <circle
                className="opacity-25"
                cx="12"
                cy="12"
                r="10"
                stroke="currentColor"
                strokeWidth="4"
              />
              <path
                className="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
              />
            </svg>
          ) : hasChildren || level === 0 ? (
            <svg
              className={`w-4 h-4 transition-transform ${
                isExpanded ? 'rotate-90' : ''
              }`}
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 5l7 7-7 7"
              />
            </svg>
          ) : (
            <span className="w-4 h-4" />
          )}
        </button>

        {/* Group Icon */}
        <div
          className={`flex-shrink-0 w-8 h-8 rounded flex items-center justify-center ${
            group.isActive ? 'bg-blue-100 text-blue-600' : 'bg-gray-100 text-gray-400'
          }`}
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"
            />
          </svg>
        </div>

        {/* Group Details */}
        <Link
          to={`/admin/groups/${group.idKey}`}
          className="flex-1 min-w-0 flex items-center justify-between group"
        >
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-medium text-gray-900 truncate group-hover:text-primary-600">
                {group.name}
              </h3>
              {!group.isActive && (
                <span className="flex-shrink-0 px-2 py-0.5 text-xs font-medium text-gray-500 bg-gray-100 rounded">
                  Inactive
                </span>
              )}
            </div>
            {group.description && (
              <p className="text-xs text-gray-500 truncate">{group.description}</p>
            )}
          </div>

          <div className="flex-shrink-0 flex items-center gap-4 text-xs text-gray-500">
            <span>{group.memberCount} members</span>
            {group.campus && (
              <span className="text-gray-400">{group.campus.name}</span>
            )}
          </div>
        </Link>
      </div>

      {/* Child Groups */}
      {isExpanded && hasChildren && (
        <div className="mt-1">
          {children.map((child) => (
            <GroupTree key={child.idKey} group={child} level={level + 1} />
          ))}
        </div>
      )}
    </div>
  );
}
