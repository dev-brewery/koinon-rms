/**
 * CommunicationPreferences Component
 * Manage Email and SMS opt-in/opt-out preferences for a person
 */

import { useState } from 'react';
import { cn } from '@/lib/utils';
import {
  useCommunicationPreferences,
  useUpdateCommunicationPreference,
} from '@/hooks/useCommunicationPreferences';

interface CommunicationPreferencesProps {
  personIdKey: string;
}

export function CommunicationPreferences({ personIdKey }: CommunicationPreferencesProps) {
  const { data: preferences, isLoading, error } = useCommunicationPreferences(personIdKey);
  const updateMutation = useUpdateCommunicationPreference();

  const [emailReason, setEmailReason] = useState('');
  const [smsReason, setSmsReason] = useState('');
  const [showEmailReason, setShowEmailReason] = useState(false);
  const [showSmsReason, setShowSmsReason] = useState(false);
  const [showSuccessMessage, setShowSuccessMessage] = useState(false);

  if (isLoading) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Communication Preferences</h2>
        <div className="flex items-center justify-center py-8">
          <div className="w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Communication Preferences</h2>
        <p className="text-red-600 text-sm">Failed to load preferences</p>
      </div>
    );
  }

  // Find preferences or use default (opted in)
  const emailPref = preferences?.find((p) => p.communicationType === 'Email');
  const smsPref = preferences?.find((p) => p.communicationType === 'Sms');

  const isEmailOptedIn = !emailPref || !emailPref.isOptedOut;
  const isSmsOptedIn = !smsPref || !smsPref.isOptedOut;

  const handleToggle = async (type: 'Email' | 'Sms', currentlyOptedIn: boolean) => {
    const isOptingOut = currentlyOptedIn; // If opted in, toggling means opting out

    if (isOptingOut) {
      // Show reason input when opting out
      if (type === 'Email') {
        setShowEmailReason(true);
      } else {
        setShowSmsReason(true);
      }
    } else {
      // Opting back in - no reason needed
      await updateMutation.mutateAsync({
        personIdKey,
        type,
        request: {
          isOptedOut: false,
          optOutReason: undefined,
        },
      });

      // Show success message
      setShowSuccessMessage(true);
      setTimeout(() => setShowSuccessMessage(false), 3000);
    }
  };

  const handleConfirmOptOut = async (type: 'Email' | 'Sms') => {
    const reason = type === 'Email' ? emailReason : smsReason;

    await updateMutation.mutateAsync({
      personIdKey,
      type,
      request: {
        isOptedOut: true,
        optOutReason: reason || undefined,
      },
    });

    // Clear state
    if (type === 'Email') {
      setEmailReason('');
      setShowEmailReason(false);
    } else {
      setSmsReason('');
      setShowSmsReason(false);
    }

    // Show success message
    setShowSuccessMessage(true);
    setTimeout(() => setShowSuccessMessage(false), 3000);
  };

  const handleCancelOptOut = (type: 'Email' | 'Sms') => {
    if (type === 'Email') {
      setEmailReason('');
      setShowEmailReason(false);
    } else {
      setSmsReason('');
      setShowSmsReason(false);
    }
  };

  const formatDateTime = (dateTime?: string) => {
    if (!dateTime) return null;
    const date = new Date(dateTime);
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    }).format(date);
  };

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">Communication Preferences</h2>
        {showSuccessMessage && (
          <div className="flex items-center gap-2 px-3 py-1 bg-green-50 text-green-700 rounded-md text-sm">
            <svg
              className="w-4 h-4"
              fill="currentColor"
              viewBox="0 0 20 20"
              aria-hidden="true"
            >
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                clipRule="evenodd"
              />
            </svg>
            <span>Saved</span>
          </div>
        )}
      </div>

      <div className="space-y-6">
        {/* Email Preference */}
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <div className="flex items-center gap-3">
              <svg
                className="w-5 h-5 text-gray-600"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
                />
              </svg>
              <div>
                <h3 className="font-medium text-gray-900">Email</h3>
                {!isEmailOptedIn && emailPref?.optOutDateTime && (
                  <p className="text-xs text-gray-500 mt-1">
                    Opted out on {formatDateTime(emailPref.optOutDateTime)}
                  </p>
                )}
                {!isEmailOptedIn && emailPref?.optOutReason && (
                  <p className="text-xs text-gray-600 mt-1">
                    Reason: {emailPref.optOutReason}
                  </p>
                )}
              </div>
            </div>
          </div>

          <button
            type="button"
            onClick={() => handleToggle('Email', isEmailOptedIn)}
            disabled={updateMutation.isPending || showEmailReason}
            className={cn(
              'relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-primary-600 focus:ring-offset-2',
              isEmailOptedIn ? 'bg-primary-600' : 'bg-gray-200',
              (updateMutation.isPending || showEmailReason) && 'opacity-50 cursor-not-allowed'
            )}
          >
            <span className="sr-only">Toggle email preference</span>
            <span
              className={cn(
                'pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out',
                isEmailOptedIn ? 'translate-x-5' : 'translate-x-0'
              )}
            />
          </button>
        </div>

        {/* Email opt-out reason input */}
        {showEmailReason && (
          <div className="ml-8 space-y-3 bg-gray-50 p-4 rounded-lg" aria-live="polite">
            <label className="block text-sm font-medium text-gray-700">
              Reason for opting out (optional)
            </label>
            <input
              type="text"
              value={emailReason}
              onChange={(e) => setEmailReason(e.target.value)}
              placeholder="e.g., Prefers SMS, Email address changed"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-primary-600"
            />
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => handleConfirmOptOut('Email')}
                disabled={updateMutation.isPending}
                className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50"
              >
                Confirm Opt-Out
              </button>
              <button
                type="button"
                onClick={() => handleCancelOptOut('Email')}
                disabled={updateMutation.isPending}
                className="px-4 py-2 bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 transition-colors disabled:opacity-50"
              >
                Cancel
              </button>
            </div>
          </div>
        )}

        {/* SMS Preference */}
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <div className="flex items-center gap-3">
              <svg
                className="w-5 h-5 text-gray-600"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 18h.01M8 21h8a2 2 0 002-2V5a2 2 0 00-2-2H8a2 2 0 00-2 2v14a2 2 0 002 2z"
                />
              </svg>
              <div>
                <h3 className="font-medium text-gray-900">SMS</h3>
                {!isSmsOptedIn && smsPref?.optOutDateTime && (
                  <p className="text-xs text-gray-500 mt-1">
                    Opted out on {formatDateTime(smsPref.optOutDateTime)}
                  </p>
                )}
                {!isSmsOptedIn && smsPref?.optOutReason && (
                  <p className="text-xs text-gray-600 mt-1">
                    Reason: {smsPref.optOutReason}
                  </p>
                )}
              </div>
            </div>
          </div>

          <button
            type="button"
            onClick={() => handleToggle('Sms', isSmsOptedIn)}
            disabled={updateMutation.isPending || showSmsReason}
            className={cn(
              'relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-primary-600 focus:ring-offset-2',
              isSmsOptedIn ? 'bg-primary-600' : 'bg-gray-200',
              (updateMutation.isPending || showSmsReason) && 'opacity-50 cursor-not-allowed'
            )}
          >
            <span className="sr-only">Toggle SMS preference</span>
            <span
              className={cn(
                'pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out',
                isSmsOptedIn ? 'translate-x-5' : 'translate-x-0'
              )}
            />
          </button>
        </div>

        {/* SMS opt-out reason input */}
        {showSmsReason && (
          <div className="ml-8 space-y-3 bg-gray-50 p-4 rounded-lg" aria-live="polite">
            <label className="block text-sm font-medium text-gray-700">
              Reason for opting out (optional)
            </label>
            <input
              type="text"
              value={smsReason}
              onChange={(e) => setSmsReason(e.target.value)}
              placeholder="e.g., No longer has this number, Prefers email"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-primary-600"
            />
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => handleConfirmOptOut('Sms')}
                disabled={updateMutation.isPending}
                className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50"
              >
                Confirm Opt-Out
              </button>
              <button
                type="button"
                onClick={() => handleCancelOptOut('Sms')}
                disabled={updateMutation.isPending}
                className="px-4 py-2 bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 transition-colors disabled:opacity-50"
              >
                Cancel
              </button>
            </div>
          </div>
        )}

        <div className="pt-4 border-t border-gray-200">
          <p className="text-xs text-gray-500">
            Toggle switches show current preferences. When enabled (blue), the person will receive
            that type of communication.
          </p>
        </div>
      </div>
    </div>
  );
}
