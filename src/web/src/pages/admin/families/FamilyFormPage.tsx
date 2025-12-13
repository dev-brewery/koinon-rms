/**
 * Family Form Page
 * Create or edit a family
 */

import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useFamily, useCreateFamily, useUpdateFamily } from '@/hooks/useFamilies';
import type { CreateFamilyRequest, UpdateFamilyRequest } from '@/services/api/types';
import { familyFormSchema } from '@/schemas/family.schema';

export function FamilyFormPage() {
  const { idKey } = useParams<{ idKey: string }>();
  const navigate = useNavigate();
  const isEdit = !!idKey;

  const { data: family, isLoading: isLoadingFamily } = useFamily(idKey);
  const createFamily = useCreateFamily();
  const updateFamily = useUpdateFamily();

  const [name, setName] = useState('');
  const [campusId, setCampusId] = useState('');
  const [street1, setStreet1] = useState('');
  const [street2, setStreet2] = useState('');
  const [city, setCity] = useState('');
  const [state, setState] = useState('');
  const [postalCode, setPostalCode] = useState('');
  const [isDirty, setIsDirty] = useState(false);
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (family) {
      setName(family.name);
      setCampusId(family.campus?.idKey || '');

      const primaryAddress = family.addresses.find((addr) => addr.isMailingAddress);
      if (primaryAddress) {
        setStreet1(primaryAddress.address.street1);
        setStreet2(primaryAddress.address.street2 || '');
        setCity(primaryAddress.address.city);
        setState(primaryAddress.address.state);
        setPostalCode(primaryAddress.address.postalCode);
      }
    }
  }, [family]);

  const validateField = (fieldName: string, value: unknown) => {
    const formData = {
      name,
      campusId,
      street1,
      street2,
      city,
      state,
      postalCode,
      [fieldName]: value,
    };

    const result = familyFormSchema.safeParse(formData);
    if (!result.success) {
      const error = result.error.issues.find(issue => issue.path[0] === fieldName);
      if (error) {
        setValidationErrors(prev => ({ ...prev, [fieldName]: error.message }));
      } else {
        setValidationErrors(prev => {
          const { [fieldName]: removed, ...rest } = prev;
          return rest;
        });
      }
    } else {
      setValidationErrors(prev => {
        const { [fieldName]: removed, ...rest } = prev;
        return rest;
      });
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Validate all fields before submit
    const formData = {
      name,
      campusId,
      street1,
      street2,
      city,
      state,
      postalCode,
    };

    const result = familyFormSchema.safeParse(formData);
    if (!result.success) {
      const errors: Record<string, string> = {};
      result.error.issues.forEach(issue => {
        const fieldName = issue.path[0] as string;
        errors[fieldName] = issue.message;
      });
      setValidationErrors(errors);
      return;
    }

    setValidationErrors({});

    if (isEdit && idKey) {
      const request: UpdateFamilyRequest = {
        name,
        campusId: campusId || undefined,
      };

      try {
        await updateFamily.mutateAsync({ idKey, request });
        navigate(`/admin/families/${idKey}`);
      } catch (err) {
        // Error handling could go here
      }
    } else {
      const request: CreateFamilyRequest = {
        name,
        campusId: campusId || undefined,
      };

      if (street1 && city && state && postalCode) {
        request.address = {
          street1,
          street2: street2 || undefined,
          city,
          state,
          postalCode,
        };
      }

      try {
        const newFamily = await createFamily.mutateAsync(request);
        navigate(`/admin/families/${newFamily.idKey}`);
      } catch (err) {
        // Error handling could go here
      }
    }
  };

  const handleCancel = () => {
    if (isDirty) {
      const confirmed = window.confirm(
        'You have unsaved changes. Are you sure you want to leave?'
      );
      if (!confirmed) return;
    }

    if (isEdit && idKey) {
      navigate(`/admin/families/${idKey}`);
    } else {
      navigate('/admin/families');
    }
  };

  const handleFieldChange = (setter: (value: string) => void) => (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    setter(e.target.value);
    setIsDirty(true);
  };

  if (isEdit && isLoadingFamily) {
    return (
      <div className="flex items-center justify-center min-h-96">
        <div className="text-center">
          <div className="inline-block w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
          <p className="mt-4 text-gray-500">Loading family...</p>
        </div>
      </div>
    );
  }

  const isSubmitting = createFamily.isPending || updateFamily.isPending;

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <button
          onClick={handleCancel}
          className="text-gray-400 hover:text-gray-600"
          aria-label="Cancel"
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
        <h1 className="text-3xl font-bold text-gray-900">
          {isEdit ? 'Edit Family' : 'Create Family'}
        </h1>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Basic Info */}
        <div className="bg-white rounded-lg border border-gray-200 p-6 space-y-4">
          <h2 className="text-lg font-semibold text-gray-900">Basic Information</h2>

          <div>
            <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
              Family Name <span className="text-red-500">*</span>
            </label>
            <input
              id="name"
              type="text"
              value={name}
              onChange={handleFieldChange(setName)}
              onBlur={() => validateField('name', name)}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              placeholder="e.g., Smith Family"
            />
            {validationErrors.name && (
              <p className="text-sm text-red-600 mt-1">{validationErrors.name}</p>
            )}
          </div>

          <div>
            <label htmlFor="campus" className="block text-sm font-medium text-gray-700 mb-1">
              Campus
            </label>
            <select
              id="campus"
              value={campusId}
              onChange={handleFieldChange(setCampusId)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            >
              <option value="">None</option>
            </select>
          </div>
        </div>

        {/* Address */}
        {!isEdit && (
          <div className="bg-white rounded-lg border border-gray-200 p-6 space-y-4">
            <h2 className="text-lg font-semibold text-gray-900">Address (Optional)</h2>

            <div>
              <label htmlFor="street1" className="block text-sm font-medium text-gray-700 mb-1">
                Street Address
              </label>
              <input
                id="street1"
                type="text"
                value={street1}
                onChange={handleFieldChange(setStreet1)}
                onBlur={() => validateField('street1', street1)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                placeholder="123 Main St"
              />
              {validationErrors.street1 && (
                <p className="text-sm text-red-600 mt-1">{validationErrors.street1}</p>
              )}
            </div>

            <div>
              <label htmlFor="street2" className="block text-sm font-medium text-gray-700 mb-1">
                Street Address 2
              </label>
              <input
                id="street2"
                type="text"
                value={street2}
                onChange={handleFieldChange(setStreet2)}
                onBlur={() => validateField('street2', street2)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                placeholder="Apt 4B"
              />
              {validationErrors.street2 && (
                <p className="text-sm text-red-600 mt-1">{validationErrors.street2}</p>
              )}
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label htmlFor="city" className="block text-sm font-medium text-gray-700 mb-1">
                  City
                </label>
                <input
                  id="city"
                  type="text"
                  value={city}
                  onChange={handleFieldChange(setCity)}
                  onBlur={() => validateField('city', city)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                />
                {validationErrors.city && (
                  <p className="text-sm text-red-600 mt-1">{validationErrors.city}</p>
                )}
              </div>

              <div>
                <label htmlFor="state" className="block text-sm font-medium text-gray-700 mb-1">
                  State
                </label>
                <input
                  id="state"
                  type="text"
                  value={state}
                  onChange={handleFieldChange(setState)}
                  onBlur={() => validateField('state', state)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  maxLength={2}
                  placeholder="CA"
                />
                {validationErrors.state && (
                  <p className="text-sm text-red-600 mt-1">{validationErrors.state}</p>
                )}
              </div>
            </div>

            <div className="w-1/2">
              <label htmlFor="postalCode" className="block text-sm font-medium text-gray-700 mb-1">
                Postal Code
              </label>
              <input
                id="postalCode"
                type="text"
                value={postalCode}
                onChange={handleFieldChange(setPostalCode)}
                onBlur={() => validateField('postalCode', postalCode)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                placeholder="12345"
              />
              {validationErrors.postalCode && (
                <p className="text-sm text-red-600 mt-1">{validationErrors.postalCode}</p>
              )}
            </div>
          </div>
        )}

        {/* Actions */}
        <div className="flex justify-end gap-3">
          <button
            type="button"
            onClick={handleCancel}
            disabled={isSubmitting}
            className="px-4 py-2 bg-white border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 disabled:opacity-50 transition-colors"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting || !name.trim()}
            className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {isSubmitting ? 'Saving...' : isEdit ? 'Save Changes' : 'Create Family'}
          </button>
        </div>
      </form>
    </div>
  );
}
