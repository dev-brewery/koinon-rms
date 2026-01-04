/**
 * Notification preferences component
 * Settings panel for managing notification preferences by type
 */

import { useNotificationPreferences, useUpdatePreference } from '@/hooks/useNotifications';
import { NotificationType } from '@/types/notification';
import { Loading } from '@/components/ui/Loading';
import { EmptyState } from '@/components/ui/EmptyState';
import { cn } from '@/lib/utils';

/**
 * Human-readable labels for notification types
 */
const NOTIFICATION_TYPE_LABELS: Record<NotificationType, { label: string; description: string }> = {
  [NotificationType.CheckinAlert]: {
    label: 'Check-in Alerts',
    description: 'Notifications about check-in events and parent pickup requests',
  },
  [NotificationType.CommunicationStatus]: {
    label: 'Communication Status',
    description: 'Updates on email and SMS delivery status',
  },
  [NotificationType.SystemAlert]: {
    label: 'System Alerts',
    description: 'Important system announcements and updates',
  },
  [NotificationType.MembershipRequest]: {
    label: 'Membership Requests',
    description: 'Notifications about group membership requests',
  },
  [NotificationType.FollowUp]: {
    label: 'Follow-up Reminders',
    description: 'Reminders for follow-up tasks and activities',
  },
};

export function NotificationPreferences() {
  const { data: preferences, isLoading, error } = useNotificationPreferences();
  const updatePreferenceMutation = useUpdatePreference();

  const handleToggle = (notificationType: NotificationType, currentValue: boolean) => {
    updatePreferenceMutation.mutate({
      notificationType,
      isEnabled: !currentValue,
    });
  };

  if (isLoading) {
    return (
      <div className="py-8">
        <Loading variant="spinner" text="Loading preferences..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className="py-8">
        <EmptyState
          icon={
            <svg className="w-12 h-12 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
          }
          title="Failed to load preferences"
          description="There was an error loading your notification preferences. Please try again."
        />
      </div>
    );
  }

  if (!preferences || preferences.length === 0) {
    return (
      <div className="py-8">
        <EmptyState
          title="No preferences available"
          description="No notification preferences are configured for your account."
        />
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg shadow-md">
      <div className="px-6 py-4 border-b border-gray-200">
        <h2 className="text-lg font-semibold text-gray-900">Notification Preferences</h2>
        <p className="mt-1 text-sm text-gray-500">
          Choose which types of notifications you want to receive
        </p>
      </div>

      <div className="divide-y divide-gray-200">
        {preferences.map((preference) => {
          const typeInfo = NOTIFICATION_TYPE_LABELS[preference.notificationType];

          return (
            <div key={preference.idKey} className="px-6 py-4 flex items-center justify-between">
              <div className="flex-1">
                <h3 className="text-sm font-medium text-gray-900">{typeInfo.label}</h3>
                <p className="mt-1 text-sm text-gray-500">{typeInfo.description}</p>
              </div>

              {/* Toggle switch */}
              <button
                role="switch"
                aria-checked={preference.isEnabled}
                aria-label={`${preference.isEnabled ? 'Disable' : 'Enable'} ${typeInfo.label}`}
                onClick={() => handleToggle(preference.notificationType, preference.isEnabled)}
                disabled={updatePreferenceMutation.isPending}
                className={cn(
                  'relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed',
                  preference.isEnabled ? 'bg-blue-600' : 'bg-gray-200'
                )}
              >
                <span
                  className={cn(
                    'pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out',
                    preference.isEnabled ? 'translate-x-5' : 'translate-x-0'
                  )}
                />
              </button>
            </div>
          );
        })}
      </div>

      {updatePreferenceMutation.isSuccess && (
        <div className="px-6 py-3 bg-green-50 border-t border-green-100">
          <p className="text-sm text-green-700">Preferences saved successfully!</p>
        </div>
      )}

      {updatePreferenceMutation.isError && (
        <div className="px-6 py-3 bg-red-50 border-t border-red-100">
          <p className="text-sm text-red-700">Failed to save preferences. Please try again.</p>
        </div>
      )}
    </div>
  );
}
