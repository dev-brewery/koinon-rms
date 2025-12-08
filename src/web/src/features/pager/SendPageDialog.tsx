/**
 * Send Page Dialog Component
 * Modal dialog for sending SMS pages to parents
 */

import { useState } from 'react';
import { useSendPage } from './hooks';
import { PagerMessageType, type PagerAssignment } from './api';
import { Button } from '@/components/ui';

export interface SendPageDialogProps {
  pager: PagerAssignment | null;
  onClose: () => void;
  onSuccess: () => void;
}

const MESSAGE_TEMPLATES: Record<PagerMessageType, string> = {
  [PagerMessageType.PickupNeeded]:
    'Please come pick up your child. They are ready to be picked up from their class.',
  [PagerMessageType.NeedsAttention]:
    'Your child needs your attention. Please come to their classroom.',
  [PagerMessageType.ServiceEnding]:
    'The service is ending. Please proceed to pick up your child.',
  [PagerMessageType.Custom]: '',
};

const MESSAGE_TYPE_LABELS: Record<PagerMessageType, string> = {
  [PagerMessageType.PickupNeeded]: 'Pickup Needed',
  [PagerMessageType.NeedsAttention]: 'Needs Attention',
  [PagerMessageType.ServiceEnding]: 'Service Ending',
  [PagerMessageType.Custom]: 'Custom Message',
};

