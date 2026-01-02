/**
 * CommunicationComposer Component
 * Full composer modal for creating and sending communications
 */

import { useState, useCallback, useMemo } from 'react';
import { MessageTypeToggle } from './MessageTypeToggle';
import { EmailComposer } from './EmailComposer';
import { SmsComposer } from './SmsComposer';
import { TemplateSelector } from './TemplateSelector';
import { SaveAsTemplateModal } from './SaveAsTemplateModal';
import { useCreateCommunication, useSendCommunication } from '@/hooks/useCommunications';
import type { GroupSummaryDto } from '@/services/api/types';
import type { CreateCommunicationRequest } from '@/services/api/communications';

interface CommunicationComposerProps {
  groups: GroupSummaryDto[];
  onSend: () => void;
  onClose: () => void;
}

export function CommunicationComposer({ groups, onSend, onClose }: CommunicationComposerProps) {
  const [communicationType, setCommunicationType] = useState<'Email' | 'Sms'>('Email');
  const [subject, setSubject] = useState('');
  const [body, setBody] = useState('');
  const [fromEmail, setFromEmail] = useState('');
  const [fromName, setFromName] = useState('');
  const [replyToEmail, setReplyToEmail] = useState('');
  const [note, setNote] = useState('');
  const [selectedGroupIdKeys, setSelectedGroupIdKeys] = useState<string[]>([]);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSaveTemplateModalOpen, setIsSaveTemplateModalOpen] = useState(false);

  const createMutation = useCreateCommunication();
  const sendMutation = useSendCommunication();

  // Calculate recipient count (simplified - just sum of group members)
  const recipientCount = useMemo(
    () =>
      selectedGroupIdKeys.reduce((sum, idKey) => {
        const group = groups.find((g) => g.idKey === idKey);
        return sum + (group?.memberCount || 0);
      }, 0),
    [selectedGroupIdKeys, groups]
  );

  const validateForm = useCallback((): boolean => {
    const newErrors: Record<string, string> = {};

    if (selectedGroupIdKeys.length === 0) {
      newErrors.groups = 'Please select at least one group';
    }

    if (!body.trim()) {
      newErrors.body = 'Message body is required';
    }

    if (communicationType === 'Email') {
      if (!subject.trim()) {
        newErrors.subject = 'Subject is required for emails';
      }
    }

    if (communicationType === 'Sms' && body.length > 1600) {
      newErrors.body = 'SMS message is too long (max 1600 characters)';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }, [communicationType, subject, body, selectedGroupIdKeys]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    try {
      const request: CreateCommunicationRequest = {
        communicationType,
        subject: communicationType === 'Email' ? subject : undefined,
        body,
        fromEmail: communicationType === 'Email' && fromEmail ? fromEmail : undefined,
        fromName: communicationType === 'Email' && fromName ? fromName : undefined,
        replyToEmail: communicationType === 'Email' && replyToEmail ? replyToEmail : undefined,
        note: note || undefined,
        groupIdKeys: selectedGroupIdKeys,
      };

      const communication = await createMutation.mutateAsync(request);
      await sendMutation.mutateAsync(communication.idKey);

      onSend();
    } catch {
      // Error is captured by mutation state and displayed in UI
    }
  };

  const handleGroupToggle = (idKey: string) => {
    setSelectedGroupIdKeys((prev) =>
      prev.includes(idKey) ? prev.filter((id) => id !== idKey) : [...prev, idKey]
    );
  };

  const handleTemplateSelect = (template: { subject?: string; body: string }) => {
    if (template.subject !== undefined) {
      setSubject(template.subject);
    }
    setBody(template.body);
  };

  const isPending = createMutation.isPending || sendMutation.isPending;
  const error = createMutation.error || sendMutation.error;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div className="bg-white rounded-lg shadow-xl max-w-3xl w-full max-h-[90vh] overflow-y-auto">
        <div className="sticky top-0 bg-white border-b border-gray-200 px-6 py-4">
          <div className="flex items-center justify-between">
            <h2 className="text-2xl font-bold text-gray-900">New Communication</h2>
            <button
              onClick={onClose}
              className="text-gray-400 hover:text-gray-600 transition-colors"
              aria-label="Close"
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M6 18L18 6M6 6l12 12"
                />
              </svg>
            </button>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-6">
          {/* Message Type Toggle */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Communication Type
            </label>
            <MessageTypeToggle value={communicationType} onChange={setCommunicationType} />
          </div>

          {/* Template Selector */}
          <TemplateSelector
            communicationType={communicationType}
            onSelect={handleTemplateSelect}
            disabled={isPending}
          />

          {/* Group Selection */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Send To <span className="text-red-500">*</span>
            </label>
            <div className="border border-gray-300 rounded-lg max-h-48 overflow-y-auto">
              {groups.length === 0 ? (
                <div className="p-4 text-center text-gray-500">No groups available</div>
              ) : (
                <div className="divide-y divide-gray-200">
                  {groups.map((group) => (
                    <label
                      key={group.idKey}
                      className="flex items-center gap-3 p-3 hover:bg-gray-50 cursor-pointer"
                    >
                      <input
                        type="checkbox"
                        checked={selectedGroupIdKeys.includes(group.idKey)}
                        onChange={() => handleGroupToggle(group.idKey)}
                        className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                      />
                      <div className="flex-1">
                        <div className="font-medium text-gray-900">{group.name}</div>
                        <div className="text-sm text-gray-500">
                          {group.memberCount} {group.memberCount === 1 ? 'member' : 'members'}
                        </div>
                      </div>
                    </label>
                  ))}
                </div>
              )}
            </div>
            {errors.groups && <p className="mt-1 text-sm text-red-600">{errors.groups}</p>}

            {/* Recipient Count Preview */}
            {selectedGroupIdKeys.length > 0 && (
              <div className="mt-2 flex items-center gap-2 text-sm text-gray-600">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
                  />
                </svg>
                <span className="font-medium">
                  {recipientCount} {recipientCount === 1 ? 'recipient' : 'recipients'}
                </span>
              </div>
            )}
          </div>

          {/* Message Composer */}
          {communicationType === 'Email' ? (
            <EmailComposer
              subject={subject}
              body={body}
              fromEmail={fromEmail}
              fromName={fromName}
              replyToEmail={replyToEmail}
              onSubjectChange={setSubject}
              onBodyChange={setBody}
              onFromEmailChange={setFromEmail}
              onFromNameChange={setFromName}
              onReplyToEmailChange={setReplyToEmail}
              errors={errors}
            />
          ) : (
            <SmsComposer value={body} onChange={setBody} error={errors.body} />
          )}

          {/* Note */}
          <div>
            <label htmlFor="note" className="block text-sm font-medium text-gray-700 mb-1">
              Internal Note (optional)
            </label>
            <textarea
              id="note"
              value={note}
              onChange={(e) => setNote(e.target.value)}
              rows={2}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              placeholder="Add an internal note about this communication..."
            />
          </div>

          {/* Error Display */}
          {error && (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
              <div className="flex items-center gap-2">
                <svg
                  className="w-5 h-5 text-red-600"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                  />
                </svg>
                <p className="text-sm font-medium text-red-800">
                  Failed to send communication. Please try again.
                </p>
              </div>
            </div>
          )}

          {/* Actions */}
          <div className="flex justify-between items-center gap-3 pt-4 border-t border-gray-200">
            <button
              type="button"
              onClick={() => setIsSaveTemplateModalOpen(true)}
              disabled={isPending || !body.trim()}
              className="px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 transition-colors"
            >
              Save as Template
            </button>
            <div className="flex gap-3">
              <button
                type="button"
                onClick={onClose}
                disabled={isPending}
                className="px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 transition-colors"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isPending}
                className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-50 transition-colors"
              >
                {isPending ? 'Sending...' : 'Send'}
              </button>
            </div>
          </div>
        </form>
      </div>

      {/* Save As Template Modal */}
      <SaveAsTemplateModal
        isOpen={isSaveTemplateModalOpen}
        onClose={() => setIsSaveTemplateModalOpen(false)}
        communicationType={communicationType}
        subject={subject}
        body={body}
      />
    </div>
  );
}
