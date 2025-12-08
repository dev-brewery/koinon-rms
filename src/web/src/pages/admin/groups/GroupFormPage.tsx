/**
 * Group Form Page
 * Create or edit a group
 */

import { useEffect, useState } from 'react';
import { useParams, useNavigate, useSearchParams, Link } from 'react-router-dom';
import { useGroup, useCreateGroup, useUpdateGroup } from '@/hooks/useGroups';
import type { CreateGroupRequest, UpdateGroupRequest } from '@/services/api/types';

export function GroupFormPage() {
  const { idKey } = useParams<{ idKey: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const isEditMode = !!idKey;

  const { data: group, isLoading } = useGroup(idKey);
  const createGroup = useCreateGroup();
  const updateGroup = useUpdateGroup();

  // Form state
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [groupTypeId, setGroupTypeId] = useState('');
  const [parentGroupId, setParentGroupId] = useState('');
  const [campusId, setCampusId] = useState('');
  const [capacity, setCapacity] = useState('');
  const [isActive, setIsActive] = useState(true);

  // Pre-fill parent group from query param
  useEffect(() => {
    const parentId = searchParams.get('parentId');
    if (parentId) {
      setParentGroupId(parentId);
    }
  }, [searchParams]);

  // Load existing group data in edit mode
  useEffect(() => {
    if (group) {
      setName(group.name);
      setDescription(group.description || '');
      setGroupTypeId(group.groupType.idKey);
      setParentGroupId(group.parentGroup?.idKey || '');
      setCampusId(group.campus?.idKey || '');
      setCapacity(group.capacity?.toString() || '');
      setIsActive(group.isActive);
    }
  }, [group]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (isEditMode && idKey) {
      // Update existing group
      const request: UpdateGroupRequest = {
        name,
        description: description || undefined,
        campusId: campusId || undefined,
        capacity: capacity ? parseInt(capacity) : undefined,
        isActive,
      };

      try {
        await updateGroup.mutateAsync({ idKey, request });
        navigate(`/admin/groups/${idKey}`);
      } catch {
        // Error is handled by TanStack Query error state
      }
    } else {
      // Create new group
      const request: CreateGroupRequest = {
        name,
        description: description || undefined,
        groupTypeId,
        parentGroupId: parentGroupId || undefined,
        campusId: campusId || undefined,
        capacity: capacity ? parseInt(capacity) : undefined,
        isActive,
      };

      try {
        const newGroup = await createGroup.mutateAsync(request);
        navigate(`/admin/groups/${newGroup.idKey}`);
      } catch {
        // Error is handled by TanStack Query error state
      }
    }
  };

  if (isEditMode && isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="inline-block w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
      </div>
    );
  }

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link
          to={idKey ? `/admin/groups/${idKey}` : '/admin/groups'}
          className="p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100 transition-colors"
        >
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
        </Link>
        <div>
          <h1 className="text-3xl font-bold text-gray-900">
            {isEditMode ? 'Edit Group' : 'Create Group'}
          </h1>
          <p className="mt-1 text-gray-600">
            {isEditMode
              ? 'Update group details and settings'
              : 'Add a new group to your organization'}
          </p>
        </div>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit} className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="space-y-6">
          {/* Name */}
          <div>
            <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
              Name <span className="text-red-500">*</span>
            </label>
            <input
              id="name"
              type="text"
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              placeholder="e.g., Elementary Check-in"
            />
          </div>

          {/* Description */}
          <div>
            <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
              Description
            </label>
            <textarea
              id="description"
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              placeholder="Optional description of this group"
            />
          </div>

          {/* Group Type (only for create) */}
          {!isEditMode && (
            <div>
              <label htmlFor="groupTypeId" className="block text-sm font-medium text-gray-700 mb-1">
                Group Type <span className="text-red-500">*</span>
              </label>
              <select
                id="groupTypeId"
                required
                value={groupTypeId}
                onChange={(e) => setGroupTypeId(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              >
                <option value="">Select a group type...</option>
                <option value="checkin-area">Check-in Area</option>
                <option value="age-group">Age Group</option>
                <option value="general">General</option>
              </select>
              <p className="mt-1 text-xs text-gray-500">
                Note: Group type cannot be changed after creation
              </p>
            </div>
          )}

          {/* Parent Group */}
          <div>
            <label htmlFor="parentGroupId" className="block text-sm font-medium text-gray-700 mb-1">
              Parent Group
            </label>
            <select
              id="parentGroupId"
              value={parentGroupId}
              onChange={(e) => setParentGroupId(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              disabled={!!searchParams.get('parentId')}
            >
              <option value="">None (Top-level group)</option>
            </select>
            {searchParams.get('parentId') && (
              <p className="mt-1 text-xs text-gray-500">Parent group is pre-selected</p>
            )}
          </div>

          {/* Campus */}
          <div>
            <label htmlFor="campusId" className="block text-sm font-medium text-gray-700 mb-1">
              Campus
            </label>
            <select
              id="campusId"
              value={campusId}
              onChange={(e) => setCampusId(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            >
              <option value="">All Campuses</option>
            </select>
          </div>

          {/* Capacity */}
          <div>
            <label htmlFor="capacity" className="block text-sm font-medium text-gray-700 mb-1">
              Capacity Limit
            </label>
            <input
              id="capacity"
              type="number"
              min="0"
              value={capacity}
              onChange={(e) => setCapacity(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              placeholder="Leave blank for unlimited"
            />
            <p className="mt-1 text-xs text-gray-500">
              Optional maximum number of members for this group
            </p>
          </div>

          {/* Is Active */}
          <div className="flex items-center gap-2">
            <input
              id="isActive"
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
            />
            <label htmlFor="isActive" className="text-sm font-medium text-gray-700">
              Active
            </label>
            <span className="text-xs text-gray-500">
              (Inactive groups are hidden from check-in)
            </span>
          </div>
        </div>

        {/* Actions */}
        <div className="mt-6 flex items-center justify-end gap-3 pt-6 border-t border-gray-200">
          <Link
            to={idKey ? `/admin/groups/${idKey}` : '/admin/groups'}
            className="px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
          >
            Cancel
          </Link>
          <button
            type="submit"
            disabled={createGroup.isPending || updateGroup.isPending}
            className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50"
          >
            {createGroup.isPending || updateGroup.isPending
              ? 'Saving...'
              : isEditMode
              ? 'Update Group'
              : 'Create Group'}
          </button>
        </div>
      </form>
    </div>
  );
}
