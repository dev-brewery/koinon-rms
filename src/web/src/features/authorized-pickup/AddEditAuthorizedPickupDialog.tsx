/**
 * Add/Edit Authorized Pickup Dialog Component
 * Modal dialog for adding or editing an authorized pickup person
 */

import { useState, useEffect } from 'react';
import { useAddAuthorizedPickup, useUpdateAuthorizedPickup } from './hooks';
import {
  AuthorizationLevel,
  PickupRelationship,
  type AuthorizedPickup,
  type CreateAuthorizedPickupRequest,
  type UpdateAuthorizedPickupRequest,
} from './api';
import type { IdKey } from '@/services/api/types';

export interface AddEditAuthorizedPickupDialogProps {
  childIdKey: IdKey;
  pickup: AuthorizedPickup | null;
  onClose: () => void;
}

const RELATIONSHIP_OPTIONS = [
  { value: PickupRelationship.Parent, label: 'Parent' },
  { value: PickupRelationship.Grandparent, label: 'Grandparent' },
  { value: PickupRelationship.Sibling, label: 'Sibling' },
  { value: PickupRelationship.Guardian, label: 'Legal Guardian' },
  { value: PickupRelationship.Aunt, label: 'Aunt' },
  { value: PickupRelationship.Uncle, label: 'Uncle' },
  { value: PickupRelationship.Friend, label: 'Friend' },
  { value: PickupRelationship.Other, label: 'Other' },
];

const AUTHORIZATION_OPTIONS = [
  {
    value: AuthorizationLevel.Always,
    label: 'Always Authorized',
    description: 'Can pick up the child at any time',
  },
  {
    value: AuthorizationLevel.EmergencyOnly,
    label: 'Emergency Only',
    description: 'Requires supervisor approval',
  },
  {
    value: AuthorizationLevel.Never,
    label: 'Not Authorized',
    description: 'Explicitly blocked from pickup (custody situations)',
  },
];

