/**
 * Request to Join Modal Component
 * Modal for users to request membership in a group
 */

import { useState, useEffect, useCallback } from 'react';
import { Button } from '@/components/ui/Button';
import { useSubmitMembershipRequest } from '@/hooks/useMembershipRequests';

interface RequestToJoinModalProps {
  isOpen: boolean;
  onClose: () => void;
  groupIdKey: string;
  groupName: string;
}

export function RequestToJoinModal({
  isOpen,
  onClose,
  groupIdKey,
  groupName,
}: RequestToJoinModalProps) {
  const [note, setNote] = useState('');
  const [showSuccess, setShowSuccess] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submitMutation = useSubmitMembershipRequest(groupIdKey);

  // Memoized close handler to prevent unnecessary re-renders
  const handleClose = useCallback(() => {
    if (!submitMutation.isPending) {
      onClose();
    }
  }, [submitMutation.isPending, onClose]);

  // Reset state when modal opens/closes
  useEffect(() => {
    if (isOpen) {
      setNote('');
      setShowSuccess(false);
      setError(null);
    }
  }, [isOpen]);

  // Add escape key handler
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !submitMutation.isPending) {
        handleClose();
      }
    };
    if (isOpen) {
      document.addEventListener('keydown', handleEscape);
      return () => document.removeEventListener('keydown', handleEscape);
    }
  }, [isOpen, submitMutation.isPending, handleClose]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    try {
      await submitMutation.mutateAsync({ note: note.trim() || undefined });
      setShowSuccess(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to submit request');
    }
  };

  // Auto-close after success with cleanup
  useEffect(() => {
    if (showSuccess) {
      const timer = setTimeout(() => {
        handleClose();
      }, 2000);
      return () => clearTimeout(timer);
    }
  }, [showSuccess, handleClose]);

  if (!isOpen) return null;

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
          <form onSubmit={handleSubmit}>
            <div className="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
              <div className="flex items-start justify-between mb-4">
                <div>
                  <h3 className="text-lg font-medium text-gray-900">
                    Request to Join Group
                  </h3>
                  <p className="text-sm text-gray-500 mt-1">{groupName}</p>
                </div>
                <button
                  type="button"
                  onClick={handleClose}
                  disabled={submitMutation.isPending}
                  className="text-gray-400 hover:text-gray-500 disabled:opacity-50"
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

              {showSuccess ? (
                <div className="py-8 text-center">
                  <svg
                    className="mx-auto h-12 w-12 text-green-600 mb-4"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                    />
                  </svg>
                  <h4 className="text-lg font-medium text-gray-900 mb-2">
                    Request Submitted!
                  </h4>
                  <p className="text-sm text-gray-600">
                    A group leader will review your request.
                  </p>
                </div>
              ) : (
                <>
                  <p className="text-sm text-gray-600 mb-4">
                    Your request will be sent to the group leaders for approval.
                    You can optionally include a message below.
                  </p>

                  <div className="mb-4">
                    <label
                      htmlFor="request-note"
                      className="block text-sm font-medium text-gray-700 mb-1"
                    >
                      Message (optional)
                    </label>
                    <textarea
                      id="request-note"
                      value={note}
                      onChange={(e) => setNote(e.target.value)}
                      disabled={submitMutation.isPending}
                      rows={4}
                      maxLength={2000}
                      placeholder="Why would you like to join this group?"
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
                    />
                    <p className="mt-1 text-xs text-gray-500">
                      {note.length}/2000 characters
                    </p>
                  </div>

                  {error && (
                    <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg">
                      <p className="text-sm text-red-800">{error}</p>
                    </div>
                  )}
                </>
              )}
            </div>

            {!showSuccess && (
              <div className="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse gap-3">
                <Button
                  type="submit"
                  loading={submitMutation.isPending}
                  disabled={submitMutation.isPending}
                >
                  Submit Request
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={handleClose}
                  disabled={submitMutation.isPending}
                >
                  Cancel
                </Button>
              </div>
            )}
          </form>
        </div>
      </div>
    </div>
  );
}