export function SendPageDialog({ pager, onClose, onSuccess }: SendPageDialogProps) {
  const [messageType, setMessageType] = useState<PagerMessageType>(
    PagerMessageType.PickupNeeded
  );
  const [customMessage, setCustomMessage] = useState('');

  const sendPageMutation = useSendPage();

  const handleClose = () => {
    setMessageType(PagerMessageType.PickupNeeded);
    setCustomMessage('');
    sendPageMutation.reset();
    onClose();
  };

  const handleSend = async () => {
    if (!pager) return;

    try {
      await sendPageMutation.mutateAsync({
        pagerNumber: pager.pagerNumber.toString(),
        messageType,
        customMessage: messageType === PagerMessageType.Custom ? customMessage : undefined,
      });

      // Success - notify parent and close
      onSuccess();
      handleClose();
    } catch (error) {
      // Error is handled by mutation state - no additional logging needed
    }
  };

  const isCustom = messageType === PagerMessageType.Custom;
  const previewMessage = isCustom ? customMessage : MESSAGE_TEMPLATES[messageType];
  const canSend = !isCustom || customMessage.trim().length > 0;

  // Rate limit warning
  const isApproachingLimit = pager && pager.messagesSentCount >= 2;
  const hasReachedLimit = pager && pager.messagesSentCount >= 3;

  if (!pager) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:p-0">
        {/* Backdrop */}
        <div
          className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"
          onClick={handleClose}
        />

        {/* Modal */}
        <div className="relative inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-2xl sm:w-full">
          <div className="bg-white px-6 pt-6 pb-4">
            {/* Header */}
            <div className="flex items-start justify-between mb-6">
              <div className="flex-1">
                <h3 className="text-2xl font-bold text-gray-900 mb-2">Send Page</h3>
                <div className="flex items-center gap-3">
                  <span className="inline-block px-3 py-1 bg-blue-600 text-white font-bold rounded text-lg">
                    P-{pager.pagerNumber}
                  </span>
                  <span className="text-lg font-semibold text-gray-700">
                    {pager.childName}
                  </span>
                </div>
                <div className="text-sm text-gray-600 mt-1">
                  {pager.groupName} · {pager.locationName}
                </div>
                {pager.parentPhoneNumber && (
                  <div className="text-sm text-gray-600 mt-1 flex items-center gap-1">
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
                        d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z"
                      />
                    </svg>
                    {pager.parentPhoneNumber}
                  </div>
                )}
              </div>
              <button
                onClick={handleClose}
                className="text-gray-400 hover:text-gray-500 min-h-[48px] min-w-[48px] flex items-center justify-center"
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

            {/* No Phone Number Warning */}
            {!pager.parentPhoneNumber && (
              <div className="mb-4 p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
                <div className="flex items-start gap-3">
                  <svg
                    className="w-6 h-6 text-yellow-600 flex-shrink-0"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                    />
                  </svg>
                  <div>
                    <p className="font-semibold text-yellow-900">No Phone Number</p>
                    <p className="text-sm text-yellow-800 mt-1">
                      This pager does not have a parent phone number. The message cannot
                      be sent.
                    </p>
                  </div>
                </div>
              </div>
            )}

            {/* Rate Limit Warning */}
            {isApproachingLimit && !hasReachedLimit && (
              <div className="mb-4 p-4 bg-orange-50 border border-orange-200 rounded-lg">
                <div className="flex items-start gap-3">
                  <svg
                    className="w-6 h-6 text-orange-600 flex-shrink-0"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                    />
                  </svg>
                  <div>
                    <p className="font-semibold text-orange-900">Approaching Rate Limit</p>
                    <p className="text-sm text-orange-800 mt-1">
                      {pager.messagesSentCount} message{pager.messagesSentCount === 1 ? '' : 's'}{' '}
                      already sent to this pager. Maximum 3 messages per hour.
                    </p>
                  </div>
                </div>
              </div>
            )}

            {/* Rate Limit Exceeded */}
            {hasReachedLimit && (
              <div className="mb-4 p-4 bg-red-50 border border-red-200 rounded-lg">
                <div className="flex items-start gap-3">
                  <svg
                    className="w-6 h-6 text-red-600 flex-shrink-0"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                    />
                  </svg>
                  <div>
                    <p className="font-semibold text-red-900">Rate Limit Exceeded</p>
                    <p className="text-sm text-red-800 mt-1">
                      Maximum 3 messages per hour has been reached for this pager. Please
                      wait before sending another message.
                    </p>
                  </div>
                </div>
              </div>
            )}

            {/* Message Type Selection */}
            <div className="mb-4">
              <label className="block text-sm font-semibold text-gray-700 mb-3">
                Message Type
              </label>
              <div className="space-y-2">
                {Object.values(PagerMessageType).map((type) => (
                  <label
                    key={type}
                    className={`flex items-start p-3 border-2 rounded-lg cursor-pointer transition-colors min-h-[56px] ${
                      messageType === type
                        ? 'border-blue-500 bg-blue-50'
                        : 'border-gray-200 hover:border-gray-300'
                    }`}
                  >
                    <input
                      type="radio"
                      name="messageType"
                      value={type}
                      checked={messageType === type}
                      onChange={(e) => setMessageType(e.target.value as PagerMessageType)}
                      className="mt-1 h-4 w-4 text-blue-600"
                    />
                    <span className="ml-3 text-base font-medium text-gray-900">
                      {MESSAGE_TYPE_LABELS[type]}
                    </span>
                  </label>
                ))}
              </div>
            </div>

            {/* Custom Message Input */}
            {isCustom && (
              <div className="mb-4">
                <label className="block text-sm font-semibold text-gray-700 mb-2">
                  Custom Message
                </label>
                <textarea
                  value={customMessage}
                  onChange={(e) => setCustomMessage(e.target.value)}
                  placeholder="Enter your custom message..."
                  rows={4}
                  className="w-full px-3 py-2 border-2 border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 text-base"
                  maxLength={160}
                />
                <p className="text-xs text-gray-500 mt-1">
                  {customMessage.length}/160 characters
                </p>
              </div>
            )}

            {/* Message Preview */}
            <div className="mb-6">
              <label className="block text-sm font-semibold text-gray-700 mb-2">
                Message Preview
              </label>
              <div className="p-4 bg-gray-50 border border-gray-200 rounded-lg">
                <p className="text-base text-gray-800">
                  {previewMessage || (
                    <span className="text-gray-400 italic">
                      Enter a custom message above...
                    </span>
                  )}
                </p>
              </div>
            </div>

            {/* Error Display */}
            {sendPageMutation.isError && (
              <div className="mb-4 p-4 bg-red-50 border border-red-200 rounded-lg">
                <p className="text-red-800 text-sm">
                  {sendPageMutation.error instanceof Error
                    ? sendPageMutation.error.message
                    : 'Failed to send page. Please try again.'}
                </p>
              </div>
            )}

            {/* Success Display */}
            {sendPageMutation.isSuccess && (
              <div className="mb-4 p-4 bg-green-50 border border-green-200 rounded-lg">
                <div className="flex items-center gap-2">
                  <svg
                    className="w-5 h-5 text-green-600"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M5 13l4 4L19 7"
                    />
                  </svg>
                  <p className="text-green-800 font-semibold">Page sent successfully!</p>
                </div>
              </div>
            )}
          </div>

          {/* Actions */}
          <div className="bg-gray-50 px-6 py-4 sm:flex sm:flex-row-reverse gap-3">
            <Button
              onClick={handleSend}
              disabled={
                !canSend ||
                !pager.parentPhoneNumber ||
                hasReachedLimit ||
                sendPageMutation.isPending
              }
              loading={sendPageMutation.isPending}
              size="lg"
              className="w-full sm:w-auto"
            >
              Send Page
            </Button>
            <Button
              onClick={handleClose}
              variant="secondary"
              size="lg"
              className="w-full sm:w-auto mt-3 sm:mt-0"
            >
              Cancel
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
