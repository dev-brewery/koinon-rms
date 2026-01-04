/**
 * NotificationSettingsSection
 * Email/SMS opt-in/out toggles
 */

import { useState, useEffect } from 'react';
import { useAuth } from '@/hooks/useAuth';
import { useCommunicationPreferences, useBulkUpdateCommunicationPreferences } from '@/hooks/useCommunicationPreferences';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Loading } from '@/components/ui/Loading';
import { ErrorState } from '@/components/ui/ErrorState';

export function NotificationSettingsSection() {
  const { user } = useAuth();
  const { data: preferences, isLoading, error } = useCommunicationPreferences(user?.idKey);
  const updatePreferences = useBulkUpdateCommunicationPreferences();

  const [emailOptedOut, setEmailOptedOut] = useState(false);
  const [smsOptedOut, setSmsOptedOut] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);

  useEffect(() => {
    if (preferences) {
      const emailPref = preferences.find(p => p.communicationType === 'Email');
      const smsPref = preferences.find(p => p.communicationType === 'Sms');
      
      setEmailOptedOut(emailPref?.isOptedOut || false);
      setSmsOptedOut(smsPref?.isOptedOut || false);
    }
  }, [preferences]);

  const handleToggle = (type: 'email' | 'sms', value: boolean) => {
    if (type === 'email') {
      setEmailOptedOut(value);
    } else {
      setSmsOptedOut(value);
    }
    setHasChanges(true);
  };

  const handleSave = async () => {
    if (!user?.idKey) return;

    try {
      await updatePreferences.mutateAsync({
        personIdKey: user.idKey,
        request: {
          preferences: [
            {
              communicationType: 'Email',
              isOptedOut: emailOptedOut,
            },
            {
              communicationType: 'Sms',
              isOptedOut: smsOptedOut,
            },
          ],
        },
      });
      setHasChanges(false);
    } catch (error) {
      // Error handled by mutation
    }
  };

  const handleReset = () => {
    if (preferences) {
      const emailPref = preferences.find(p => p.communicationType === 'Email');
      const smsPref = preferences.find(p => p.communicationType === 'Sms');
      
      setEmailOptedOut(emailPref?.isOptedOut || false);
      setSmsOptedOut(smsPref?.isOptedOut || false);
      setHasChanges(false);
    }
  };

  if (isLoading) {
    return <Loading />;
  }

  if (error) {
    return <ErrorState title="Error" message="Failed to load notification preferences" />;
  }

  return (
    <Card>
      <div className="space-y-6">
        <div>
          <h3 className="text-lg font-semibold text-gray-900 mb-2">Communication Preferences</h3>
          <p className="text-sm text-gray-600">
            Control how we can contact you. Opting out means you will not receive communications of that type.
          </p>
        </div>

        {/* Email Toggle */}
        <div className="flex items-center justify-between py-4 border-b border-gray-200">
          <div className="flex-1">
            <h4 className="text-base font-medium text-gray-900">Email Communications</h4>
            <p className="text-sm text-gray-500">
              Receive announcements and updates via email
            </p>
          </div>
          <button
            type="button"
            onClick={() => handleToggle('email', !emailOptedOut)}
            className={`
              relative inline-flex h-6 w-11 items-center rounded-full transition-colors
              focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2
              ${emailOptedOut ? 'bg-gray-300' : 'bg-blue-600'}
            `}
            aria-label="Toggle email communications"
          >
            <span
              className={`
                inline-block h-4 w-4 transform rounded-full bg-white transition-transform
                ${emailOptedOut ? 'translate-x-1' : 'translate-x-6'}
              `}
            />
          </button>
        </div>

        {/* SMS Toggle */}
        <div className="flex items-center justify-between py-4">
          <div className="flex-1">
            <h4 className="text-base font-medium text-gray-900">SMS Communications</h4>
            <p className="text-sm text-gray-500">
              Receive text messages for time-sensitive updates
            </p>
          </div>
          <button
            type="button"
            onClick={() => handleToggle('sms', !smsOptedOut)}
            className={`
              relative inline-flex h-6 w-11 items-center rounded-full transition-colors
              focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2
              ${smsOptedOut ? 'bg-gray-300' : 'bg-blue-600'}
            `}
            aria-label="Toggle SMS communications"
          >
            <span
              className={`
                inline-block h-4 w-4 transform rounded-full bg-white transition-transform
                ${smsOptedOut ? 'translate-x-1' : 'translate-x-6'}
              `}
            />
          </button>
        </div>

        {/* Action Buttons */}
        {hasChanges && (
          <div className="flex gap-3 pt-4 border-t border-gray-200">
            <Button
              onClick={handleSave}
              variant="primary"
              loading={updatePreferences.isPending}
            >
              Save Changes
            </Button>
            <Button
              onClick={handleReset}
              variant="outline"
              disabled={updatePreferences.isPending}
            >
              Cancel
            </Button>
          </div>
        )}

        {updatePreferences.isError && (
          <p className="text-sm text-red-600">
            Failed to update preferences. Please try again.
          </p>
        )}
        {updatePreferences.isSuccess && !hasChanges && (
          <p className="text-sm text-green-600">
            Preferences updated successfully!
          </p>
        )}
      </div>
    </Card>
  );
}
