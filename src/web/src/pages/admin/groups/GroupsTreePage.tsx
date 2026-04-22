/**
 * Groups Tree Page
 * Main page for viewing and managing groups in a tree view
 */

import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useGroups } from '@/hooks/useGroups';
import { GroupTree } from '@/components/admin/groups/GroupTree';
import { Loading, EmptyState, ErrorState } from '@/components/ui';
import { getErrorMessage } from '@/lib/errorMessages';

type ViewMode = 'tree' | 'list';

export function GroupsTreePage() {
  const navigate = useNavigate();
  const [viewMode, setViewMode] = useState<ViewMode>('tree');
  const [searchQuery, setSearchQuery] = useState('');

  // Fetch top-level groups (no parent)
  const { data, isLoading, error, refetch } = useGroups({
    q: searchQuery || undefined,
    includeInactive: false,
  });

  const groups = data?.data || [];
  // Show all groups in tree view - the API should return only top-level when no parentGroupId filter
  const topLevelGroups = groups;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Groups</h1>
          <p className="mt-2 text-gray-600">
            Manage check-in groups and organizational structure
          </p>
        </div>
        <Link
          to="/admin/groups/new"
          className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
        >
          Create Group
        </Link>
      </div>

      {/* Filters and View Toggle */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <div className="flex items-center gap-4">
          {/* Search */}
          <div className="flex-1 max-w-md">
            <div className="relative">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
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
                    d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
                  />
                </svg>
              </div>
              <input
                type="text"
                placeholder="Search groups..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-10 pr-4 py-2 w-full border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
            </div>
          </div>

          {/* View Mode Toggle */}
          <div className="flex items-center gap-2 bg-gray-100 rounded-lg p-1">
            <button
              onClick={() => setViewMode('tree')}
              className={`px-3 py-1.5 text-sm font-medium rounded transition-colors ${
                viewMode === 'tree'
                  ? 'bg-white text-gray-900 shadow-sm'
                  : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z"
                />
              </svg>
            </button>
            <button
              onClick={() => setViewMode('list')}
              className={`px-3 py-1.5 text-sm font-medium rounded transition-colors ${
                viewMode === 'list'
                  ? 'bg-white text-gray-900 shadow-sm'
                  : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M4 6h16M4 10h16M4 14h16M4 18h16"
                />
              </svg>
            </button>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="bg-white rounded-lg border border-gray-200">
        {isLoading ? (
          <Loading text="Loading groups..." />
        ) : error ? (
          <ErrorState
            title="Failed to load groups"
            message={getErrorMessage(error).message}
            onRetry={() => refetch()}
          />
        ) : groups.length === 0 ? (
          <EmptyState
            title={searchQuery ? 'No groups found' : 'No groups yet'}
            description={
              searchQuery
                ? 'Try adjusting your search or create a new group.'
                : 'Organize your ministry by creating groups for classes, teams, or serving areas.'
            }
            action={
              !searchQuery
                ? {
                    label: 'Create your first group',
                    onClick: () => navigate('/admin/groups/new'),
                  }
                : undefined
            }
          />
        ) : viewMode === 'tree' ? (
          <div className="p-4">
            {topLevelGroups.map((group) => (
              <GroupTree key={group.idKey} group={group} />
            ))}
          </div>
        ) : (
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
                <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Type</th>
                <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Members</th>
                <th scope="col" className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider"></th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {groups.map((group) => (
                <tr
                  key={group.idKey}
                  className="hover:bg-gray-50 cursor-pointer"
                  onClick={() => navigate(`/admin/groups/${group.idKey}`)}
                >
                  <td className="px-6 py-4 whitespace-nowrap">
                    <Link to={`/admin/groups/${group.idKey}`} className="text-sm font-medium text-gray-900 hover:text-primary-600">
                      {group.name}
                    </Link>
                    {group.description && (
                      <p className="text-xs text-gray-500 mt-1 truncate max-w-xs">{group.description}</p>
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                      {group.groupTypeName || group.groupType?.name}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {group.memberCount} members
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right">
                    <Link to={`/admin/groups/${group.idKey}`} className="text-gray-400 hover:text-gray-600">
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                      </svg>
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
