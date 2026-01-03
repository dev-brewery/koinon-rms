/**
 * Locations Page
 * Admin page for managing locations with tree view
 */

import { useState } from 'react';
import { useLocationTree, useDeleteLocation } from '@/hooks/useLocations';
import { useCampuses } from '@/hooks/useCampuses';
import { LocationTreeNode } from '@/components/admin/LocationTreeNode';
import { LocationEditorModal } from '@/components/admin/LocationEditorModal';
import type { LocationDto } from '@/types/location';

export function LocationsPage() {
  // State for filters
  const [includeInactive, setIncludeInactive] = useState(false);
  const [selectedCampusIdKey, setSelectedCampusIdKey] = useState<string>('');

  // State for editor modal
  const [isEditorOpen, setIsEditorOpen] = useState(false);
  const [editingLocation, setEditingLocation] = useState<LocationDto | undefined>(undefined);
  const [parentIdKeyForNew, setParentIdKeyForNew] = useState<string | undefined>(undefined);

  // State for delete confirmation
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false);
  const [locationToDelete, setLocationToDelete] = useState<string | undefined>(undefined);

  // Fetch data
  const {
    data: locationTree = [],
    isLoading,
    error,
  } = useLocationTree({
    campusIdKey: selectedCampusIdKey || undefined,
    includeInactive,
  });

  const { data: campuses = [] } = useCampuses(false);
  const deleteMutation = useDeleteLocation();

  // Handler functions
  const handleAddLocation = () => {
    setEditingLocation(undefined);
    setParentIdKeyForNew(undefined);
    setIsEditorOpen(true);
  };

  const handleAddChild = (parentIdKey: string) => {
    setEditingLocation(undefined);
    setParentIdKeyForNew(parentIdKey);
    setIsEditorOpen(true);
  };

  const handleEdit = (location: LocationDto) => {
    setEditingLocation(location);
    setParentIdKeyForNew(undefined);
    setIsEditorOpen(true);
  };

  const handleDelete = (idKey: string) => {
    setLocationToDelete(idKey);
    setIsDeleteConfirmOpen(true);
  };

  const confirmDelete = async () => {
    if (!locationToDelete) return;

    try {
      await deleteMutation.mutateAsync(locationToDelete);
      setIsDeleteConfirmOpen(false);
      setLocationToDelete(undefined);
    } catch (error) {
      console.error('Failed to delete location:', error);
    }
  };

  const handleCloseEditor = () => {
    setIsEditorOpen(false);
    setEditingLocation(undefined);
    setParentIdKeyForNew(undefined);
  };

  const handleCloseDeleteConfirm = () => {
    setIsDeleteConfirmOpen(false);
    setLocationToDelete(undefined);
  };

  // Find location name for delete confirmation
  const getLocationName = (idKey: string): string => {
    const findInTree = (nodes: LocationDto[]): string | undefined => {
      for (const node of nodes) {
        if (node.idKey === idKey) return node.name;
        if (node.children && node.children.length > 0) {
          const found = findInTree(node.children);
          if (found) return found;
        }
      }
      return undefined;
    };

    return findInTree(locationTree) || 'this location';
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Locations</h1>
          <p className="mt-2 text-gray-600">Manage your organization's location hierarchy</p>
        </div>
        <button
          onClick={handleAddLocation}
          className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
        >
          Add Location
        </button>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <div className="flex flex-wrap items-center gap-4">
          {/* Campus Filter */}
          <div className="flex items-center gap-2">
            <label htmlFor="campusFilter" className="text-sm font-medium text-gray-700">
              Campus:
            </label>
            <select
              id="campusFilter"
              value={selectedCampusIdKey}
              onChange={(e) => setSelectedCampusIdKey(e.target.value)}
              className="px-3 py-1.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            >
              <option value="">All Campuses</option>
              {campuses.map((campus) => (
                <option key={campus.idKey} value={campus.idKey}>
                  {campus.name}
                </option>
              ))}
            </select>
          </div>

          {/* Include Inactive */}
          <div className="flex items-center">
            <input
              type="checkbox"
              id="includeInactive"
              checked={includeInactive}
              onChange={(e) => setIncludeInactive(e.target.checked)}
              className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
            />
            <label htmlFor="includeInactive" className="ml-2 text-sm text-gray-700">
              Include inactive locations
            </label>
          </div>
        </div>
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="flex items-center justify-center py-12">
          <div className="inline-block w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
        </div>
      )}

      {/* Error State */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex items-center gap-2">
            <svg
              className="w-5 h-5 text-red-600"
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
            <p className="text-sm font-medium text-red-800">Failed to load locations</p>
          </div>
        </div>
      )}

      {/* Location Tree */}
      {!isLoading && !error && (
        <div className="bg-white rounded-lg border border-gray-200">
          {locationTree.length === 0 ? (
            <div className="p-12 text-center">
              <svg
                className="w-12 h-12 text-gray-400 mx-auto mb-4"
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
              <p className="text-gray-500 mb-4">
                {selectedCampusIdKey || !includeInactive
                  ? 'No locations found with current filters'
                  : 'No locations found'}
              </p>
              <button
                onClick={handleAddLocation}
                className="inline-block px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
              >
                Add First Location
              </button>
            </div>
          ) : (
            <div className="p-2">
              {locationTree.map((location) => (
                <LocationTreeNode
                  key={location.idKey}
                  location={location}
                  level={0}
                  onEdit={handleEdit}
                  onDelete={handleDelete}
                  onAddChild={handleAddChild}
                />
              ))}
            </div>
          )}
        </div>
      )}

      {/* Editor Modal */}
      <LocationEditorModal
        isOpen={isEditorOpen}
        onClose={handleCloseEditor}
        location={editingLocation}
        parentLocationIdKey={parentIdKeyForNew}
      />

      {/* Delete Confirmation Dialog */}
      {isDeleteConfirmOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
          <div className="bg-white rounded-lg shadow-xl max-w-md w-full p-6">
            <div className="flex items-start gap-4">
              <div className="flex-shrink-0 w-10 h-10 rounded-full bg-red-100 flex items-center justify-center">
                <svg
                  className="w-6 h-6 text-red-600"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                  />
                </svg>
              </div>
              <div className="flex-1">
                <h3 className="text-lg font-semibold text-gray-900">Delete Location</h3>
                <p className="mt-2 text-sm text-gray-600">
                  Are you sure you want to delete "{locationToDelete ? getLocationName(locationToDelete) : 'this location'}"? This action cannot be undone.
                </p>
                {deleteMutation.error && (
                  <p className="mt-2 text-sm text-red-600">
                    Failed to delete location. It may have child locations or be in use.
                  </p>
                )}
              </div>
            </div>

            <div className="flex justify-end gap-3 mt-6">
              <button
                onClick={handleCloseDeleteConfirm}
                disabled={deleteMutation.isPending}
                className="px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50"
              >
                Cancel
              </button>
              <button
                onClick={confirmDelete}
                disabled={deleteMutation.isPending}
                className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50"
              >
                {deleteMutation.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
