/**
 * FamilyMemberCard
 * Display and edit family member information
 */

import { useState } from 'react';
import { useUpdateFamilyMember } from '@/hooks/useProfile';
import type { FamilyMemberDto } from '@/types/profile';

interface FamilyMemberCardProps {
  member: FamilyMemberDto;
}

export function FamilyMemberCard({ member }: FamilyMemberCardProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [nickName, setNickName] = useState(member.nickName || '');
  const [allergies, setAllergies] = useState(member.allergies || '');
  const [specialNeeds, setSpecialNeeds] = useState(member.specialNeeds || '');

  const updateMutation = useUpdateFamilyMember();

  const handleSave = async () => {
    try {
      await updateMutation.mutateAsync({
        personIdKey: member.idKey,
        data: {
          nickName: nickName || undefined,
          allergies: allergies || undefined,
          specialNeeds: specialNeeds || undefined,
        },
      });
      setIsEditing(false);
    } catch (error) {
      // Error is handled by TanStack Query mutation state
    }
  };

  const handleCancel = () => {
    setNickName(member.nickName || '');
    setAllergies(member.allergies || '');
    setSpecialNeeds(member.specialNeeds || '');
    setIsEditing(false);
  };

  if (!isEditing) {
    // View Mode
    return (
      <div className="bg-white border border-gray-200 rounded-lg p-4">
        <div className="flex items-start gap-4">
          {member.photoUrl ? (
            <img
              src={member.photoUrl}
              alt={member.fullName}
              className="w-16 h-16 rounded-full object-cover"
            />
          ) : (
            <div className="w-16 h-16 rounded-full bg-primary-100 flex items-center justify-center">
              <span className="text-xl font-semibold text-primary-700">
                {member.firstName[0]}
                {member.lastName[0]}
              </span>
            </div>
          )}

          <div className="flex-1 min-w-0">
            <div className="flex items-start justify-between gap-2">
              <div>
                <h3 className="text-lg font-semibold text-gray-900">{member.fullName}</h3>
                {member.nickName && (
                  <p className="text-sm text-gray-600">Prefers: {member.nickName}</p>
                )}
                <div className="flex items-center gap-2 mt-1">
                  <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800">
                    {member.familyRole}
                  </span>
                  {member.age && (
                    <span className="text-sm text-gray-600">{member.age} years old</span>
                  )}
                </div>
              </div>

              {member.canEdit && (
                <button
                  onClick={() => setIsEditing(true)}
                  className="text-sm text-primary-600 hover:text-primary-700 font-medium"
                >
                  Edit
                </button>
              )}
            </div>

            {(member.allergies || member.specialNeeds) && (
              <div className="mt-3 space-y-1">
                {member.allergies && (
                  <div className="text-sm">
                    <span className="font-medium text-gray-700">Allergies:</span>{' '}
                    <span className="text-gray-900">{member.allergies}</span>
                  </div>
                )}
                {member.specialNeeds && (
                  <div className="text-sm">
                    <span className="font-medium text-gray-700">Special Needs:</span>{' '}
                    <span className="text-gray-900">{member.specialNeeds}</span>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      </div>
    );
  }

  // Edit Mode
  return (
    <div className="bg-white border border-gray-200 rounded-lg p-4">
      <div className="flex items-start gap-4 mb-4">
        {member.photoUrl ? (
          <img
            src={member.photoUrl}
            alt={member.fullName}
            className="w-16 h-16 rounded-full object-cover"
          />
        ) : (
          <div className="w-16 h-16 rounded-full bg-primary-100 flex items-center justify-center">
            <span className="text-xl font-semibold text-primary-700">
              {member.firstName[0]}
              {member.lastName[0]}
            </span>
          </div>
        )}

        <div className="flex-1 min-w-0">
          <h3 className="text-lg font-semibold text-gray-900">{member.fullName}</h3>
          <div className="flex items-center gap-2 mt-1">
            <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800">
              {member.familyRole}
            </span>
            {member.age && (
              <span className="text-sm text-gray-600">{member.age} years old</span>
            )}
          </div>
        </div>
      </div>

      <div className="space-y-3">
        <div>
          <label htmlFor={`nickName-${member.idKey}`} className="block text-sm font-medium text-gray-700 mb-1">
            Preferred Name (Nickname)
          </label>
          <input
            type="text"
            id={`nickName-${member.idKey}`}
            value={nickName}
            onChange={(e) => setNickName(e.target.value)}
            placeholder={member.firstName}
            disabled={updateMutation.isPending}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 disabled:bg-gray-100"
          />
        </div>

        <div>
          <label htmlFor={`allergies-${member.idKey}`} className="block text-sm font-medium text-gray-700 mb-1">
            Allergies
          </label>
          <textarea
            id={`allergies-${member.idKey}`}
            value={allergies}
            onChange={(e) => setAllergies(e.target.value)}
            placeholder="Any allergies or dietary restrictions"
            rows={2}
            disabled={updateMutation.isPending}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 disabled:bg-gray-100"
          />
        </div>

        <div>
          <label htmlFor={`specialNeeds-${member.idKey}`} className="block text-sm font-medium text-gray-700 mb-1">
            Special Needs
          </label>
          <textarea
            id={`specialNeeds-${member.idKey}`}
            value={specialNeeds}
            onChange={(e) => setSpecialNeeds(e.target.value)}
            placeholder="Any special needs or accommodations"
            rows={2}
            disabled={updateMutation.isPending}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 disabled:bg-gray-100"
          />
        </div>

        <div className="flex gap-2 pt-2">
          <button
            onClick={handleSave}
            disabled={updateMutation.isPending}
            className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50 text-sm"
          >
            {updateMutation.isPending ? 'Saving...' : 'Save'}
          </button>
          <button
            onClick={handleCancel}
            disabled={updateMutation.isPending}
            className="px-4 py-2 text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors disabled:opacity-50 text-sm"
          >
            Cancel
          </button>
        </div>

        {updateMutation.isError && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-2 text-sm text-red-800">
            Failed to update. Please try again.
          </div>
        )}
      </div>
    </div>
  );
}
