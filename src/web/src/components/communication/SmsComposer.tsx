/**
 * SmsComposer Component
 * Text area for composing SMS messages with character counting
 */

import { useMemo } from 'react';

interface SmsComposerProps {
  value: string;
  onChange: (value: string) => void;
  error?: string;
}

const SMS_SEGMENT_SIZE = 160;
const SMS_MAX_LENGTH = 1600;

export function SmsComposer({ value, onChange, error }: SmsComposerProps) {
  const characterCount = value.length;
  const segments = Math.ceil(characterCount / SMS_SEGMENT_SIZE) || 1;
  const isTooLong = characterCount > SMS_MAX_LENGTH;
  const isMultiSegment = characterCount > SMS_SEGMENT_SIZE;

  const warningMessage = useMemo(() => {
    if (isTooLong) {
      return `Message too long (max ${SMS_MAX_LENGTH} characters)`;
    }
    if (isMultiSegment) {
      return `This message will be sent as ${segments} segments`;
    }
    return null;
  }, [isTooLong, isMultiSegment, segments]);

  return (
    <div className="space-y-2">
      <label htmlFor="sms-body" className="block text-sm font-medium text-gray-700">
        Message <span className="text-red-500">*</span>
      </label>
      <textarea
        id="sms-body"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        rows={6}
        maxLength={SMS_MAX_LENGTH}
        className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
          error || isTooLong ? 'border-red-500' : 'border-gray-300'
        }`}
        placeholder="Enter your SMS message..."
      />

      {/* Character Counter */}
      <div className="flex items-center justify-between text-sm">
        <span className={`font-medium ${isTooLong ? 'text-red-600' : 'text-gray-600'}`}>
          {characterCount} / {SMS_SEGMENT_SIZE}
          {isMultiSegment && ` (${segments} segments)`}
        </span>
      </div>

      {/* Warning Message */}
      {warningMessage && (
        <div
          className={`flex items-center gap-2 p-3 rounded-lg ${
            isTooLong
              ? 'bg-red-50 border border-red-200'
              : 'bg-amber-50 border border-amber-200'
          }`}
        >
          <svg
            className={`w-5 h-5 flex-shrink-0 ${isTooLong ? 'text-red-600' : 'text-amber-600'}`}
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
          <p className={`text-sm font-medium ${isTooLong ? 'text-red-800' : 'text-amber-800'}`}>
            {warningMessage}
          </p>
        </div>
      )}

      {/* Error Message */}
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  );
}
