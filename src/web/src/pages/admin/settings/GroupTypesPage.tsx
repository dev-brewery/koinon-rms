/**
 * Group Types Page
 * Admin page for managing group types
 */

import { useState } from 'react';
import { useGroupTypes, useArchiveGroupType } from '@/hooks/useGroupTypes';
import { GroupTypeCard } from '@/components/admin/GroupTypeCard';
import { GroupTypeEditorModal } from '@/components/admin/GroupTypeEditorModal';
import { GroupTypeGroupsModal } from '@/components/admin/GroupTypeGroupsModal';
import type { GroupTypeAdminDto } from '@/services/api/types';

export function GroupTypesPage() {
  const [includeArchived, setIncludeArchived] = useState(false);
  const [editingType, setEditingType] = useState<GroupTypeAdminDto | null>(null);
  const [isCreating, setIsCreating] = useState(false);
  const [viewingGroupsFor, setViewingGroupsFor] = useState<GroupTypeAdminDto | null>(null);
  const [archivingId, setArchivingId] = useState<string | null>(null);

  const { data: groupTypes = [], isLoading, error } = useGroupTypes(includeArchived);
  const archiveMutation = useArchiveGroupType();

  const handleEdit = (groupType: GroupTypeAdminDto) => {
    setEditingType(groupType);
  };

  const handleCreate = () => {
    setIsCreating(true);
  };

  const handleCloseEditor = () => {
    setEditingType(null);
    setIsCreating(false);
  };

  const handleViewGroups = (groupType: GroupTypeAdminDto) => {
    setViewingGroupsFor(groupType);
  };

  const handleCloseGroups = () => {
    setViewingGroupsFor(null);
  };

  const handleArchive = async (groupType: GroupTypeAdminDto) => {
    if (!confirm(`Are you sure you want to archive "${groupType.name}"?`)) {
      return;
    }

    setArchivingId(groupType.idKey);
    try {
      await archiveMutation.mutateAsync(groupType.idKey);
    } catch (error) {
      console.error('Failed to archive group type:', error);
    } finally {
      setArchivingId(null);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Group Types</h1>
          <p className="mt-2 text-gray-600">Configure group types and their default settings</p>
        </div>
        <button
          onClick={handleCreate}
          className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
        >
          Create Group Type
        </button>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <div className="flex items-center">
          <input
            type="checkbox"
            id="includeArchived"
            checked={includeArchived}
            onChange={(e) => setIncludeArchived(e.target.checked)}
            className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
          />
          <label htmlFor="includeArchived" className="ml-2 text-sm text-gray-700">
            Include archived group types
          </label>
        </div>
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="flex items-center justify-center py-12">
          <div className="inline-block w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
        </div>
      )}

      {/* Error State */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex items-center gap-2">
            <svg
              className="w-5 h-5 text-red-600"
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
            <p className="text-sm font-medium text-red-800">Failed to load group types</p>
          </div>
        </div>
      )}

      {/* Group Types Grid */}
      {!isLoading && !error && (
        <div className="space-y-4">
          {groupTypes.length === 0 ? (
            <div className="bg-white rounded-lg border border-gray-200 p-12 text-center">
              <svg
                className="w-12 h-12 text-gray-400 mx-auto mb-4"
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
              <p className="text-gray-500 mb-4">No group types found</p>
              <button
                onClick={handleCreate}
                className="inline-block px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
              >
                Create First Group Type
              </button>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {groupTypes.map((groupType) => (
                <div key={groupType.idKey} className="relative">
                  <GroupTypeCard
                    groupType={groupType}
                    onEdit={() => handleEdit(groupType)}
                    onViewGroups={() => handleViewGroups(groupType)}
                  />
                  {!groupType.isSystem && !groupType.isArchived && (
                    <button
                      onClick={() => handleArchive(groupType)}
                      disabled={archivingId === groupType.idKey}
                      className="absolute top-2 right-2 p-2 text-gray-400 hover:text-red-600 transition-colors disabled:opacity-50"
                      title="Archive group type"
                    >
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M5 8h14M5 8a2 2 0 110-4h14a2 2 0 110 4M5 8v10a2 2 0 002 2h10a2 2 0 002-2V8m-9 4h4"
                        />
                      </svg>
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Modals */}
      <GroupTypeEditorModal
        isOpen={isCreating || editingType !== null}
        onClose={handleCloseEditor}
        groupType={editingType || undefined}
      />

      <GroupTypeGroupsModal
        isOpen={viewingGroupsFor !== null}
        onClose={handleCloseGroups}
        groupType={viewingGroupsFor}
      />
    </div>
  );
}
