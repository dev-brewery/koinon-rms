/**
 * SaveAsTemplateModal Component
 * Modal for saving current communication as a reusable template
 */

import { useState, useEffect, useCallback } from 'react';
import { useCreateCommunicationTemplate } from '@/hooks/useCommunicationTemplates';
import { useToast } from '@/contexts/ToastContext';

interface SaveAsTemplateModalProps {
  isOpen: boolean;
  onClose: () => void;
  communicationType: 'Email' | 'Sms';
  subject?: string;
  body: string;
}

export function SaveAsTemplateModal({
  isOpen,
  onClose,
  communicationType,
  subject,
  body,
}: SaveAsTemplateModalProps) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});

  const createMutation = useCreateCommunicationTemplate();
  const { success, error: showError } = useToast();

  // Reset state when modal opens
  useEffect(() => {
    if (isOpen) {
      setName('');
      setDescription('');
      setErrors({});
    }
  }, [isOpen]);

  // Memoized close handler
  const handleClose = useCallback(() => {
    if (!createMutation.isPending) {
      onClose();
    }
  }, [createMutation.isPending, onClose]);

  // Escape key handler
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !createMutation.isPending) {
        handleClose();
      }
    };
    if (isOpen) {
      document.addEventListener('keydown', handleEscape);
      return () => document.removeEventListener('keydown', handleEscape);
    }
  }, [isOpen, createMutation.isPending, handleClose]);

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!name.trim()) {
      newErrors.name = 'Template name is required';
    }

    if (!body.trim()) {
      newErrors.body = 'Template body cannot be empty';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Client-side validation before submitting
    if (!name.trim() || !body.trim()) {
      const newErrors: Record<string, string> = {};
      if (!name.trim()) {
        newErrors.name = 'Template name is required';
      }
      if (!body.trim()) {
        newErrors.body = 'Template body cannot be empty';
      }
      setErrors(newErrors);
      return;
    }

    if (!validateForm()) {
      return;
    }

    try {
      await createMutation.mutateAsync({
        name: name.trim(),
        description: description.trim() || undefined,
        communicationType,
        subject: communicationType === 'Email' && subject ? subject.trim() : undefined,
        body: body.trim(),
      });

      success('Template Saved', `Template "${name}" has been created successfully`);
      handleClose();
    } catch (err) {
      showError(
        'Save Failed',
        err instanceof Error ? err.message : 'Failed to save template'
      );
    }
  };

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
                  <h3 className="text-lg font-medium text-gray-900">Save as Template</h3>
                  <p className="text-sm text-gray-500 mt-1">
                    Create a reusable template from this {communicationType.toLowerCase()}
                  </p>
                </div>
                <button
                  type="button"
                  onClick={handleClose}
                  disabled={createMutation.isPending}
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

              {/* Template Name */}
              <div className="mb-4">
                <label htmlFor="template-name" className="block text-sm font-medium text-gray-700 mb-1">
                  Template Name <span className="text-red-500">*</span>
                </label>
                <input
                  id="template-name"
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  disabled={createMutation.isPending}
                  maxLength={200}
                  placeholder="e.g., Weekly Newsletter, Event Reminder"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
                />
                {errors.name && <p className="mt-1 text-sm text-red-600">{errors.name}</p>}
              </div>

              {/* Description */}
              <div className="mb-4">
                <label htmlFor="template-description" className="block text-sm font-medium text-gray-700 mb-1">
                  Description (optional)
                </label>
                <textarea
                  id="template-description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  disabled={createMutation.isPending}
                  rows={3}
                  maxLength={1000}
                  placeholder="Describe when to use this template..."
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
                />
              </div>

              {/* Preview */}
              <div className="mb-4 p-3 bg-gray-50 border border-gray-200 rounded-lg">
                <p className="text-xs font-medium text-gray-700 mb-2">Template Preview:</p>
                {communicationType === 'Email' && subject && (
                  <p className="text-sm text-gray-900 font-medium mb-1">Subject: {subject}</p>
                )}
                <p className="text-sm text-gray-600 line-clamp-3">{body || '(empty body)'}</p>
              </div>

              {errors.body && (
                <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg">
                  <p className="text-sm text-red-800">{errors.body}</p>
                </div>
              )}
            </div>

            {/* Actions */}
            <div className="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse gap-3">
              <button
                type="submit"
                disabled={createMutation.isPending}
                className="w-full sm:w-auto px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-50 transition-colors"
              >
                {createMutation.isPending ? 'Saving...' : 'Save Template'}
              </button>
              <button
                type="button"
                onClick={handleClose}
                disabled={createMutation.isPending}
                className="w-full sm:w-auto mt-3 sm:mt-0 px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 transition-colors"
              >
                Cancel
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
