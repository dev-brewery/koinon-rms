/**
 * DisplaySettingsSection
 * Theme, date format, and timezone preferences
 */

import { useState, useEffect } from 'react';
import { usePreferences, useUpdatePreferences } from '@/hooks/useSettings';
import { Theme } from '@/types/settings';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Loading } from '@/components/ui/Loading';
import { ErrorState } from '@/components/ui/ErrorState';

const THEME_OPTIONS = [
  { value: Theme.System.toString(), label: 'System Default' },
  { value: Theme.Light.toString(), label: 'Light' },
  { value: Theme.Dark.toString(), label: 'Dark' },
];

const DATE_FORMAT_OPTIONS = [
  { value: 'MM/DD/YYYY', label: 'MM/DD/YYYY (US)' },
  { value: 'DD/MM/YYYY', label: 'DD/MM/YYYY (International)' },
  { value: 'YYYY-MM-DD', label: 'YYYY-MM-DD (ISO)' },
];

const TIMEZONE_OPTIONS = [
  { value: 'America/New_York', label: 'Eastern Time (US)' },
  { value: 'America/Chicago', label: 'Central Time (US)' },
  { value: 'America/Denver', label: 'Mountain Time (US)' },
  { value: 'America/Phoenix', label: 'Arizona' },
  { value: 'America/Los_Angeles', label: 'Pacific Time (US)' },
  { value: 'America/Anchorage', label: 'Alaska' },
  { value: 'Pacific/Honolulu', label: 'Hawaii' },
  { value: 'UTC', label: 'UTC' },
];

export function DisplaySettingsSection() {
  const { data: preferences, isLoading, error } = usePreferences();
  const updatePreferences = useUpdatePreferences();

  const [theme, setTheme] = useState<Theme>(Theme.System);
  const [dateFormat, setDateFormat] = useState('MM/DD/YYYY');
  const [timezone, setTimezone] = useState('America/New_York');
  const [hasChanges, setHasChanges] = useState(false);

  useEffect(() => {
    if (preferences) {
      setTheme(preferences.theme ?? Theme.System);
      setDateFormat(preferences.dateFormat || 'MM/DD/YYYY');
      setTimezone(preferences.timeZone || 'America/New_York');
    }
  }, [preferences]);

  const handleChange = () => {
    setHasChanges(true);
  };

  const handleSave = async () => {
    try {
      await updatePreferences.mutateAsync({
        theme,
        dateFormat,
        timeZone: timezone,
      });
      setHasChanges(false);
    } catch (error) {
      // Error handled by mutation
    }
  };

  const handleReset = () => {
    if (preferences) {
      setTheme(preferences.theme ?? Theme.System);
      setDateFormat(preferences.dateFormat || 'MM/DD/YYYY');
      setTimezone(preferences.timeZone || 'America/New_York');
      setHasChanges(false);
    }
  };

  if (isLoading) {
    return <Loading />;
  }

  if (error) {
    return <ErrorState title="Error" message="Failed to load display preferences" />;
  }

  return (
    <Card>
      <div className="space-y-6">
        <div>
          <h3 className="text-lg font-semibold text-gray-900 mb-2">Display Preferences</h3>
          <p className="text-sm text-gray-600">
            Customize how dates, times, and the interface appear
          </p>
        </div>

        <Select
          label="Theme"
          options={THEME_OPTIONS}
          value={theme.toString()}
          onChange={(e) => {
            setTheme(parseInt(e.target.value) as Theme);
            handleChange();
          }}
        />

        <Select
          label="Date Format"
          options={DATE_FORMAT_OPTIONS}
          value={dateFormat}
          onChange={(e) => {
            setDateFormat(e.target.value);
            handleChange();
          }}
        />

        <Select
          label="Timezone"
          options={TIMEZONE_OPTIONS}
          value={timezone}
          onChange={(e) => {
            setTimezone(e.target.value);
            handleChange();
          }}
        />

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
