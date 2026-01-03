/**
 * EmailComposer Component
 * Form fields for composing email messages
 */

import { useRef } from 'react';
import { flushSync } from 'react-dom';
import { MergeFieldPicker } from './MergeFieldPicker';

interface EmailComposerProps {
  subject: string;
  body: string;
  fromEmail?: string;
  fromName?: string;
  replyToEmail?: string;
  onSubjectChange: (value: string) => void;
  onBodyChange: (value: string) => void;
  onFromEmailChange: (value: string) => void;
  onFromNameChange: (value: string) => void;
  onReplyToEmailChange: (value: string) => void;
  errors?: {
    subject?: string;
    body?: string;
    fromEmail?: string;
    fromName?: string;
    replyToEmail?: string;
  };
}

/**
 * Helper function to insert a token at the cursor position in an input or textarea
 * Uses flushSync to prevent race conditions between state update and cursor positioning
 */
const insertAtCursor = (
  ref: React.RefObject<HTMLInputElement | HTMLTextAreaElement>,
  currentValue: string,
  onChange: (value: string) => void,
  token: string
) => {
  const element = ref.current;
  if (element) {
    const start = element.selectionStart || 0;
    const end = element.selectionEnd || 0;
    const newValue = currentValue.slice(0, start) + token + currentValue.slice(end);
    
    flushSync(() => {
      onChange(newValue);
    });
    
    element.selectionStart = element.selectionEnd = start + token.length;
    element.focus();
  } else {
    onChange(currentValue + token);
  }
};

export function EmailComposer({
  subject,
  body,
  fromEmail,
  fromName,
  replyToEmail,
  onSubjectChange,
  onBodyChange,
  onFromEmailChange,
  onFromNameChange,
  onReplyToEmailChange,
  errors = {},
}: EmailComposerProps) {
  const subjectRef = useRef<HTMLInputElement>(null);
  const bodyRef = useRef<HTMLTextAreaElement>(null);

  const handleInsertSubjectField = (token: string) => {
    insertAtCursor(subjectRef, subject, onSubjectChange, token);
  };

  const handleInsertBodyField = (token: string) => {
    insertAtCursor(bodyRef, body, onBodyChange, token);
  };

  return (
    <div className="space-y-4">
      {/* From Name */}
      <div>
        <label htmlFor="fromName" className="block text-sm font-medium text-gray-700 mb-1">
          From Name
        </label>
        <input
          type="text"
          id="fromName"
          value={fromName || ''}
          onChange={(e) => onFromNameChange(e.target.value)}
          className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
            errors.fromName ? 'border-red-500' : 'border-gray-300'
          }`}
          placeholder="Your Name or Organization"
        />
        {errors.fromName && <p className="mt-1 text-sm text-red-600">{errors.fromName}</p>}
      </div>

      {/* From Email */}
      <div>
        <label htmlFor="fromEmail" className="block text-sm font-medium text-gray-700 mb-1">
          From Email
        </label>
        <input
          type="email"
          id="fromEmail"
          value={fromEmail || ''}
          onChange={(e) => onFromEmailChange(e.target.value)}
          className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
            errors.fromEmail ? 'border-red-500' : 'border-gray-300'
          }`}
          placeholder="sender@example.com"
        />
        {errors.fromEmail && <p className="mt-1 text-sm text-red-600">{errors.fromEmail}</p>}
      </div>

      {/* Reply-To Email */}
      <div>
        <label htmlFor="replyToEmail" className="block text-sm font-medium text-gray-700 mb-1">
          Reply-To Email
        </label>
        <input
          type="email"
          id="replyToEmail"
          value={replyToEmail || ''}
          onChange={(e) => onReplyToEmailChange(e.target.value)}
          className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
            errors.replyToEmail ? 'border-red-500' : 'border-gray-300'
          }`}
          placeholder="reply@example.com"
        />
        {errors.replyToEmail && <p className="mt-1 text-sm text-red-600">{errors.replyToEmail}</p>}
      </div>

      {/* Subject */}
      <div>
        <div className="flex items-center justify-between mb-1">
          <label htmlFor="subject" className="block text-sm font-medium text-gray-700">
            Subject <span className="text-red-500">*</span>
          </label>
          <MergeFieldPicker onInsert={handleInsertSubjectField} />
        </div>
        <input
          type="text"
          id="subject"
          ref={subjectRef}
          value={subject}
          onChange={(e) => onSubjectChange(e.target.value)}
          className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
            errors.subject ? 'border-red-500' : 'border-gray-300'
          }`}
          placeholder="Email subject line"
        />
        {errors.subject && <p className="mt-1 text-sm text-red-600">{errors.subject}</p>}
      </div>

      {/* Body */}
      <div>
        <div className="flex items-center justify-between mb-1">
          <label htmlFor="email-body" className="block text-sm font-medium text-gray-700">
            Message <span className="text-red-500">*</span>
          </label>
          <MergeFieldPicker onInsert={handleInsertBodyField} />
        </div>
        <textarea
          id="email-body"
          ref={bodyRef}
          value={body}
          onChange={(e) => onBodyChange(e.target.value)}
          rows={10}
          className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
            errors.body ? 'border-red-500' : 'border-gray-300'
          }`}
          placeholder="Enter your email message..."
        />
        {errors.body && <p className="mt-1 text-sm text-red-600">{errors.body}</p>}
      </div>
    </div>
  );
}
