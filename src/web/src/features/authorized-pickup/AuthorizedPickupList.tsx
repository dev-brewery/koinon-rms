/**
 * Authorized Pickup List Component
 * Displays and manages authorized pickup persons for a child
 */

import { useState } from 'react';
import {
  useAuthorizedPickups,
  useDeleteAuthorizedPickup,
  useAutoPopulateFamilyMembers,
} from './hooks';
import { AuthorizationLevel, type AuthorizedPickup } from './api';
import { AddEditAuthorizedPickupDialog } from './AddEditAuthorizedPickupDialog';
import type { IdKey } from '@/services/api/types';

export interface AuthorizedPickupListProps {
  childIdKey: IdKey;
  childName: string;
}

const AUTHORIZATION_LEVEL_LABELS = {
  [AuthorizationLevel.Always]: 'Always Authorized',
  [AuthorizationLevel.EmergencyOnly]: 'Emergency Only',
  [AuthorizationLevel.Never]: 'Not Authorized',
};

const AUTHORIZATION_LEVEL_COLORS = {
  [AuthorizationLevel.Always]: 'bg-green-100 text-green-800 border-green-300',
  [AuthorizationLevel.EmergencyOnly]:
    'bg-yellow-100 text-yellow-800 border-yellow-300',
  [AuthorizationLevel.Never]: 'bg-red-100 text-red-800 border-red-300',
};

export function AuthorizedPickupList({
  childIdKey,
  childName,
}: AuthorizedPickupListProps) {
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingPickup, setEditingPickup] = useState<AuthorizedPickup | null>(
    null
  );

  const { data: pickups, isLoading, error } = useAuthorizedPickups(childIdKey);
  const deletePickup = useDeleteAuthorizedPickup();
  const autoPopulate = useAutoPopulateFamilyMembers();

  const handleEdit = (pickup: AuthorizedPickup) => {
    setEditingPickup(pickup);
    setDialogOpen(true);
  };

  const handleDelete = async (pickup: AuthorizedPickup) => {
    if (
      !confirm(
        `Are you sure you want to remove ${pickup.authorizedPersonName || pickup.name} from the authorized pickup list?`
      )
    ) {
      return;
    }

    try {
      await deletePickup.mutateAsync({
        pickupIdKey: pickup.idKey,
        childIdKey,
      });
    } catch (error) {
      // Error already captured by mutation hook
    }
  };

  const handleAddNew = () => {
    setEditingPickup(null);
    setDialogOpen(true);
  };

  const handleAutoPopulate = async () => {
    if (
      !confirm(
        'This will add all adult family members as authorized pickups. Continue?'
      )
    ) {
      return;
    }

    try {
      await autoPopulate.mutateAsync(childIdKey);
    } catch (error) {
      // Error already captured by mutation hook
    }
  };

  const handleDialogClose = () => {
    setDialogOpen(false);
    setEditingPickup(null);
  };

  if (error) {
    return (
      <div className="rounded-md bg-red-50 p-4">
        <div className="flex">
          <div className="ml-3">
            <h3 className="text-sm font-medium text-red-800">
              Error loading authorized pickups
            </h3>
            <p className="mt-2 text-sm text-red-700">
              {error instanceof Error ? error.message : 'Unknown error occurred'}
            </p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-medium text-gray-900">
          Authorized Pickups for {childName}
        </h3>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={handleAutoPopulate}
            disabled={autoPopulate.isPending}
            className="inline-flex items-center px-3 py-2 border border-gray-300 shadow-sm text-sm leading-4 font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50"
          >
            {autoPopulate.isPending ? 'Adding...' : 'Auto-populate Family'}
          </button>
          <button
            type="button"
            onClick={handleAddNew}
            className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            Add Authorized Pickup
          </button>
        </div>
      </div>

      {/* Loading state */}
      {isLoading && (
        <div className="text-center py-8">
          <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900"></div>
          <p className="mt-2 text-sm text-gray-500">Loading authorized pickups...</p>
        </div>
      )}

      {/* Empty state */}
      {!isLoading && pickups && pickups.length === 0 && (
        <div className="text-center py-8 bg-gray-50 rounded-lg">
          <svg
            className="mx-auto h-12 w-12 text-gray-400"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"
            />
          </svg>
          <h3 className="mt-2 text-sm font-medium text-gray-900">
            No authorized pickups
          </h3>
          <p className="mt-1 text-sm text-gray-500">
            Add authorized persons or auto-populate with family members.
          </p>
        </div>
      )}

      {/* Pickup list */}
      {!isLoading && pickups && pickups.length > 0 && (
        <div className="bg-white shadow overflow-hidden sm:rounded-md">
          <ul className="divide-y divide-gray-200">
            {pickups.map((pickup) => (
              <li key={pickup.idKey}>
                <div className="px-4 py-4 sm:px-6 hover:bg-gray-50">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center min-w-0 flex-1">
                      {/* Photo */}
                      {pickup.photoUrl ? (
                        <img
                          src={pickup.photoUrl}
                          alt={pickup.authorizedPersonName || pickup.name}
                          className="h-12 w-12 rounded-full flex-shrink-0"
                        />
                      ) : (
                        <div className="h-12 w-12 rounded-full bg-gray-200 flex items-center justify-center flex-shrink-0">
                          <svg
                            className="h-6 w-6 text-gray-400"
                            fill="none"
                            viewBox="0 0 24 24"
                            stroke="currentColor"
                          >
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={2}
                              d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                            />
                          </svg>
                        </div>
                      )}

                      {/* Info */}
                      <div className="ml-4 min-w-0 flex-1">
                        <p className="text-sm font-medium text-gray-900 truncate">
                          {pickup.authorizedPersonName || pickup.name}
                        </p>
                        <div className="flex items-center gap-3 mt-1">
                          <span
                            className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border ${
                              AUTHORIZATION_LEVEL_COLORS[pickup.authorizationLevel]
                            }`}
                          >
                            {AUTHORIZATION_LEVEL_LABELS[pickup.authorizationLevel]}
                          </span>
                          {pickup.phoneNumber && (
                            <span className="text-xs text-gray-500">
                              {pickup.phoneNumber}
                            </span>
                          )}
                        </div>
                      </div>
                    </div>

                    {/* Actions */}
                    <div className="ml-4 flex items-center gap-2 flex-shrink-0">
                      <button
                        type="button"
                        onClick={() => handleEdit(pickup)}
                        className="inline-flex items-center px-3 py-1.5 border border-gray-300 shadow-sm text-xs font-medium rounded text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                      >
                        Edit
                      </button>
                      <button
                        type="button"
                        onClick={() => handleDelete(pickup)}
                        disabled={deletePickup.isPending}
                        className="inline-flex items-center px-3 py-1.5 border border-transparent shadow-sm text-xs font-medium rounded text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50"
                      >
                        {deletePickup.isPending ? 'Removing...' : 'Remove'}
                      </button>
                    </div>
                  </div>
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Add/Edit Dialog */}
      {dialogOpen && (
        <AddEditAuthorizedPickupDialog
          childIdKey={childIdKey}
          pickup={editingPickup}
          onClose={handleDialogClose}
        />
      )}
    </div>
  );
}
