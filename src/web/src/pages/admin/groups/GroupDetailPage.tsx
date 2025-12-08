/**
 * Group Detail Page
 * View and edit a single group's details
 */

import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useGroup, useChildGroups, useDeleteGroup } from '@/hooks/useGroups';
import { GroupSchedulesSection } from '@/components/admin/groups';

export function GroupDetailPage() {
  const { idKey } = useParams<{ idKey: string }>();
  const navigate = useNavigate();
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  const { data: group, isLoading, error } = useGroup(idKey);
  const { data: childrenData } = useChildGroups(idKey);
  const deleteGroup = useDeleteGroup();

  const children = childrenData?.data || [];

  const handleDelete = async () => {
    if (!idKey) return;

    try {
      await deleteGroup.mutateAsync(idKey);
      navigate('/admin/groups');
    } catch {
      // Error is handled by TanStack Query error state
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="inline-block w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
      </div>
    );
  }

  if (error || !group) {
    return (
      <div className="text-center py-12">
        <svg
          className="w-12 h-12 text-red-400 mx-auto mb-4"
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
        <p className="text-red-600">Failed to load group</p>
        <Link
          to="/admin/groups"
          className="inline-block mt-4 px-4 py-2 text-primary-600 hover:text-primary-700"
        >
          Back to Groups
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link
            to="/admin/groups"
            className="p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100 transition-colors"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M15 19l-7-7 7-7"
              />
            </svg>
          </Link>
          <div>
            <h1 className="text-3xl font-bold text-gray-900">{group.name}</h1>
            <p className="mt-1 text-gray-600">{group.groupType.name}</p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Link
            to={`/admin/groups/${idKey}/edit`}
            className="px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
          >
            Edit
          </Link>
          <button
            onClick={() => setShowDeleteConfirm(true)}
            className="px-4 py-2 text-red-600 bg-white border border-red-300 rounded-lg hover:bg-red-50 transition-colors"
          >
            Delete
          </button>
        </div>
      </div>

      {/* Status Banner */}
      {!group.isActive && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
          <div className="flex items-center gap-2">
            <svg
              className="w-5 h-5 text-yellow-600"
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
            <p className="text-sm font-medium text-yellow-800">
              This group is inactive and not visible in check-in
            </p>
          </div>
        </div>
      )}

      {/* Main Content */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Details */}
        <div className="lg:col-span-2 space-y-6">
          {/* Basic Info */}
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Details</h2>
            <dl className="space-y-4">
              {group.description && (
                <div>
                  <dt className="text-sm font-medium text-gray-500">Description</dt>
                  <dd className="mt-1 text-sm text-gray-900">{group.description}</dd>
                </div>
              )}

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <dt className="text-sm font-medium text-gray-500">Group Type</dt>
                  <dd className="mt-1 text-sm text-gray-900">{group.groupType.name}</dd>
                </div>

                {group.campus && (
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Campus</dt>
                    <dd className="mt-1 text-sm text-gray-900">{group.campus.name}</dd>
                  </div>
                )}
              </div>

              {group.capacity && (
                <div>
                  <dt className="text-sm font-medium text-gray-500">Capacity</dt>
                  <dd className="mt-1 text-sm text-gray-900">{group.capacity} people</dd>
                </div>
              )}

              {group.parentGroup && (
                <div>
                  <dt className="text-sm font-medium text-gray-500">Parent Group</dt>
                  <dd className="mt-1">
                    <Link
                      to={`/admin/groups/${group.parentGroup.idKey}`}
                      className="text-sm text-primary-600 hover:text-primary-700"
                    >
                      {group.parentGroup.name}
                    </Link>
                  </dd>
                </div>
              )}

              {group.schedule && (
                <div>
                  <dt className="text-sm font-medium text-gray-500">Schedule</dt>
                  <dd className="mt-1 text-sm text-gray-900">
                    {group.schedule.name} - {group.schedule.startTime}
                  </dd>
                </div>
              )}
            </dl>
          </div>

          {/* Child Groups */}
          {children.length > 0 && (
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-semibold text-gray-900">
                  Child Groups ({children.length})
                </h2>
                <Link
                  to={`/admin/groups/new?parentId=${idKey}`}
                  className="text-sm text-primary-600 hover:text-primary-700 font-medium"
                >
                  Add Child Group
                </Link>
              </div>

              <div className="space-y-2">
                {children.map((child) => (
                  <Link
                    key={child.idKey}
                    to={`/admin/groups/${child.idKey}`}
                    className="block p-3 hover:bg-gray-50 rounded-lg transition-colors"
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <div
                          className={`w-8 h-8 rounded flex items-center justify-center ${
                            child.isActive
                              ? 'bg-blue-100 text-blue-600'
                              : 'bg-gray-100 text-gray-400'
                          }`}
                        >
                          <svg
                            className="w-4 h-4"
                            fill="none"
                            stroke="currentColor"
                            viewBox="0 0 24 24"
                            aria-hidden="true"
                          >
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={2}
                              d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"
                            />
                          </svg>
                        </div>
                        <div>
                          <h3 className="text-sm font-medium text-gray-900">{child.name}</h3>
                          <p className="text-xs text-gray-500">{child.memberCount} members</p>
                        </div>
                      </div>
                      <svg
                        className="w-4 h-4 text-gray-400"
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
                    </div>
                  </Link>
                ))}
              </div>
            </div>
          )}

          {/* Schedules */}
          <GroupSchedulesSection groupIdKey={idKey!} />
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Stats */}
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Statistics</h2>
            <div className="space-y-4">
              <div>
                <div className="text-2xl font-bold text-gray-900">-</div>
                <div className="text-sm text-gray-500">Members</div>
              </div>
              <div>
                <div className="text-2xl font-bold text-gray-900">{children.length}</div>
                <div className="text-sm text-gray-500">Child Groups</div>
              </div>
            </div>
          </div>

          {/* Metadata */}
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Metadata</h2>
            <dl className="space-y-3 text-sm">
              <div>
                <dt className="text-gray-500">Created</dt>
                <dd className="text-gray-900">
                  {new Date(group.createdDateTime).toLocaleDateString()}
                </dd>
              </div>
              {group.modifiedDateTime && (
                <div>
                  <dt className="text-gray-500">Last Modified</dt>
                  <dd className="text-gray-900">
                    {new Date(group.modifiedDateTime).toLocaleDateString()}
                  </dd>
                </div>
              )}
              <div>
                <dt className="text-gray-500">Status</dt>
                <dd>
                  <span
                    className={`inline-block px-2 py-0.5 text-xs font-medium rounded ${
                      group.isActive
                        ? 'bg-green-100 text-green-800'
                        : 'bg-gray-100 text-gray-800'
                    }`}
                  >
                    {group.isActive ? 'Active' : 'Inactive'}
                  </span>
                </dd>
              </div>
            </dl>
          </div>
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      {showDeleteConfirm && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Delete Group</h3>
            <p className="text-gray-600 mb-6">
              Are you sure you want to delete "{group.name}"? This action cannot be undone.
            </p>
            <div className="flex items-center gap-3 justify-end">
              <button
                onClick={() => setShowDeleteConfirm(false)}
                className="px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={handleDelete}
                disabled={deleteGroup.isPending}
                className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50"
              >
                {deleteGroup.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
