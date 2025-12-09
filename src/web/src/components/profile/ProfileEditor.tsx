/**
 * ProfileEditor
 * View/Edit toggle for user profile with form fields
 */

import { useState, useEffect } from 'react';
import { PhoneNumberEditor } from './PhoneNumberEditor';
import { useUpdateMyProfile } from '@/hooks/useProfile';
import type { MyProfileDto, PhoneNumberRequestDto } from '@/types/profile';

interface ProfileEditorProps {
  profile: MyProfileDto;
}

export function ProfileEditor({ profile }: ProfileEditorProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [nickName, setNickName] = useState(profile.nickName || '');
  const [email, setEmail] = useState(profile.email || '');
  const [emailPreference, setEmailPreference] = useState(profile.emailPreference || 'EmailAllowed');
  const [phoneNumbers, setPhoneNumbers] = useState<PhoneNumberRequestDto[]>(() =>
    profile.phoneNumbers?.map((p) => ({
      idKey: p.idKey,
      number: p.number,
      extension: p.extension,
      phoneTypeIdKey: p.phoneType?.idKey,
      isMessagingEnabled: p.isMessagingEnabled,
      isUnlisted: p.isUnlisted,
    })) || []
  );

  const updateMutation = useUpdateMyProfile();

  // Reset form when profile changes or editing is cancelled
  useEffect(() => {
    setNickName(profile.nickName || '');
    setEmail(profile.email || '');
    setEmailPreference(profile.emailPreference || 'EmailAllowed');
    setPhoneNumbers(
      profile.phoneNumbers?.map((p) => ({
        idKey: p.idKey,
        number: p.number,
        extension: p.extension,
        phoneTypeIdKey: p.phoneType?.idKey,
        isMessagingEnabled: p.isMessagingEnabled,
        isUnlisted: p.isUnlisted,
      })) || []
    );
  }, [profile, isEditing]);

  const handleSave = async () => {
    try {
      await updateMutation.mutateAsync({
        nickName: nickName || undefined,
        email: email || undefined,
        emailPreference,
        phoneNumbers: phoneNumbers.length > 0 ? phoneNumbers : undefined,
      });
      setIsEditing(false);
    } catch (error) {
      // Error is handled by TanStack Query mutation state
    }
  };

  const handleCancel = () => {
    setNickName(profile.nickName || '');
    setEmail(profile.email || '');
    setEmailPreference(profile.emailPreference || 'EmailAllowed');
    setIsEditing(false);
  };

  if (!isEditing) {
    // View Mode
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="flex items-start justify-between mb-6">
          <div className="flex items-center gap-4">
            {profile.photoUrl ? (
              <img
                src={profile.photoUrl}
                alt={profile.fullName}
                className="w-16 h-16 rounded-full object-cover"
              />
            ) : (
              <div className="w-16 h-16 rounded-full bg-primary-100 flex items-center justify-center">
                <span className="text-2xl font-semibold text-primary-700">
                  {profile.firstName[0]}
                  {profile.lastName[0]}
                </span>
              </div>
            )}
            <div>
              <h2 className="text-2xl font-bold text-gray-900">{profile.fullName}</h2>
              {profile.nickName && (
                <p className="text-sm text-gray-600">Prefers: {profile.nickName}</p>
              )}
            </div>
          </div>
          <button
            onClick={() => setIsEditing(true)}
            className="px-4 py-2 text-primary-600 border border-primary-600 rounded-lg hover:bg-primary-50 transition-colors"
          >
            Edit Profile
          </button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label className="block text-sm font-medium text-gray-500 mb-1">Email</label>
            <p className="text-gray-900">{profile.email || 'Not provided'}</p>
            {profile.email && (
              <p className="text-xs text-gray-500 mt-1">
                Status: {profile.isEmailActive ? 'Active' : 'Inactive'} • Preference:{' '}
                {profile.emailPreference}
              </p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-500 mb-1">
              Phone Numbers
            </label>
            {profile.phoneNumbers.length > 0 ? (
              <div className="space-y-1">
                {profile.phoneNumbers.map((phone) => (
                  <div key={phone.idKey} className="text-gray-900">
                    {phone.numberFormatted}
                    {phone.phoneType && (
                      <span className="text-xs text-gray-500 ml-2">
                        ({phone.phoneType.value})
                      </span>
                    )}
                    {phone.isMessagingEnabled && (
                      <span className="text-xs text-green-600 ml-2">SMS enabled</span>
                    )}
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-gray-900">Not provided</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-500 mb-1">Birth Date</label>
            <p className="text-gray-900">
              {profile.birthDate || 'Not provided'}
              {profile.age && <span className="text-gray-500 ml-2">({profile.age} years)</span>}
            </p>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-500 mb-1">Gender</label>
            <p className="text-gray-900">{profile.gender}</p>
          </div>

          {profile.primaryFamily && (
            <div>
              <label className="block text-sm font-medium text-gray-500 mb-1">Family</label>
              <p className="text-gray-900">{profile.primaryFamily.name}</p>
            </div>
          )}

          {profile.primaryCampus && (
            <div>
              <label className="block text-sm font-medium text-gray-500 mb-1">Campus</label>
              <p className="text-gray-900">{profile.primaryCampus.name}</p>
            </div>
          )}
        </div>
      </div>
    );
  }

  // Edit Mode
  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <h2 className="text-xl font-bold text-gray-900 mb-6">Edit Profile</h2>

      <div className="space-y-4">
        <div>
          <label htmlFor="nickName" className="block text-sm font-medium text-gray-700 mb-1">
            Preferred Name (Nickname)
          </label>
          <input
            type="text"
            id="nickName"
            value={nickName}
            onChange={(e) => setNickName(e.target.value)}
            placeholder={profile.firstName}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          />
        </div>

        <div>
          <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
            Email Address
          </label>
          <input
            type="email"
            id="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="your.email@example.com"
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          />
        </div>

        <div>
          <label htmlFor="emailPreference" className="block text-sm font-medium text-gray-700 mb-1">
            Email Preference
          </label>
          <select
            id="emailPreference"
            value={emailPreference}
            onChange={(e) => setEmailPreference(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          >
            <option value="EmailAllowed">Email Allowed</option>
            <option value="NoMassEmails">No Mass Emails</option>
            <option value="DoNotEmail">Do Not Email</option>
          </select>
        </div>

        <PhoneNumberEditor
          phoneNumbers={profile.phoneNumbers}
          onChange={setPhoneNumbers}
          disabled={updateMutation.isPending}
        />

        <div className="flex gap-3 pt-4">
          <button
            onClick={handleSave}
            disabled={updateMutation.isPending}
            className="px-6 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50"
          >
            {updateMutation.isPending ? 'Saving...' : 'Save Changes'}
          </button>
          <button
            onClick={handleCancel}
            disabled={updateMutation.isPending}
            className="px-6 py-2 text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors disabled:opacity-50"
          >
            Cancel
          </button>
        </div>

        {updateMutation.isError && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-3 text-sm text-red-800">
            Failed to update profile. Please try again.
          </div>
        )}
      </div>
    </div>
  );
}
