/**
 * Request Review Modal Component
 * Modal for group leaders to approve or deny membership requests
 */

import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/Button';
import type { GroupMemberRequestDto } from '@/services/api/types';
import { formatDate } from '@/lib/utils';

interface RequestReviewModalProps {
  isOpen: boolean;
  onClose: () => void;
  request: GroupMemberRequestDto | null;
  onApprove: (requestIdKey: string, note?: string) => Promise<void>;
  onDeny: (requestIdKey: string, note?: string) => Promise<void>;
}

export function RequestReviewModal({
  isOpen,
  onClose,
  request,
  onApprove,
  onDeny,
}: RequestReviewModalProps) {
  const [responseNote, setResponseNote] = useState('');
  const [isProcessing, setIsProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Reset state when modal opens/closes
  useEffect(() => {
    if (isOpen) {
      setResponseNote('');
      setError(null);
    }
  }, [isOpen]);

  // Add escape key handler
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !isProcessing) {
        onClose();
      }
    };
    if (isOpen) {
      document.addEventListener('keydown', handleEscape);
      return () => document.removeEventListener('keydown', handleEscape);
    }
  }, [isOpen, isProcessing, onClose]);

  const handleApprove = async () => {
    if (!request) return;

    setError(null);
    setIsProcessing(true);

    try {
      await onApprove(request.idKey, responseNote.trim() || undefined);
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to approve request');
    } finally {
      setIsProcessing(false);
    }
  };

  const handleDeny = async () => {
    if (!request) return;

    setError(null);
    setIsProcessing(true);

    try {
      await onDeny(request.idKey, responseNote.trim() || undefined);
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to deny request');
    } finally {
      setIsProcessing(false);
    }
  };

  if (!isOpen || !request) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:p-0">
        {/* Backdrop */}
        <div
          className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"
          onClick={() => !isProcessing && onClose()}
        />

        {/* Modal */}
        <div className="relative inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
          <div className="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
            <div className="flex items-start justify-between mb-4">
              <div>
                <h3 className="text-lg font-medium text-gray-900">
                  Review Membership Request
                </h3>
              </div>
              <button
                onClick={onClose}
                disabled={isProcessing}
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

            {/* Request Details */}
            <div className="space-y-4 mb-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Requester
                </label>
                <p className="text-base text-gray-900">{request.requester.fullName}</p>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Requested
                </label>
                <p className="text-sm text-gray-600">
                  {formatDate(request.createdDateTime)}
                </p>
              </div>

              {request.requestNote && (
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Message from Requester
                  </label>
                  <div className="p-3 bg-gray-50 border border-gray-200 rounded-lg">
                    <p className="text-sm text-gray-900 whitespace-pre-wrap">
                      {request.requestNote}
                    </p>
                  </div>
                </div>
              )}

              <div>
                <label
                  htmlFor="response-note"
                  className="block text-sm font-medium text-gray-700 mb-1"
                >
                  Response Message (optional)
                </label>
                <textarea
                  id="response-note"
                  value={responseNote}
                  onChange={(e) => setResponseNote(e.target.value)}
                  disabled={isProcessing}
                  rows={3}
                  maxLength={2000}
                  placeholder="Add a message to the requester..."
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
                />
                <p className="mt-1 text-xs text-gray-500">
                  {responseNote.length}/2000 characters
                </p>
              </div>

              {error && (
                <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
                  <p className="text-sm text-red-800">{error}</p>
                </div>
              )}
            </div>
          </div>

          {/* Actions */}
          <div className="bg-gray-50 px-4 py-3 sm:px-6 flex flex-col sm:flex-row gap-3">
            <Button
              onClick={handleApprove}
              disabled={isProcessing}
              loading={isProcessing}
              className="flex-1 bg-green-600 hover:bg-green-700 active:bg-green-800"
            >
              Approve Request
            </Button>
            <Button
              onClick={handleDeny}
              disabled={isProcessing}
              variant="outline"
              className="flex-1 border-red-300 text-red-700 hover:bg-red-50 active:bg-red-100"
            >
              Deny Request
            </Button>
            <Button
              variant="ghost"
              onClick={onClose}
              disabled={isProcessing}
              className="sm:w-auto"
            >
              Cancel
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
