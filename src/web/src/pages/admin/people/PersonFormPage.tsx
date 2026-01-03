/**
 * Person Form Page
 * Create or edit a person
 */

import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { usePerson, useCreatePerson, useUpdatePerson } from '@/hooks/usePeople';
import type { CreatePersonRequest, UpdatePersonRequest, Gender } from '@/services/api/types';
import { personFormSchema } from '@/schemas/person.schema';
import { PersonPhotoUpload } from '@/components/admin/people/PersonPhotoUpload';

interface PhoneNumberForm {
  number: string;
  phoneTypeValueId?: string;
  isMessagingEnabled: boolean;
}

export function PersonFormPage() {
  const { idKey } = useParams<{ idKey: string }>();
  const navigate = useNavigate();
  const isEdit = !!idKey;

  const { data: person, isLoading } = usePerson(idKey);
  const createMutation = useCreatePerson();
  const updateMutation = useUpdatePerson();

  const [firstName, setFirstName] = useState('');
  const [nickName, setNickName] = useState('');
  const [middleName, setMiddleName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [gender, setGender] = useState<Gender>('Unknown');
  const [birthDate, setBirthDate] = useState('');
  const [campusId, setCampusId] = useState('');
  const [phoneNumbers, setPhoneNumbers] = useState<PhoneNumberForm[]>([]);
  const [isDirty, setIsDirty] = useState(false);
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (person) {
      setFirstName(person.firstName);
      setNickName(person.nickName || '');
      setMiddleName(person.middleName || '');
      setLastName(person.lastName);
      setEmail(person.email || '');
      setGender(person.gender);
      setBirthDate(person.birthDate || '');
      setCampusId(person.primaryCampus?.idKey || '');
      setPhoneNumbers(
        person.phoneNumbers.map((p) => ({
          number: p.number,
          phoneTypeValueId: p.phoneType?.idKey,
          isMessagingEnabled: p.isMessagingEnabled,
        }))
      );
    }
  }, [person]);

  const handleAddPhoneNumber = () => {
    setPhoneNumbers([...phoneNumbers, { number: '', isMessagingEnabled: true }]);
    setIsDirty(true);
  };

  const handleRemovePhoneNumber = (index: number) => {
    setPhoneNumbers(phoneNumbers.filter((_, i) => i !== index));
    setIsDirty(true);
  };

  const handlePhoneNumberChange = (index: number, field: keyof PhoneNumberForm, value: string | boolean) => {
    const updated = [...phoneNumbers];
    updated[index] = { ...updated[index], [field]: value };
    setPhoneNumbers(updated);
    setIsDirty(true);
  };

  const validateField = (fieldName: string, value: unknown) => {
    const formData = {
      firstName,
      lastName,
      nickName,
      middleName,
      email,
      gender,
      birthDate,
      campusId,
      phoneNumbers,
      [fieldName]: value,
    };

    const result = personFormSchema.safeParse(formData);
    if (!result.success) {
      const error = result.error.issues.find(issue => issue.path[0] === fieldName);
      if (error) {
        setValidationErrors(prev => ({ ...prev, [fieldName]: error.message }));
      } else {
        setValidationErrors(prev => {
          // eslint-disable-next-line @typescript-eslint/no-unused-vars
          const { [fieldName]: _removed, ...rest } = prev;
          return rest;
        });
      }
    } else {
      setValidationErrors(prev => {
        // eslint-disable-next-line @typescript-eslint/no-unused-vars
        const { [fieldName]: _removed, ...rest } = prev;
        return rest;
      });
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Validate all fields before submit
    const formData = {
      firstName,
      lastName,
      nickName,
      middleName,
      email,
      gender,
      birthDate,
      campusId,
      phoneNumbers,
    };

    const result = personFormSchema.safeParse(formData);
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

    const phoneNumbersData = phoneNumbers
      .filter((p) => p.number.trim())
      .map((p) => ({
        number: p.number,
        phoneTypeValueId: p.phoneTypeValueId,
        isMessagingEnabled: p.isMessagingEnabled,
      }));

    try {
      if (isEdit) {
        const request: UpdatePersonRequest = {};

        if (firstName !== person?.firstName) request.firstName = firstName;
        if (nickName !== person?.nickName) request.nickName = nickName || undefined;
        if (middleName !== person?.middleName) request.middleName = middleName || undefined;
        if (lastName !== person?.lastName) request.lastName = lastName;
        if (email !== person?.email) request.email = email || null;
        if (gender !== person?.gender) request.gender = gender;
        if (birthDate !== person?.birthDate) request.birthDate = birthDate || null;
        if (campusId !== person?.primaryCampus?.idKey) request.primaryCampusId = campusId || null;

        const result = await updateMutation.mutateAsync({ idKey: idKey!, request });
        navigate(`/admin/people/${result.idKey}`);
      } else {
        const request: CreatePersonRequest = {
          firstName,
          nickName: nickName || undefined,
          middleName: middleName || undefined,
          lastName,
          email: email || undefined,
          gender,
          birthDate: birthDate || undefined,
          phoneNumbers: phoneNumbersData.length > 0 ? phoneNumbersData : undefined,
          campusId: campusId || undefined,
          createFamily: true,
        };

        const result = await createMutation.mutateAsync(request);
        navigate(`/admin/people/${result.idKey}`);
      }
    } catch {
      // Error is handled by TanStack Query error state
    }
  };

  const handleCancel = () => {
    if (isDirty) {
      const confirmed = window.confirm('You have unsaved changes. Are you sure you want to leave?');
      if (!confirmed) return;
    }
    navigate(isEdit ? `/admin/people/${idKey}` : '/admin/people');
  };

  if (isEdit && isLoading) {
    return (
      <div className="p-12 text-center">
        <div className="inline-block w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
        <p className="mt-4 text-gray-500">Loading person...</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Back Link */}
      <Link
        to={isEdit ? `/admin/people/${idKey}` : '/admin/people'}
        className="inline-flex items-center text-sm text-gray-600 hover:text-gray-900"
      >
        <svg
          className="w-4 h-4 mr-1"
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
        Back
      </Link>

      {/* Form */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h1 className="text-2xl font-bold text-gray-900 mb-6">
          {isEdit ? 'Edit Person' : 'Add Person'}
        </h1>

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Photo Upload - Edit Mode Only */}
          {isEdit && idKey && (
            <div className="pb-6 border-b border-gray-200">
              <PersonPhotoUpload
                personIdKey={idKey}
                currentPhotoUrl={person?.photoUrl}
              />
            </div>
          )}
          {/* Names */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="firstName" className="block text-sm font-medium text-gray-700 mb-1">
                First Name <span className="text-red-500">*</span>
              </label>
              <input
                id="firstName"
                type="text"
                required
                maxLength={50}
                value={firstName}
                onChange={(e) => {
                  setFirstName(e.target.value);
                  setIsDirty(true);
                }}
                onBlur={() => validateField('firstName', firstName)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
              {validationErrors.firstName && (
                <p className="text-sm text-red-600 mt-1">{validationErrors.firstName}</p>
              )}
            </div>

            <div>
              <label htmlFor="lastName" className="block text-sm font-medium text-gray-700 mb-1">
                Last Name <span className="text-red-500">*</span>
              </label>
              <input
                id="lastName"
                type="text"
                required
                maxLength={50}
                value={lastName}
                onChange={(e) => {
                  setLastName(e.target.value);
                  setIsDirty(true);
                }}
                onBlur={() => validateField('lastName', lastName)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
              {validationErrors.lastName && (
                <p className="text-sm text-red-600 mt-1">{validationErrors.lastName}</p>
              )}
            </div>

            <div>
              <label htmlFor="nickName" className="block text-sm font-medium text-gray-700 mb-1">
                Nick Name
              </label>
              <input
                id="nickName"
                type="text"
                maxLength={50}
                value={nickName}
                onChange={(e) => {
                  setNickName(e.target.value);
                  setIsDirty(true);
                }}
                onBlur={() => validateField('nickName', nickName)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
              {validationErrors.nickName && (
                <p className="text-sm text-red-600 mt-1">{validationErrors.nickName}</p>
              )}
            </div>

            <div>
              <label htmlFor="middleName" className="block text-sm font-medium text-gray-700 mb-1">
                Middle Name
              </label>
              <input
                id="middleName"
                type="text"
                maxLength={50}
                value={middleName}
                onChange={(e) => {
                  setMiddleName(e.target.value);
                  setIsDirty(true);
                }}
                onBlur={() => validateField('middleName', middleName)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
              {validationErrors.middleName && (
                <p className="text-sm text-red-600 mt-1">{validationErrors.middleName}</p>
              )}
            </div>
          </div>

          {/* Demographics */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="gender" className="block text-sm font-medium text-gray-700 mb-1">
                Gender
              </label>
              <select
                id="gender"
                value={gender}
                onChange={(e) => {
                  setGender(e.target.value as Gender);
                  setIsDirty(true);
                }}
                onBlur={() => validateField('gender', gender)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              >
                <option value="Unknown">Unknown</option>
                <option value="Male">Male</option>
                <option value="Female">Female</option>
              </select>
              {validationErrors.gender && (
                <p className="text-sm text-red-600 mt-1">{validationErrors.gender}</p>
              )}
            </div>

            <div>
              <label htmlFor="birthDate" className="block text-sm font-medium text-gray-700 mb-1">
                Birth Date
              </label>
              <input
                id="birthDate"
                type="date"
                value={birthDate}
                onChange={(e) => {
                  setBirthDate(e.target.value);
                  setIsDirty(true);
                }}
                onBlur={() => validateField('birthDate', birthDate)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
              {validationErrors.birthDate && (
                <p className="text-sm text-red-600 mt-1">{validationErrors.birthDate}</p>
              )}
            </div>
          </div>

          {/* Contact */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                Email
              </label>
              <input
                id="email"
                type="email"
                value={email}
                onChange={(e) => {
                  setEmail(e.target.value);
                  setIsDirty(true);
                }}
                onBlur={() => validateField('email', email)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              />
              {validationErrors.email && (
                <p className="text-sm text-red-600 mt-1">{validationErrors.email}</p>
              )}
            </div>

            <div>
              <label htmlFor="campusId" className="block text-sm font-medium text-gray-700 mb-1">
                Campus
              </label>
              <select
                id="campusId"
                value={campusId}
                onChange={(e) => {
                  setCampusId(e.target.value);
                  setIsDirty(true);
                }}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              >
                <option value="">Select Campus</option>
              </select>
            </div>
          </div>

          {/* Phone Numbers */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <label className="block text-sm font-medium text-gray-700">
                Phone Numbers
              </label>
              <button
                type="button"
                onClick={handleAddPhoneNumber}
                className="text-sm text-primary-600 hover:text-primary-700"
              >
                + Add Phone
              </button>
            </div>
            <div className="space-y-2">
              {phoneNumbers.map((phone, index) => (
                <div key={index} className="flex items-center gap-2">
                  <input
                    type="tel"
                    placeholder="Phone number"
                    value={phone.number}
                    onChange={(e) => handlePhoneNumberChange(index, 'number', e.target.value)}
                    className="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  />
                  <label className="flex items-center gap-1 text-sm text-gray-700">
                    <input
                      type="checkbox"
                      checked={phone.isMessagingEnabled}
                      onChange={(e) => handlePhoneNumberChange(index, 'isMessagingEnabled', e.target.checked)}
                      className="rounded"
                    />
                    SMS
                  </label>
                  <button
                    type="button"
                    onClick={() => handleRemovePhoneNumber(index)}
                    className="p-2 text-red-600 hover:text-red-700"
                    aria-label="Remove phone number"
                  >
                    <svg
                      className="w-5 h-5"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                      aria-hidden="true"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                      />
                    </svg>
                  </button>
                </div>
              ))}
            </div>
          </div>

          {/* Actions */}
          <div className="flex items-center justify-end gap-4 pt-4 border-t border-gray-200">
            <button
              type="button"
              onClick={handleCancel}
              className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={createMutation.isPending || updateMutation.isPending}
              className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50"
            >
              {createMutation.isPending || updateMutation.isPending
                ? 'Saving...'
                : isEdit
                  ? 'Update Person'
                  : 'Create Person'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
