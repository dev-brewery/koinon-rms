/**
 * CampusEditorModal Component
 * Modal for creating and editing campuses
 */

import { useState, useEffect, useCallback } from 'react';
import type { CampusDto } from '@/services/api/types';
import type { CreateCampusRequest, UpdateCampusRequest } from '@/types/campus';
import { useCreateCampus, useUpdateCampus } from '@/hooks/useCampuses';

interface CampusEditorModalProps {
  isOpen: boolean;
  onClose: () => void;
  campus?: CampusDto;
}

const TIMEZONE_OPTIONS = [
  { value: 'America/New_York', label: 'Eastern Time (ET)' },
  { value: 'America/Chicago', label: 'Central Time (CT)' },
  { value: 'America/Denver', label: 'Mountain Time (MT)' },
  { value: 'America/Phoenix', label: 'Arizona (no DST)' },
  { value: 'America/Los_Angeles', label: 'Pacific Time (PT)' },
  { value: 'America/Anchorage', label: 'Alaska Time (AKT)' },
  { value: 'Pacific/Honolulu', label: 'Hawaii-Aleutian Time (HAT)' },
  { value: 'UTC', label: 'UTC' },
];

export function CampusEditorModal({ isOpen, onClose, campus }: CampusEditorModalProps) {
  const isEditing = !!campus;
  const createMutation = useCreateCampus();
  const updateMutation = useUpdateCampus();

  const [formData, setFormData] = useState({
    name: '',
    shortCode: '',
    description: '',
    url: '',
    phoneNumber: '',
    timeZoneId: '',
    serviceTimes: '',
    order: 0,
    isActive: true,
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  // Populate form when editing
  useEffect(() => {
    if (campus) {
      setFormData({
        name: campus.name,
        shortCode: campus.shortCode || '',
        description: campus.description || '',
        url: campus.url || '',
        phoneNumber: campus.phoneNumber || '',
        timeZoneId: campus.timeZoneId || '',
        serviceTimes: campus.serviceTimes || '',
        order: campus.order,
        isActive: campus.isActive,
      });
    } else {
      setFormData({
        name: '',
        shortCode: '',
        description: '',
        url: '',
        phoneNumber: '',
        timeZoneId: '',
        serviceTimes: '',
        order: 0,
        isActive: true,
      });
    }
  }, [campus]);

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

    if (formData.shortCode && formData.shortCode.length > 10) {
      newErrors.shortCode = 'Short code must be 10 characters or less';
    }

    if (formData.description && formData.description.length > 500) {
      newErrors.description = 'Description must be 500 characters or less';
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
      const request: CreateCampusRequest | UpdateCampusRequest = {
        name: formData.name,
        shortCode: formData.shortCode || undefined,
        description: formData.description || undefined,
        url: formData.url || undefined,
        phoneNumber: formData.phoneNumber || undefined,
        timeZoneId: formData.timeZoneId || undefined,
        serviceTimes: formData.serviceTimes || undefined,
        order: formData.order,
      };

      if (isEditing) {
        await updateMutation.mutateAsync({
          idKey: campus.idKey,
          request: { ...request, isActive: formData.isActive },
        });
      } else {
        await createMutation.mutateAsync(request as CreateCampusRequest);
      }

      onClose();
    } catch (error) {
      console.error('Failed to save campus:', error);
    }
  };

  if (!isOpen) {
    return null;
  }

  const isPending = createMutation.isPending || updateMutation.isPending;
  const error = createMutation.error || updateMutation.error;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
        <div className="sticky top-0 bg-white border-b border-gray-200 px-6 py-4">
          <h2 className="text-2xl font-bold text-gray-900">
            {isEditing ? 'Edit Campus' : 'Create Campus'}
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
              placeholder="Main Campus"
            />
            {errors.name && <p className="mt-1 text-sm text-red-600">{errors.name}</p>}
          </div>

          {/* Short Code */}
          <div>
            <label htmlFor="shortCode" className="block text-sm font-medium text-gray-700 mb-1">
              Short Code
            </label>
            <input
              type="text"
              id="shortCode"
              value={formData.shortCode}
              onChange={(e) => setFormData({ ...formData, shortCode: e.target.value })}
              maxLength={10}
              className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
                errors.shortCode ? 'border-red-500' : 'border-gray-300'
              }`}
              placeholder="MC"
            />
            {errors.shortCode && <p className="mt-1 text-sm text-red-600">{errors.shortCode}</p>}
            <p className="mt-1 text-sm text-gray-500">Maximum 10 characters</p>
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
              placeholder="Brief description of this campus"
            />
            {errors.description && <p className="mt-1 text-sm text-red-600">{errors.description}</p>}
            <p className="mt-1 text-sm text-gray-500">
              {formData.description.length}/500 characters
            </p>
          </div>

          {/* URL */}
          <div>
            <label htmlFor="url" className="block text-sm font-medium text-gray-700 mb-1">
              URL
            </label>
            <input
              type="url"
              id="url"
              value={formData.url}
              onChange={(e) => setFormData({ ...formData, url: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              placeholder="https://example.com/main-campus"
            />
          </div>

          {/* Phone Number */}
          <div>
            <label htmlFor="phoneNumber" className="block text-sm font-medium text-gray-700 mb-1">
              Phone Number
            </label>
            <input
              type="tel"
              id="phoneNumber"
              value={formData.phoneNumber}
              onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              placeholder="(555) 123-4567"
            />
          </div>

          {/* Time Zone */}
          <div>
            <label htmlFor="timeZoneId" className="block text-sm font-medium text-gray-700 mb-1">
              Time Zone
            </label>
            <select
              id="timeZoneId"
              value={formData.timeZoneId}
              onChange={(e) => setFormData({ ...formData, timeZoneId: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            >
              <option value="">Select a time zone...</option>
              {TIMEZONE_OPTIONS.map((tz) => (
                <option key={tz.value} value={tz.value}>
                  {tz.label}
                </option>
              ))}
            </select>
          </div>

          {/* Service Times */}
          <div>
            <label htmlFor="serviceTimes" className="block text-sm font-medium text-gray-700 mb-1">
              Service Times
            </label>
            <textarea
              id="serviceTimes"
              value={formData.serviceTimes}
              onChange={(e) => setFormData({ ...formData, serviceTimes: e.target.value })}
              rows={2}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              placeholder="Saturday 5:00 PM, Sunday 9:00 AM, 11:00 AM"
            />
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
                Failed to {isEditing ? 'update' : 'create'} campus. Please try again.
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
