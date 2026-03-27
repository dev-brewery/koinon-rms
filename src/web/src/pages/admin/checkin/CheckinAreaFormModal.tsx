/**
 * Check-in Area Form Modal
 * Create or edit a check-in area (group with check-in type)
 */

import { useState, useEffect } from 'react';
import { useCreateGroup, useUpdateGroup } from '@/hooks/useGroups';
import { useToast } from '@/contexts/ToastContext';
import type { CheckinAreaDto } from '@/services/api/types';

interface CheckinAreaFormModalProps {
  area: CheckinAreaDto | null;
  onClose: () => void;
}

export function CheckinAreaFormModal({ area, onClose }: CheckinAreaFormModalProps) {
  const toast = useToast();
  const isEditing = !!area;

  const createGroup = useCreateGroup();
  const updateGroup = useUpdateGroup();

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [isActive, setIsActive] = useState(true);

  useEffect(() => {
    if (area) {
      setName(area.name);
      setDescription(area.description ?? '');
      setIsActive(area.isActive);
    } else {
      setName('');
      setDescription('');
      setIsActive(true);
    }
  }, [area]);

  const isPending = createGroup.isPending || updateGroup.isPending;

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    if (!name.trim()) return;

    try {
      if (isEditing) {
        await updateGroup.mutateAsync({
          idKey: area.idKey,
          request: {
            name: name.trim(),
            description: description.trim() || undefined,
            isActive,
          },
        });
        toast.success('Area updated', `"${name.trim()}" has been updated.`);
      } else {
        // groupTypeId must be provided by the caller or configured system-wide.
        // When creating a new check-in area, the group type for check-in is
        // looked up server-side; pass empty string and let validation surface the issue.
        await createGroup.mutateAsync({
          name: name.trim(),
          description: description.trim() || undefined,
          groupTypeId: '',
          isActive,
        });
        toast.success('Area created', `"${name.trim()}" has been created.`);
      }
      onClose();
    } catch {
      toast.error(
        isEditing ? 'Update failed' : 'Create failed',
        'Could not save the check-in area. Please try again.'
      );
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto" role="dialog" aria-modal="true" aria-labelledby="area-modal-title">
      {/* Backdrop */}
      <div className="fixed inset-0 bg-black bg-opacity-40" onClick={onClose} />

      {/* Modal */}
      <div className="relative min-h-screen flex items-center justify-center p-4">
        <div className="relative bg-white rounded-xl shadow-xl w-full max-w-md">
          {/* Header */}
          <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
            <h2 id="area-modal-title" className="text-lg font-semibold text-gray-900">
              {isEditing ? 'Edit Check-in Area' : 'Create Check-in Area'}
            </h2>
            <button
              type="button"
              onClick={onClose}
              className="p-1 text-gray-400 hover:text-gray-600 rounded-md"
              aria-label="Close"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit} className="px-6 py-5 space-y-5">
            {/* Name */}
            <div>
              <label htmlFor="area-name" className="block text-sm font-medium text-gray-700 mb-1">
                Name <span className="text-red-500">*</span>
              </label>
              <input
                id="area-name"
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm"
                placeholder="e.g. Children's Ministry"
              />
            </div>

            {/* Description */}
            <div>
              <label htmlFor="area-description" className="block text-sm font-medium text-gray-700 mb-1">
                Description
              </label>
              <textarea
                id="area-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 text-sm resize-none"
                placeholder="Optional description..."
              />
            </div>

            {/* Active Toggle */}
            <div className="flex items-center gap-3">
              <button
                type="button"
                role="switch"
                aria-checked={isActive}
                onClick={() => setIsActive((v) => !v)}
                className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 ${
                  isActive ? 'bg-primary-600' : 'bg-gray-200'
                }`}
              >
                <span
                  className={`inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform ${
                    isActive ? 'translate-x-6' : 'translate-x-1'
                  }`}
                />
              </button>
              <span className="text-sm font-medium text-gray-700">Active</span>
            </div>

            {/* Footer */}
            <div className="flex items-center justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={onClose}
                disabled={isPending}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors disabled:opacity-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isPending || !name.trim()}
                className="px-4 py-2 text-sm font-medium text-white bg-primary-600 rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isPending ? 'Saving...' : isEditing ? 'Save Changes' : 'Create Area'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
