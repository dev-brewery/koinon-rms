/**
 * GroupTypeEditorModal Component
 * Modal for creating and editing group types
 */

import { useState, useEffect, useCallback } from 'react';
import type { GroupTypeAdminDto, CreateGroupTypeRequest, UpdateGroupTypeRequest } from '@/services/api/types';
import { useCreateGroupType, useUpdateGroupType, useGroupType } from '@/hooks/useGroupTypes';

interface GroupTypeEditorModalProps {
  isOpen: boolean;
  onClose: () => void;
  groupType?: GroupTypeAdminDto;
}

const ICON_OPTIONS = [
  { value: 'fa fa-users', label: 'Users' },
  { value: 'fa fa-heart', label: 'Heart' },
  { value: 'fa fa-book', label: 'Book' },
  { value: 'fa fa-graduation-cap', label: 'Graduation Cap' },
  { value: 'fa fa-home', label: 'Home' },
  { value: 'fa fa-child', label: 'Child' },
  { value: 'fa fa-hands-helping', label: 'Helping Hands' },
  { value: 'fa fa-praying-hands', label: 'Praying Hands' },
  { value: 'fa fa-cross', label: 'Cross' },
  { value: 'fa fa-music', label: 'Music' },
  { value: 'fa fa-globe', label: 'Globe' },
  { value: 'fa fa-star', label: 'Star' },
];

