/**
 * ProfileSettingsSection
 * Display/edit name, email, and photo for user settings
 */

import { useState, useEffect } from 'react';
import { useMyProfile, useUpdateMyProfile } from '@/hooks/useProfile';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Loading } from '@/components/ui/Loading';
import { ErrorState } from '@/components/ui/ErrorState';

export function ProfileSettingsSection() {
  const { data: profile, isLoading, error } = useMyProfile();
  const updateProfile = useUpdateMyProfile();

  const [nickName, setNickName] = useState('');
  const [email, setEmail] = useState('');
  const [emailPreference, setEmailPreference] = useState('');
  const [isEditing, setIsEditing] = useState(false);

  // Initialize form when profile loads
  useEffect(() => {
    if (profile) {
      setNickName(profile.nickName || '');
      setEmail(profile.email || '');
      setEmailPreference(profile.emailPreference || 'EmailAllowed');
    }
  }, [profile]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      await updateProfile.mutateAsync({
        nickName: nickName || undefined,
        email: email || undefined,
        emailPreference: emailPreference || undefined,
      });
      setIsEditing(false);
    } catch (error) {
      // Error handled by mutation
    }
  };

  const handleCancel = () => {
    // Reset form
    if (profile) {
      setNickName(profile.nickName || '');
      setEmail(profile.email || '');
      setEmailPreference(profile.emailPreference || 'EmailAllowed');
    }
    setIsEditing(false);
  };

  if (isLoading) {
    return <Loading />;
  }

  if (error) {
    return <ErrorState title="Error" message="Failed to load profile settings" />;
  }

  if (!profile) {
    return <ErrorState title="Not Found" message="Profile not found" />;
  }

  return (
    <div className="space-y-6">
      {/* Profile Photo */}
      <Card>
        <div className="flex items-center gap-4">
          {profile.photoUrl ? (
            <img
              src={profile.photoUrl}
              alt={profile.fullName}
              className="w-20 h-20 rounded-full object-cover"
            />
          ) : (
            <div className="w-20 h-20 rounded-full bg-gray-200 flex items-center justify-center">
              <svg className="w-10 h-10 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                />
              </svg>
            </div>
          )}
          <div>
            <h3 className="text-lg font-semibold text-gray-900">{profile.fullName}</h3>
            <p className="text-sm text-gray-500">{profile.email || 'No email'}</p>
          </div>
        </div>
      </Card>

      {/* Profile Form */}
      <Card>
        <form onSubmit={handleSubmit} className="space-y-4">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Profile Information</h3>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Input
              label="First Name"
              value={profile.firstName}
              disabled
              className="bg-gray-50"
            />
            <Input
              label="Last Name"
              value={profile.lastName}
              disabled
              className="bg-gray-50"
            />
          </div>

          <Input
            label="Preferred Name"
            value={nickName}
            onChange={(e) => setNickName(e.target.value)}
            disabled={!isEditing}
            placeholder="Optional nickname or preferred name"
          />

          <Input
            label="Email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            disabled={!isEditing}
            placeholder="your.email@example.com"
          />

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Email Preference
            </label>
            <select
              value={emailPreference}
              onChange={(e) => setEmailPreference(e.target.value)}
              disabled={!isEditing}
              className="w-full px-4 py-3 text-base border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
            >
              <option value="EmailAllowed">Allowed</option>
              <option value="NoMassEmails">No Mass Emails</option>
              <option value="DoNotEmail">Do Not Email</option>
            </select>
          </div>

          {/* Action Buttons */}
          <div className="flex gap-3 pt-4">
            {!isEditing ? (
              <Button
                type="button"
                onClick={() => setIsEditing(true)}
                variant="primary"
              >
                Edit Profile
              </Button>
            ) : (
              <>
                <Button
                  type="submit"
                  variant="primary"
                  loading={updateProfile.isPending}
                >
                  Save Changes
                </Button>
                <Button
                  type="button"
                  onClick={handleCancel}
                  variant="outline"
                  disabled={updateProfile.isPending}
                >
                  Cancel
                </Button>
              </>
            )}
          </div>

          {updateProfile.isError && (
            <p className="text-sm text-red-600">
              Failed to update profile. Please try again.
            </p>
          )}
          {updateProfile.isSuccess && !isEditing && (
            <p className="text-sm text-green-600">
              Profile updated successfully!
            </p>
          )}
        </form>
      </Card>
    </div>
  );
}
