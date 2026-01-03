/**
 * LocationEditorModal Component
 * Modal for creating and editing locations
 */

import { useState, useEffect, useCallback } from 'react';
import type { LocationDto } from '@/types/location';
import type { CreateLocationRequest, UpdateLocationRequest } from '@/types/location';
import { useCreateLocation, useUpdateLocation, useLocations } from '@/hooks/useLocations';
import { useCampuses } from '@/hooks/useCampuses';

interface LocationEditorModalProps {
  isOpen: boolean;
  onClose: () => void;
  location?: LocationDto;
  parentLocationIdKey?: string;
}

export function LocationEditorModal({
  isOpen,
  onClose,
  location,
  parentLocationIdKey,
}: LocationEditorModalProps) {
  const isEditing = !!location;
  const createMutation = useCreateLocation();
  const updateMutation = useUpdateLocation();

  // Fetch locations (flat list) for parent selection
  const { data: locations = [] } = useLocations({ includeInactive: false });
  
  // Fetch campuses for campus selection
  const { data: campuses = [] } = useCampuses(false);

  const [formData, setFormData] = useState({
    name: '',
    description: '',
    parentLocationIdKey: parentLocationIdKey || '',
    campusIdKey: '',
    softRoomThreshold: 0,
    firmRoomThreshold: 0,
    order: 0,
    isActive: true,
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  // Populate form when editing
  useEffect(() => {
    if (location) {
      setFormData({
        name: location.name,
        description: location.description || '',
        parentLocationIdKey: location.parentLocationIdKey || '',
        campusIdKey: location.campusIdKey || '',
        softRoomThreshold: location.softRoomThreshold || 0,
        firmRoomThreshold: location.firmRoomThreshold || 0,
        order: location.order,
        isActive: location.isActive,
      });
    } else {
      setFormData({
        name: '',
        description: '',
        parentLocationIdKey: parentLocationIdKey || '',
        campusIdKey: '',
        softRoomThreshold: 0,
        firmRoomThreshold: 0,
        order: 0,
        isActive: true,
      });
    }
  }, [location, parentLocationIdKey]);

  // Handle escape key
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };

    if (isOpen) {
      document.addEventListener('keydown', handleEscape);
    }

    return () => {
      document.removeEventListener('keydown', handleEscape);
    };
  }, [isOpen, onClose]);

  const validateForm = useCallback((): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.name.trim()) {
      newErrors.name = 'Name is required';
    } else if (formData.name.trim().length < 2) {
      newErrors.name = 'Name must be at least 2 characters';
    }

    if (formData.description && formData.description.length > 500) {
      newErrors.description = 'Description must be 500 characters or less';
    }

    if (formData.softRoomThreshold && formData.firmRoomThreshold) {
      if (formData.softRoomThreshold > formData.firmRoomThreshold) {
        newErrors.softRoomThreshold = 'Soft threshold cannot exceed firm threshold';
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }, [formData]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    try {
      const request: CreateLocationRequest | UpdateLocationRequest = {
        name: formData.name,
        description: formData.description || undefined,
        parentLocationIdKey: formData.parentLocationIdKey || undefined,
        campusIdKey: formData.campusIdKey || undefined,
        softRoomThreshold: formData.softRoomThreshold || undefined,
        firmRoomThreshold: formData.firmRoomThreshold || undefined,
        order: formData.order,
      };

      if (isEditing) {
        await updateMutation.mutateAsync({
          idKey: location.idKey,
          request: { ...request, isActive: formData.isActive },
        });
      } else {
        await createMutation.mutateAsync(request as CreateLocationRequest);
      }

      onClose();
    } catch (error) {
      console.error('Failed to save location:', error);
    }
  };

  if (!isOpen) {
    return null;
  }

  const isPending = createMutation.isPending || updateMutation.isPending;
  const error = createMutation.error || updateMutation.error;

  // Filter out the current location from parent options (can't be its own parent)
  const availableParentLocations = isEditing
    ? locations.filter((loc) => loc.idKey !== location.idKey)
    : locations;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
        <div className="sticky top-0 bg-white border-b border-gray-200 px-6 py-4">
          <h2 className="text-2xl font-bold text-gray-900">
            {isEditing ? 'Edit Location' : 'Create Location'}
          </h2>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-6">
          {/* Name */}
          <div>
            <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
              Name <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              id="name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
                errors.name ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="Children's Ministry"
            />
            {errors.name && <p className="mt-1 text-sm text-red-600">{errors.name}</p>}
          </div>

          {/* Description */}
          <div>
            <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
              Description
            </label>
            <textarea
              id="description"
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              maxLength={500}
              rows={3}
              className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
                errors.description ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="Brief description of this location"
            />
            {errors.description && <p className="mt-1 text-sm text-red-600">{errors.description}</p>}
            <p className="mt-1 text-sm text-gray-500">
              {formData.description.length}/500 characters
            </p>
          </div>

          {/* Parent Location */}
          <div>
            <label htmlFor="parentLocationIdKey" className="block text-sm font-medium text-gray-700 mb-1">
              Parent Location
            </label>
            <select
              id="parentLocationIdKey"
              value={formData.parentLocationIdKey}
              onChange={(e) => setFormData({ ...formData, parentLocationIdKey: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            >
              <option value="">None (Top Level)</option>
              {availableParentLocations.map((loc) => (
                <option key={loc.idKey} value={loc.idKey}>
                  {loc.name}
                </option>
              ))}
            </select>
            <p className="mt-1 text-sm text-gray-500">
              Optional: select a parent location to create a hierarchy
            </p>
          </div>

          {/* Campus */}
          <div>
            <label htmlFor="campusIdKey" className="block text-sm font-medium text-gray-700 mb-1">
              Campus
            </label>
            <select
              id="campusIdKey"
              value={formData.campusIdKey}
              onChange={(e) => setFormData({ ...formData, campusIdKey: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            >
              <option value="">Select a campus...</option>
              {campuses.map((campus) => (
                <option key={campus.idKey} value={campus.idKey}>
                  {campus.name}
                </option>
              ))}
            </select>
          </div>

          {/* Room Thresholds */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label htmlFor="softRoomThreshold" className="block text-sm font-medium text-gray-700 mb-1">
                Soft Room Threshold
              </label>
              <input
                type="number"
                id="softRoomThreshold"
                value={formData.softRoomThreshold}
                onChange={(e) =>
                  setFormData({ ...formData, softRoomThreshold: parseInt(e.target.value, 10) || 0 })
                }
                min="0"
                className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
                  errors.softRoomThreshold ? 'border-red-500' : 'border-gray-300'
                }`}
              />
              {errors.softRoomThreshold && (
                <p className="mt-1 text-sm text-red-600">{errors.softRoomThreshold}</p>
              )}
              <p className="mt-1 text-sm text-gray-500">Warning capacity</p>
            </div>

            <div>
              <label htmlFor="firmRoomThreshold" className="block text-sm font-medium text-gray-700 mb-1">
                Firm Room Threshold
              </label>
              <input
                type="number"
                id="firmRoomThreshold"
                value={formData.firmRoomThreshold}
                onChange={(e) =>
                  setFormData({ ...formData, firmRoomThreshold: parseInt(e.target.value, 10) || 0 })
                }
                min="0"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
              <p className="mt-1 text-sm text-gray-500">Maximum capacity</p>
            </div>
          </div>

          {/* Order */}
          <div>
            <label htmlFor="order" className="block text-sm font-medium text-gray-700 mb-1">
              Display Order
            </label>
            <input
              type="number"
              id="order"
              value={formData.order}
              onChange={(e) => setFormData({ ...formData, order: parseInt(e.target.value, 10) || 0 })}
              min="0"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
            <p className="mt-1 text-sm text-gray-500">Lower numbers appear first</p>
          </div>

          {/* Is Active (only shown in edit mode) */}
          {isEditing && (
            <div className="flex items-center">
              <input
                type="checkbox"
                id="isActive"
                checked={formData.isActive}
                onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
              />
              <label htmlFor="isActive" className="ml-2 text-sm text-gray-700">
                Active
              </label>
            </div>
          )}

          {/* Error Display */}
          {error && (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
              <p className="text-sm text-red-800">
                Failed to {isEditing ? 'update' : 'create'} location. Please try again.
              </p>
            </div>
          )}

          {/* Actions */}
          <div className="flex justify-end gap-3 pt-4 border-t border-gray-200">
            <button
              type="button"
              onClick={onClose}
              disabled={isPending}
              className="px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isPending}
              className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-50"
            >
              {isPending ? 'Saving...' : isEditing ? 'Update' : 'Create'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
