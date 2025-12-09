/**
 * Confirm/Decline Modal Component
 * Modal dialog for confirming or declining a volunteer assignment
 */

import { useState } from 'react';
import { Button } from '@/components/ui/Button';
import { VolunteerScheduleStatus } from '@/types/volunteer';

interface ConfirmDeclineModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (status: VolunteerScheduleStatus, declineReason?: string) => void;
  isSubmitting?: boolean;
  action: 'confirm' | 'decline';
  assignmentInfo?: {
    memberName: string;
    scheduleName: string;
    date: string;
  };
}

export function ConfirmDeclineModal({
  isOpen,
  onClose,
  onSubmit,
  isSubmitting = false,
  action,
  assignmentInfo,
}: ConfirmDeclineModalProps) {
  const [declineReason, setDeclineReason] = useState('');

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (action === 'confirm') {
      onSubmit(VolunteerScheduleStatus.Confirmed);
    } else {
      onSubmit(VolunteerScheduleStatus.Declined, declineReason || undefined);
    }

    // Reset form
    setDeclineReason('');
  };

  const handleClose = () => {
    setDeclineReason('');
    onClose();
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex min-h-screen items-center justify-center p-4">
        {/* Backdrop */}
        <div
          className="fixed inset-0 bg-black bg-opacity-30 transition-opacity"
          onClick={handleClose}
        />

        {/* Modal */}
        <div className="relative bg-white rounded-lg shadow-xl max-w-md w-full p-6">
          <form onSubmit={handleSubmit}>
            <h2 className="text-xl font-bold text-gray-900 mb-4">
              {action === 'confirm' ? 'Confirm Assignment' : 'Decline Assignment'}
            </h2>

            {assignmentInfo && (
              <div className="mb-4 p-4 bg-gray-50 rounded-lg">
                <p className="text-sm text-gray-600">
                  <span className="font-medium">Volunteer:</span> {assignmentInfo.memberName}
                </p>
                <p className="text-sm text-gray-600">
                  <span className="font-medium">Schedule:</span> {assignmentInfo.scheduleName}
                </p>
                <p className="text-sm text-gray-600">
                  <span className="font-medium">Date:</span> {formatDate(assignmentInfo.date)}
                </p>
              </div>
            )}

            {action === 'confirm' ? (
              <p className="text-gray-700 mb-6">
                Are you sure you want to confirm this assignment?
              </p>
            ) : (
              <div className="mb-6">
                <label
                  htmlFor="declineReason"
                  className="block text-sm font-medium text-gray-700 mb-2"
                >
                  Reason for declining (optional)
                </label>
                <textarea
                  id="declineReason"
                  rows={4}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  placeholder="Enter your reason for declining..."
                  value={declineReason}
                  onChange={(e) => setDeclineReason(e.target.value)}
                  disabled={isSubmitting}
                />
              </div>
            )}

            <div className="flex gap-3 justify-end">
              <Button
                type="button"
                variant="outline"
                onClick={handleClose}
                disabled={isSubmitting}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                variant={action === 'confirm' ? 'primary' : 'secondary'}
                loading={isSubmitting}
              >
                {action === 'confirm' ? 'Confirm' : 'Decline'}
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