export function GroupTypeEditorModal({ isOpen, onClose, groupType }: GroupTypeEditorModalProps) {
  const isEditing = !!groupType;
  const createMutation = useCreateGroupType();
  const updateMutation = useUpdateGroupType();

  // Fetch detail DTO when editing to get showInGroupList and showInNavigation
  const { data: groupTypeDetail } = useGroupType(isEditing ? groupType?.idKey : undefined);

  const [formData, setFormData] = useState({
    name: '',
    description: '',
    iconCssClass: 'fa fa-users',
    color: '#3B82F6',
    groupTerm: 'Group',
    groupMemberTerm: 'Member',
    takesAttendance: false,
    allowSelfRegistration: false,
    requiresMemberApproval: false,
    defaultIsPublic: true,
    defaultGroupCapacity: undefined as number | undefined,
    showInGroupList: true,
    showInNavigation: false,
    order: 0,
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  // Populate form when editing - use detail DTO for showInGroupList/showInNavigation
  useEffect(() => {
    if (groupType) {
      setFormData({
        name: groupType.name,
        description: groupType.description || '',
        iconCssClass: groupType.iconCssClass || 'fa fa-users',
        color: groupType.color || '#3B82F6',
        groupTerm: groupType.groupTerm,
        groupMemberTerm: groupType.groupMemberTerm,
        takesAttendance: groupType.takesAttendance,
        allowSelfRegistration: groupType.allowSelfRegistration,
        requiresMemberApproval: groupType.requiresMemberApproval,
        defaultIsPublic: groupType.defaultIsPublic,
        defaultGroupCapacity: groupType.defaultGroupCapacity,
        showInGroupList: groupTypeDetail?.showInGroupList ?? false,
        showInNavigation: groupTypeDetail?.showInNavigation ?? false,
        order: groupType.order,
      });
    }
  }, [groupType, groupTypeDetail]);

  // Handle escape key
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };

    if (isOpen) {
      document.addEventListener('keydown', handleEscape);
    }

    return () => {
      document.removeEventListener('keydown', handleEscape);
    };
  }, [isOpen, onClose]);

  const validateForm = useCallback((): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.name.trim()) {
      newErrors.name = 'Name is required';
    }

    if (!formData.groupTerm.trim()) {
      newErrors.groupTerm = 'Group term is required';
    }

    if (!formData.groupMemberTerm.trim()) {
      newErrors.groupMemberTerm = 'Group member term is required';
    }

    if (formData.defaultGroupCapacity !== undefined && formData.defaultGroupCapacity < 1) {
      newErrors.defaultGroupCapacity = 'Capacity must be at least 1';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }, [formData]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    try {
      const request: CreateGroupTypeRequest | UpdateGroupTypeRequest = {
        name: formData.name,
        description: formData.description || undefined,
        iconCssClass: formData.iconCssClass,
        color: formData.color,
        groupTerm: formData.groupTerm,
        groupMemberTerm: formData.groupMemberTerm,
        takesAttendance: formData.takesAttendance,
        allowSelfRegistration: formData.allowSelfRegistration,
        requiresMemberApproval: formData.requiresMemberApproval,
        defaultIsPublic: formData.defaultIsPublic,
        defaultGroupCapacity: formData.defaultGroupCapacity,
        showInGroupList: formData.showInGroupList,
        showInNavigation: formData.showInNavigation,
        order: formData.order,
      };

      if (isEditing) {
        await updateMutation.mutateAsync({ idKey: groupType.idKey, request });
      } else {
        await createMutation.mutateAsync(request as CreateGroupTypeRequest);
      }

      onClose();
    } catch (error) {
      console.error('Failed to save group type:', error);
    }
  };

  if (!isOpen) {
    return null;
  }

  const isPending = createMutation.isPending || updateMutation.isPending;
  const error = createMutation.error || updateMutation.error;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
        <div className="sticky top-0 bg-white border-b border-gray-200 px-6 py-4">
          <h2 className="text-2xl font-bold text-gray-900">
            {isEditing ? 'Edit Group Type' : 'Create Group Type'}
          </h2>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-6">
          {/* Name */}
          <div>
            <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
              Name <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              id="name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
                errors.name ? 'border-red-500' : 'border-gray-300'
              }`}
            />
            {errors.name && <p className="mt-1 text-sm text-red-600">{errors.name}</p>}
          </div>

          {/* Description */}
          <div>
            <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
              Description
            </label>
            <textarea
              id="description"
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              rows={3}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>

          {/* Icon and Color */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label htmlFor="iconCssClass" className="block text-sm font-medium text-gray-700 mb-1">
                Icon
              </label>
              <select
                id="iconCssClass"
                value={formData.iconCssClass}
                onChange={(e) => setFormData({ ...formData, iconCssClass: e.target.value })}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              >
                {ICON_OPTIONS.map((icon) => (
                  <option key={icon.value} value={icon.value}>
                    {icon.label}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label htmlFor="color" className="block text-sm font-medium text-gray-700 mb-1">
                Color
              </label>
              <div className="flex gap-2">
                <input
                  type="color"
                  id="color"
                  value={formData.color}
                  onChange={(e) => setFormData({ ...formData, color: e.target.value })}
                  className="w-12 h-10 border border-gray-300 rounded cursor-pointer"
                />
                <input
                  type="text"
                  value={formData.color}
                  onChange={(e) => setFormData({ ...formData, color: e.target.value })}
                  className="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  placeholder="#3B82F6"
                />
              </div>
            </div>
          </div>

          {/* Group Terms */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label htmlFor="groupTerm" className="block text-sm font-medium text-gray-700 mb-1">
                Group Term <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                id="groupTerm"
                value={formData.groupTerm}
                onChange={(e) => setFormData({ ...formData, groupTerm: e.target.value })}
                className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
                  errors.groupTerm ? 'border-red-500' : 'border-gray-300'
                }`}
                placeholder="e.g., Group, Team, Class"
              />
              {errors.groupTerm && <p className="mt-1 text-sm text-red-600">{errors.groupTerm}</p>}
            </div>

            <div>
              <label htmlFor="groupMemberTerm" className="block text-sm font-medium text-gray-700 mb-1">
                Member Term <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                id="groupMemberTerm"
                value={formData.groupMemberTerm}
                onChange={(e) => setFormData({ ...formData, groupMemberTerm: e.target.value })}
                className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
                  errors.groupMemberTerm ? 'border-red-500' : 'border-gray-300'
                }`}
                placeholder="e.g., Member, Participant, Student"
              />
              {errors.groupMemberTerm && <p className="mt-1 text-sm text-red-600">{errors.groupMemberTerm}</p>}
            </div>
          </div>

          {/* Default Group Capacity */}
          <div>
            <label htmlFor="defaultGroupCapacity" className="block text-sm font-medium text-gray-700 mb-1">
              Default Group Capacity
            </label>
            <input
              type="number"
              id="defaultGroupCapacity"
              value={formData.defaultGroupCapacity ?? ''}
              onChange={(e) =>
                setFormData({
                  ...formData,
                  defaultGroupCapacity: e.target.value ? parseInt(e.target.value, 10) : undefined,
                })
              }
              min="1"
              className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
                errors.defaultGroupCapacity ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="Leave empty for no limit"
            />
            {errors.defaultGroupCapacity && (
              <p className="mt-1 text-sm text-red-600">{errors.defaultGroupCapacity}</p>
            )}
          </div>

          {/* Order */}
          <div>
            <label htmlFor="order" className="block text-sm font-medium text-gray-700 mb-1">
              Display Order
            </label>
            <input
              type="number"
              id="order"
              value={formData.order}
              onChange={(e) => setFormData({ ...formData, order: parseInt(e.target.value, 10) || 0 })}
              min="0"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>

          {/* Checkboxes */}
          <div className="space-y-3">
            <div className="flex items-center">
              <input
                type="checkbox"
                id="takesAttendance"
                checked={formData.takesAttendance}
                onChange={(e) => setFormData({ ...formData, takesAttendance: e.target.checked })}
                className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
              />
              <label htmlFor="takesAttendance" className="ml-2 text-sm text-gray-700">
                Takes Attendance
              </label>
            </div>

            <div className="flex items-center">
              <input
                type="checkbox"
                id="allowSelfRegistration"
                checked={formData.allowSelfRegistration}
                onChange={(e) => setFormData({ ...formData, allowSelfRegistration: e.target.checked })}
                className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
              />
              <label htmlFor="allowSelfRegistration" className="ml-2 text-sm text-gray-700">
                Allow Self Registration
              </label>
            </div>

            <div className="flex items-center">
              <input
                type="checkbox"
                id="requiresMemberApproval"
                checked={formData.requiresMemberApproval}
                onChange={(e) => setFormData({ ...formData, requiresMemberApproval: e.target.checked })}
                className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
              />
              <label htmlFor="requiresMemberApproval" className="ml-2 text-sm text-gray-700">
                Requires Member Approval
              </label>
            </div>

            <div className="flex items-center">
              <input
                type="checkbox"
                id="defaultIsPublic"
                checked={formData.defaultIsPublic}
                onChange={(e) => setFormData({ ...formData, defaultIsPublic: e.target.checked })}
                className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
              />
              <label htmlFor="defaultIsPublic" className="ml-2 text-sm text-gray-700">
                Public by Default
              </label>
            </div>

            <div className="flex items-center">
              <input
                type="checkbox"
                id="showInGroupList"
                checked={formData.showInGroupList}
                onChange={(e) => setFormData({ ...formData, showInGroupList: e.target.checked })}
                className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
              />
              <label htmlFor="showInGroupList" className="ml-2 text-sm text-gray-700">
                Show in Group List
              </label>
            </div>

            <div className="flex items-center">
              <input
                type="checkbox"
                id="showInNavigation"
                checked={formData.showInNavigation}
                onChange={(e) => setFormData({ ...formData, showInNavigation: e.target.checked })}
                className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
              />
              <label htmlFor="showInNavigation" className="ml-2 text-sm text-gray-700">
                Show in Navigation
              </label>
            </div>
          </div>

          {/* Error Display */}
          {error && (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
              <p className="text-sm text-red-800">
                Failed to {isEditing ? 'update' : 'create'} group type. Please try again.
              </p>
            </div>
          )}

          {/* Actions */}
          <div className="flex justify-end gap-3 pt-4 border-t border-gray-200">
            <button
              type="button"
              onClick={onClose}
              disabled={isPending}
              className="px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isPending}
              className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-50"
            >
              {isPending ? 'Saving...' : isEditing ? 'Update' : 'Create'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
