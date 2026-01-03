/**
 * MessagePreview Component
 * Displays a preview of communication message with merge fields replaced using sample data
 */

import { useState, useEffect } from 'react';
import { post } from '@/services/api/client';
import { cn } from '@/lib/utils';

// ============================================================================
// Types
// ============================================================================

interface MessagePreviewProps {
  subject?: string;
  body: string;
  communicationType: 'Email' | 'Sms';
  onClose: () => void;
}

interface PreviewResponse {
  data: {
    subject?: string;
    body: string;
  };
}

// ============================================================================
// Component
// ============================================================================

export function MessagePreview({
  subject,
  body,
  communicationType,
  onClose,
}: MessagePreviewProps) {
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [renderedSubject, setRenderedSubject] = useState<string>('');
  const [renderedBody, setRenderedBody] = useState<string>('');

  useEffect(() => {
    const fetchPreview = async () => {
      setIsLoading(true);
      setError(null);

      try {
        const response = await post<PreviewResponse>('/communications/preview', {
          subject: communicationType === 'Email' ? subject : undefined,
          body,
          communicationType,
        });

        setRenderedSubject(response.data.subject || '');
        setRenderedBody(response.data.body);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load preview');
      } finally {
        setIsLoading(false);
      }
    };

    fetchPreview();
  }, [subject, body, communicationType]);

  // Prevent body scroll when preview is open
  useEffect(() => {
    document.body.style.overflow = 'hidden';
    return () => {
      document.body.style.overflow = '';
    };
  }, []);

  // Handle escape key
  useEffect(() => {
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose();
      }
    };

    document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  }, [onClose]);

  const handleBackdropClick = (event: React.MouseEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget) {
      onClose();
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
      onClick={handleBackdropClick}
      aria-labelledby="preview-title"
      aria-modal="true"
      role="dialog"
    >
      {/* Backdrop */}
      <div className="fixed inset-0 bg-black/50 transition-opacity" aria-hidden="true" />

      {/* Preview Dialog */}
      <div className="relative bg-white rounded-lg shadow-xl max-w-3xl w-full max-h-[90vh] overflow-hidden flex flex-col">
        {/* Header */}
        <div className="sticky top-0 bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="flex-shrink-0 w-10 h-10 rounded-full bg-blue-100 flex items-center justify-center">
              <svg
                className="w-5 h-5 text-blue-600"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M15 12a3 3 0 11-6 0 3 3 0 016 0z M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                />
              </svg>
            </div>
            <div>
              <h2 id="preview-title" className="text-xl font-bold text-gray-900">
                Message Preview
              </h2>
              <p className="text-sm text-gray-600">
                {communicationType === 'Email' ? 'Email' : 'SMS'} with sample merge field data
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 transition-colors"
            aria-label="Close preview"
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

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6">
          {/* Loading State */}
          {isLoading && (
            <div className="flex items-center justify-center py-12">
              <div className="flex flex-col items-center gap-3">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600" />
                <p className="text-sm text-gray-600">Loading preview...</p>
              </div>
            </div>
          )}

          {/* Error State */}
          {error && !isLoading && (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
              <div className="flex items-center gap-2">
                <svg
                  className="w-5 h-5 text-red-600 flex-shrink-0"
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
                <div>
                  <p className="text-sm font-medium text-red-800">Failed to load preview</p>
                  <p className="text-sm text-red-700 mt-1">{error}</p>
                </div>
              </div>
            </div>
          )}

          {/* Preview Content */}
          {!isLoading && !error && (
            <div className="space-y-4">
              {/* Email Preview */}
              {communicationType === 'Email' && (
                <div className="border border-gray-200 rounded-lg overflow-hidden">
                  {/* Email Header */}
                  <div className="bg-gray-50 border-b border-gray-200 p-4">
                    <div className="space-y-2">
                      <div className="flex items-start gap-2">
                        <span className="text-sm font-medium text-gray-600 min-w-[70px]">
                          Subject:
                        </span>
                        <span className="text-sm text-gray-900 font-medium flex-1">
                          {renderedSubject || '(no subject)'}
                        </span>
                      </div>
                      <div className="flex items-start gap-2">
                        <span className="text-sm font-medium text-gray-600 min-w-[70px]">
                          To:
                        </span>
                        <span className="text-sm text-gray-700 flex-1">
                          Sample Recipient &lt;sample@example.com&gt;
                        </span>
                      </div>
                    </div>
                  </div>

                  {/* Email Body */}
                  <div className="p-6 bg-white">
                    <div
                      className={cn(
                        'prose prose-sm max-w-none',
                        'prose-headings:text-gray-900 prose-p:text-gray-700',
                        'whitespace-pre-wrap break-words'
                      )}
                    >
                      {renderedBody}
                    </div>
                  </div>
                </div>
              )}

              {/* SMS Preview */}
              {communicationType === 'Sms' && (
                <div className="max-w-md mx-auto">
                  {/* Phone Frame */}
                  <div className="bg-gray-100 rounded-3xl p-4 shadow-lg">
                    <div className="bg-white rounded-2xl overflow-hidden shadow-sm">
                      {/* Chat Header */}
                      <div className="bg-gray-50 border-b border-gray-200 px-4 py-3">
                        <div className="flex items-center gap-3">
                          <div className="w-8 h-8 rounded-full bg-primary-500 flex items-center justify-center">
                            <svg
                              className="w-5 h-5 text-white"
                              fill="none"
                              stroke="currentColor"
                              viewBox="0 0 24 24"
                            >
                              <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth={2}
                                d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                              />
                            </svg>
                          </div>
                          <div className="flex-1">
                            <p className="text-sm font-medium text-gray-900">Your Organization</p>
                            <p className="text-xs text-gray-500">SMS Message</p>
                          </div>
                        </div>
                      </div>

                      {/* Message Bubble */}
                      <div className="p-4 min-h-[200px] bg-gradient-to-b from-gray-50 to-white">
                        <div className="flex justify-start">
                          <div className="max-w-[85%]">
                            <div className="bg-gray-200 rounded-2xl rounded-tl-sm px-4 py-2 shadow-sm">
                              <p className="text-sm text-gray-900 whitespace-pre-wrap break-words">
                                {renderedBody}
                              </p>
                            </div>
                            <p className="text-xs text-gray-500 mt-1 ml-2">Just now</p>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* Character Count Info */}
                  <div className="mt-4 text-center">
                    <p className="text-xs text-gray-500">
                      Preview shows how recipients will see the message
                    </p>
                  </div>
                </div>
              )}

              {/* Info Notice */}
              <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                <div className="flex items-start gap-2">
                  <svg
                    className="w-5 h-5 text-blue-600 flex-shrink-0 mt-0.5"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                    />
                  </svg>
                  <div className="flex-1">
                    <p className="text-sm font-medium text-blue-900">About this preview</p>
                    <p className="text-sm text-blue-800 mt-1">
                      This preview uses sample data for merge fields. The actual message will use
                      real recipient data when sent.
                    </p>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="sticky bottom-0 bg-white border-t border-gray-200 px-6 py-4">
          <div className="flex justify-end">
            <button
              onClick={onClose}
              className="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors font-medium"
            >
              Close Preview
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
