/**
 * Edit Member Modal Component
 * Modal for editing member status and notes
 */

import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/Button';
import type { MyGroupMemberDetailDto } from '@/services/api/types';

interface EditMemberModalProps {
  isOpen: boolean;
  onClose: () => void;
  member: MyGroupMemberDetailDto | null;
  onSave: (data: {
    roleId?: string;
    status?: string;
    note?: string;
  }) => Promise<void>;
  isSaving: boolean;
}

export function EditMemberModal({
  isOpen,
  onClose,
  member,
  onSave,
  isSaving,
}: EditMemberModalProps) {
  const [status, setStatus] = useState<string>('Active');
  const [note, setNote] = useState('');

  // Initialize form when member changes
  useEffect(() => {
    if (member) {
      setStatus(member.status);
      setNote(member.note || '');
    }
  }, [member]);

  // Add escape key handler
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    if (isOpen) {
      document.addEventListener('keydown', handleEscape);
      return () => document.removeEventListener('keydown', handleEscape);
    }
  }, [isOpen, onClose]);

  const handleSave = async () => {
    const data: {
      roleId?: string;
      status?: string;
      note?: string;
    } = {};

    // Only send changed values
    if (member) {
      if (status !== member.status) data.status = status;
      if (note !== (member.note || '')) data.note = note;
    }

    await onSave(data);
  };

  const handleClose = () => {
    onClose();
  };

  if (!isOpen || !member) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:p-0">
        {/* Backdrop */}
        <div
          className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"
          onClick={handleClose}
        />

        {/* Modal */}
        <div className="relative inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
          <div className="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
            <div className="flex items-start justify-between mb-4">
              <h3 className="text-lg font-medium text-gray-900">
                Edit Member: {member.firstName} {member.lastName}
              </h3>
              <button
                onClick={handleClose}
                className="text-gray-400 hover:text-gray-500"
                aria-label="Close"
              >
                <svg
                  className="w-6 h-6"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              </button>
            </div>

            {/* Status Dropdown */}
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Status
              </label>
              <select
                value={status}
                onChange={(e) => setStatus(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              >
                <option value="Active">Active</option>
                <option value="Inactive">Inactive</option>
                <option value="Pending">Pending</option>
              </select>
            </div>

            {/* Note Textarea */}
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Note
              </label>
              <textarea
                value={note}
                onChange={(e) => setNote(e.target.value)}
                rows={3}
                placeholder="Add a note about this member..."
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
          </div>

          {/* Actions */}
          <div className="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse gap-3">
            <Button onClick={handleSave} loading={isSaving} disabled={isSaving}>
              Save Changes
            </Button>
            <Button variant="outline" onClick={handleClose} disabled={isSaving}>
              Cancel
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
