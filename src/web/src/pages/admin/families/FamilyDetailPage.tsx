/**
 * Family Detail Page
 * Displays family information with members and address
 */

import { useState } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { useFamily, useRemoveFamilyMember, useAddFamilyMember } from '@/hooks/useFamilies';
import { FamilyMemberCard } from '@/components/admin/families/FamilyMemberCard';
import { AddMemberModal } from '@/components/admin/families/AddMemberModal';

export function FamilyDetailPage() {
  const { idKey } = useParams<{ idKey: string }>();
  const navigate = useNavigate();
  const { data: family, isLoading, error } = useFamily(idKey);
  const removeMember = useRemoveFamilyMember();
  const addMember = useAddFamilyMember();

  const [isAddMemberModalOpen, setIsAddMemberModalOpen] = useState(false);
  const [removingMemberId, setRemovingMemberId] = useState<string | null>(null);

  const handleRemoveMember = async (personIdKey: string) => {
    if (!idKey) return;

    const confirmed = window.confirm(
      'Are you sure you want to remove this person from the family?'
    );

    if (!confirmed) return;

    setRemovingMemberId(personIdKey);
    try {
      await removeMember.mutateAsync({
        familyIdKey: idKey,
        personIdKey,
      });
    } finally {
      setRemovingMemberId(null);
    }
  };

  const handleAddMember = async (personId: string, roleId: string) => {
    if (!idKey) return;

    try {
      await addMember.mutateAsync({
        familyIdKey: idKey,
        request: {
          personId,
          roleId,
        },
      });
      setIsAddMemberModalOpen(false);
    } catch (err) {
      // Error handling via toast/notification could go here
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-96">
        <div className="text-center">
          <div className="inline-block w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
          <p className="mt-4 text-gray-500">Loading family...</p>
        </div>
      </div>
    );
  }

  if (error || !family) {
    return (
      <div className="flex items-center justify-center min-h-96">
        <div className="text-center">
          <svg
            className="w-12 h-12 text-red-400 mx-auto mb-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
          <p className="text-red-600 mb-2">Failed to load family</p>
          <p className="text-sm text-gray-500 mb-4">
            {error instanceof Error ? error.message : 'Unknown error'}
          </p>
          <button
            onClick={() => navigate('/admin/families')}
            className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700"
          >
            Back to Families
          </button>
        </div>
      </div>
    );
  }

  const primaryAddress = family.addresses.find((addr) => addr.isMailingAddress);

  // Get role IDs from existing family members (if available) or use first member's role
  const adultRoleId = family.members.find(m => m.role.name.toLowerCase() === 'adult')?.role.idKey || '';
  const childRoleId = family.members.find(m => m.role.name.toLowerCase() === 'child')?.role.idKey || '';

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <button
            onClick={() => navigate('/admin/families')}
            className="text-gray-400 hover:text-gray-600"
            aria-label="Back to families"
          >
            <svg
              className="w-6 h-6"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M15 19l-7-7 7-7"
              />
            </svg>
          </button>
          <div>
            <h1 className="text-3xl font-bold text-gray-900">{family.name}</h1>
            {family.campus && (
              <p className="mt-1 text-gray-600">{family.campus.name}</p>
            )}
          </div>
        </div>
        <Link
          to={`/admin/families/${idKey}/edit`}
          className="px-4 py-2 bg-white border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors"
        >
          Edit Family
        </Link>
      </div>

      {/* Address Section */}
      {primaryAddress && (
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <div className="flex items-start justify-between mb-3">
            <h2 className="text-lg font-semibold text-gray-900">Address</h2>
            <Link
              to={`/admin/families/${idKey}/edit`}
              className="text-sm text-primary-600 hover:text-primary-700"
            >
              Edit
            </Link>
          </div>
          <div className="flex items-start gap-3 text-gray-700">
            <svg
              className="w-5 h-5 text-gray-400 mt-0.5"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"
              />
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"
              />
            </svg>
            <address className="not-italic whitespace-pre-line">
              {primaryAddress.address.formattedAddress}
            </address>
          </div>
        </div>
      )}

      {/* Members Section */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">
            Family Members ({family.members.length})
          </h2>
          <button
            onClick={() => setIsAddMemberModalOpen(true)}
            className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
          >
            Add Member
          </button>
        </div>

        {family.members.length === 0 ? (
          <div className="text-center py-8 text-gray-500">
            <svg
              className="w-12 h-12 text-gray-300 mx-auto mb-3"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"
              />
            </svg>
            <p>No family members yet</p>
            <button
              onClick={() => setIsAddMemberModalOpen(true)}
              className="mt-3 text-primary-600 hover:text-primary-700 font-medium"
            >
              Add your first member
            </button>
          </div>
        ) : (
          <div className="space-y-3">
            {family.members.map((member) => (
              <FamilyMemberCard
                key={member.person.idKey}
                member={member}
                onRemove={() => handleRemoveMember(member.person.idKey)}
                readOnly={removingMemberId === member.person.idKey}
              />
            ))}
          </div>
        )}
      </div>

      {/* Add Member Modal */}
      <AddMemberModal
        isOpen={isAddMemberModalOpen}
        onClose={() => setIsAddMemberModalOpen(false)}
        onAdd={handleAddMember}
        adultRoleId={adultRoleId}
        childRoleId={childRoleId}
        existingMemberIds={family.members.map((m) => m.person.idKey)}
      />
    </div>
  );
}