export function AddEditAuthorizedPickupDialog({
  childIdKey,
  pickup,
  onClose,
}: AddEditAuthorizedPickupDialogProps) {
  const isEditing = !!pickup;

  const [name, setName] = useState(pickup?.name || pickup?.authorizedPersonName || '');
  const [phoneNumber, setPhoneNumber] = useState(pickup?.phoneNumber || '');
  const [relationship, setRelationship] = useState<PickupRelationship>(
    pickup?.relationship ?? PickupRelationship.Parent
  );
  const [authorizationLevel, setAuthorizationLevel] = useState<AuthorizationLevel>(
    pickup?.authorizationLevel ?? AuthorizationLevel.Always
  );
  const [custodyNotes, setCustodyNotes] = useState('');

  const addPickup = useAddAuthorizedPickup();
  const updatePickup = useUpdateAuthorizedPickup();

  // Reset form when pickup changes
  useEffect(() => {
    if (pickup) {
      setName(pickup.name || pickup.authorizedPersonName || '');
      setPhoneNumber(pickup.phoneNumber || '');
      setRelationship(pickup.relationship);
      setAuthorizationLevel(pickup.authorizationLevel);
    }
  }, [pickup]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      if (isEditing) {
        const request: UpdateAuthorizedPickupRequest = {
          relationship,
          authorizationLevel,
          custodyNotes: custodyNotes || undefined,
        };

        await updatePickup.mutateAsync({
          pickupIdKey: pickup.idKey,
          request,
        });
      } else {
        const request: CreateAuthorizedPickupRequest = {
          name: name || undefined,
          phoneNumber: phoneNumber || undefined,
          relationship,
          authorizationLevel,
          custodyNotes: custodyNotes || undefined,
        };

        await addPickup.mutateAsync({
          childIdKey,
          request,
        });
      }

      onClose();
    } catch (error) {
      // Error already captured by mutation hooks
    }
  };

  const canSubmit = isEditing || name.trim().length > 0;
  const isPending = addPickup.isPending || updatePickup.isPending;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:p-0">
        {/* Backdrop */}
        <div
          className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"
          onClick={onClose}
        />

        {/* Dialog */}
        <div className="relative inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
          <form onSubmit={handleSubmit}>
            <div className="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
              <h3 className="text-lg leading-6 font-medium text-gray-900 mb-4">
                {isEditing ? 'Edit Authorized Pickup' : 'Add Authorized Pickup'}
              </h3>

              <div className="space-y-4">
                {/* Name (disabled when editing) */}
                <div>
                  <label
                    htmlFor="name"
                    className="block text-sm font-medium text-gray-700"
                  >
                    Name {!isEditing && <span className="text-red-500">*</span>}
                  </label>
                  <input
                    type="text"
                    id="name"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    disabled={isEditing}
                    required={!isEditing}
                    className="mt-1 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm disabled:bg-gray-100 disabled:cursor-not-allowed"
                    placeholder="John Doe"
                  />
                  {isEditing && (
                    <p className="mt-1 text-xs text-gray-500">
                      Name cannot be changed after creation
                    </p>
                  )}
                </div>

                {/* Phone Number (disabled when editing) */}
                <div>
                  <label
                    htmlFor="phoneNumber"
                    className="block text-sm font-medium text-gray-700"
                  >
                    Phone Number
                  </label>
                  <input
                    type="tel"
                    id="phoneNumber"
                    value={phoneNumber}
                    onChange={(e) => setPhoneNumber(e.target.value)}
                    disabled={isEditing}
                    className="mt-1 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm disabled:bg-gray-100 disabled:cursor-not-allowed"
                    placeholder="(555) 123-4567"
                  />
                </div>

                {/* Relationship */}
                <div>
                  <label
                    htmlFor="relationship"
                    className="block text-sm font-medium text-gray-700"
                  >
                    Relationship <span className="text-red-500">*</span>
                  </label>
                  <select
                    id="relationship"
                    value={relationship}
                    onChange={(e) =>
                      setRelationship(Number(e.target.value) as PickupRelationship)
                    }
                    required
                    className="mt-1 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                  >
                    {RELATIONSHIP_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </div>

                {/* Authorization Level */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Authorization Level <span className="text-red-500">*</span>
                  </label>
                  <div className="space-y-2">
                    {AUTHORIZATION_OPTIONS.map((option) => (
                      <label
                        key={option.value}
                        className="relative flex items-start p-3 border rounded-lg cursor-pointer hover:bg-gray-50"
                      >
                        <input
                          type="radio"
                          name="authorizationLevel"
                          value={option.value}
                          checked={authorizationLevel === option.value}
                          onChange={(e) =>
                            setAuthorizationLevel(
                              Number(e.target.value) as AuthorizationLevel
                            )
                          }
                          className="h-4 w-4 mt-0.5 text-blue-600 focus:ring-blue-500 border-gray-300"
                        />
                        <div className="ml-3 flex-1">
                          <span className="block text-sm font-medium text-gray-900">
                            {option.label}
                          </span>
                          <span className="block text-xs text-gray-500">
                            {option.description}
                          </span>
                        </div>
                      </label>
                    ))}
                  </div>
                </div>

                {/* Custody Notes (supervisor only - shown but disabled for now) */}
                {authorizationLevel === AuthorizationLevel.Never && (
                  <div>
                    <label
                      htmlFor="custodyNotes"
                      className="block text-sm font-medium text-gray-700"
                    >
                      Custody Notes
                      <span className="ml-1 text-xs text-gray-500">
                        (Supervisor only)
                      </span>
                    </label>
                    <textarea
                      id="custodyNotes"
                      value={custodyNotes}
                      onChange={(e) => setCustodyNotes(e.target.value)}
                      rows={3}
                      className="mt-1 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                      placeholder="Enter custody-related notes (visible to supervisors only)"
                    />
                  </div>
                )}
              </div>

              {/* Error message */}
              {(addPickup.error || updatePickup.error) && (
                <div className="mt-4 rounded-md bg-red-50 p-3">
                  <p className="text-sm text-red-700">
                    {addPickup.error instanceof Error
                      ? addPickup.error.message
                      : updatePickup.error instanceof Error
                        ? updatePickup.error.message
                        : 'Failed to save authorized pickup'}
                  </p>
                </div>
              )}
            </div>

            {/* Actions */}
            <div className="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse gap-2">
              <button
                type="submit"
                disabled={!canSubmit || isPending}
                className="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:ml-3 sm:w-auto sm:text-sm disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isPending ? 'Saving...' : isEditing ? 'Update' : 'Add'}
              </button>
              <button
                type="button"
                onClick={onClose}
                disabled={isPending}
                className="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:mt-0 sm:w-auto sm:text-sm disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Cancel
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
