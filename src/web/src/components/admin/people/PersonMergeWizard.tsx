/**
 * Person Merge Wizard
 * Multi-step wizard for merging two people
 */

import { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { usePersonComparison, useMergePeople } from '@/hooks/usePersonMerge';
import { Loading, ErrorState } from '@/components/ui';
import { useToast } from '@/contexts/ToastContext';
import type { PersonDto } from '@/types/person';

interface PersonMergeWizardProps {
  person1IdKey: string;
  person2IdKey: string;
}

type WizardStep = 'select-survivor' | 'choose-fields' | 'preview' | 'confirm';
type FieldSelection = 'survivor' | 'merged';

export function PersonMergeWizard({ person1IdKey, person2IdKey }: PersonMergeWizardProps) {
  const navigate = useNavigate();
  const { success: showSuccess, error: showError } = useToast();
  const { data, isLoading, error } = usePersonComparison(person1IdKey, person2IdKey);
  const mergeMutation = useMergePeople();

  const [currentStep, setCurrentStep] = useState<WizardStep>('select-survivor');
  const [survivorIdKey, setSurvivorIdKey] = useState<string>('');
  const [fieldSelections, setFieldSelections] = useState<Record<string, FieldSelection>>({});
  const [notes, setNotes] = useState('');
  const [confirmed, setConfirmed] = useState(false);

  // Fields that can be selected - MUST be before early returns (React hooks rule)
  const selectableFields = useMemo(() => {
    if (!data) return [];

    const { person1, person2 } = data;
    const fields: Array<{ key: string; label: string; value1: unknown; value2: unknown }> = [];

    const addField = (key: string, label: string, value1: unknown, value2: unknown) => {
      if (value1 !== value2) {
        fields.push({ key, label, value1, value2 });
      }
    };

    addField('firstName', 'First Name', person1.firstName, person2.firstName);
    addField('middleName', 'Middle Name', person1.middleName, person2.middleName);
    addField('lastName', 'Last Name', person1.lastName, person2.lastName);
    addField('nickName', 'Nick Name', person1.nickName, person2.nickName);
    addField('email', 'Email', person1.email, person2.email);
    addField('birthDate', 'Birth Date', person1.birthDate, person2.birthDate);
    addField('gender', 'Gender', person1.gender, person2.gender);
    addField('primaryCampus', 'Campus', person1.primaryCampus?.name, person2.primaryCampus?.name);
    addField('connectionStatus', 'Connection Status', person1.connectionStatus?.value, person2.connectionStatus?.value);
    addField('recordStatus', 'Record Status', person1.recordStatus?.value, person2.recordStatus?.value);

    return fields;
  }, [data]);

  if (isLoading) {
    return <Loading text="Loading merge wizard..." />;
  }

  if (error) {
    return (
      <ErrorState
        title="Failed to load merge wizard"
        message={error instanceof Error ? error.message : 'Unknown error'}
      />
    );
  }

  if (!data) {
    return null;
  }

  const { person1, person2 } = data;
  const survivor = survivorIdKey === person1IdKey ? person1 : person2;
  const merged = survivorIdKey === person1IdKey ? person2 : person1;
  const mergedIdKey = survivorIdKey === person1IdKey ? person2IdKey : person1IdKey;

  const handleNext = () => {
    if (currentStep === 'select-survivor') {
      if (!survivorIdKey) {
        showError('Error', 'Please select which person to keep');
        return;
      }
      // Initialize field selections with 'survivor' as default
      const initialSelections: Record<string, FieldSelection> = {};
      selectableFields.forEach((field) => {
        initialSelections[field.key] = 'survivor';
      });
      setFieldSelections(initialSelections);
      setCurrentStep('choose-fields');
    } else if (currentStep === 'choose-fields') {
      setCurrentStep('preview');
    } else if (currentStep === 'preview') {
      setCurrentStep('confirm');
    }
  };

  const handleBack = () => {
    if (currentStep === 'choose-fields') {
      setCurrentStep('select-survivor');
    } else if (currentStep === 'preview') {
      setCurrentStep('choose-fields');
    } else if (currentStep === 'confirm') {
      setCurrentStep('preview');
    }
  };

  const handleSubmit = async () => {
    if (!confirmed) {
      showError('Error', 'Please confirm you understand this cannot be undone');
      return;
    }

    try {
      const result = await mergeMutation.mutateAsync({
        survivorIdKey,
        mergedIdKey,
        fieldSelections,
        notes: notes || undefined,
      });

      showSuccess('Success', 'People merged successfully');
      navigate(`/admin/people/${result.survivorIdKey}`);
    } catch (err) {
      showError('Error', 'Failed to merge people');
    }
  };

  const renderStepIndicator = () => (
    <div className="flex items-center justify-center gap-4 mb-8">
      {['Select Survivor', 'Choose Fields', 'Preview', 'Confirm'].map((label, index) => {
        const stepKeys: WizardStep[] = ['select-survivor', 'choose-fields', 'preview', 'confirm'];
        const isActive = stepKeys[index] === currentStep;
        const isCompleted = stepKeys.indexOf(currentStep) > index;

        return (
          <div key={label} className="flex items-center gap-2">
            <div
              className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-semibold ${
                isActive
                  ? 'bg-primary-600 text-white'
                  : isCompleted
                  ? 'bg-green-600 text-white'
                  : 'bg-gray-200 text-gray-600'
              }`}
            >
              {isCompleted ? '✓' : index + 1}
            </div>
            <span className={`text-sm ${isActive ? 'font-semibold' : ''}`}>{label}</span>
            {index < 3 && <div className="w-8 h-0.5 bg-gray-300" />}
          </div>
        );
      })}
    </div>
  );

  const renderSelectSurvivor = () => (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-gray-900 mb-2">Select Survivor</h2>
        <p className="text-gray-600">
          Choose which person record to keep. The other record will be merged into this one.
        </p>
      </div>

      <div className="grid grid-cols-2 gap-6">
        <button
          onClick={() => setSurvivorIdKey(person1IdKey)}
          className={`p-6 border-2 rounded-lg text-left transition-all ${
            survivorIdKey === person1IdKey
              ? 'border-primary-600 bg-primary-50'
              : 'border-gray-300 hover:border-gray-400'
          }`}
        >
          <div className="flex items-start gap-4">
            {person1.photoUrl ? (
              <img
                src={person1.photoUrl}
                alt={`${person1.firstName} ${person1.lastName}`}
                className="w-16 h-16 rounded-full object-cover"
              />
            ) : (
              <div className="w-16 h-16 rounded-full bg-gray-300 flex items-center justify-center">
                <span className="text-xl font-semibold text-gray-600">
                  {person1.firstName.charAt(0)}{person1.lastName.charAt(0)}
                </span>
              </div>
            )}
            <div className="flex-1">
              <h3 className="text-lg font-semibold text-gray-900">
                {person1.firstName} {person1.lastName}
              </h3>
              {person1.email && <p className="text-sm text-gray-600">{person1.email}</p>}
              <div className="mt-2 text-sm text-gray-500">
                <p>{data.person1AttendanceCount} attendance records</p>
                <p>{data.person1GroupMembershipCount} group memberships</p>
                <p>${data.person1ContributionTotal.toFixed(2)} in contributions</p>
              </div>
            </div>
          </div>
        </button>

        <button
          onClick={() => setSurvivorIdKey(person2IdKey)}
          className={`p-6 border-2 rounded-lg text-left transition-all ${
            survivorIdKey === person2IdKey
              ? 'border-primary-600 bg-primary-50'
              : 'border-gray-300 hover:border-gray-400'
          }`}
        >
          <div className="flex items-start gap-4">
            {person2.photoUrl ? (
              <img
                src={person2.photoUrl}
                alt={`${person2.firstName} ${person2.lastName}`}
                className="w-16 h-16 rounded-full object-cover"
              />
            ) : (
              <div className="w-16 h-16 rounded-full bg-gray-300 flex items-center justify-center">
                <span className="text-xl font-semibold text-gray-600">
                  {person2.firstName.charAt(0)}{person2.lastName.charAt(0)}
                </span>
              </div>
            )}
            <div className="flex-1">
              <h3 className="text-lg font-semibold text-gray-900">
                {person2.firstName} {person2.lastName}
              </h3>
              {person2.email && <p className="text-sm text-gray-600">{person2.email}</p>}
              <div className="mt-2 text-sm text-gray-500">
                <p>{data.person2AttendanceCount} attendance records</p>
                <p>{data.person2GroupMembershipCount} group memberships</p>
                <p>${data.person2ContributionTotal.toFixed(2)} in contributions</p>
              </div>
            </div>
          </div>
        </button>
      </div>
    </div>
  );

  const renderChooseFields = () => {
    if (selectableFields.length === 0) {
      return (
        <div className="space-y-6">
          <div>
            <h2 className="text-xl font-semibold text-gray-900 mb-2">Choose Fields</h2>
            <p className="text-gray-600">
              All fields are identical. Proceeding to next step.
            </p>
          </div>
        </div>
      );
    }

    return (
      <div className="space-y-6">
        <div>
          <h2 className="text-xl font-semibold text-gray-900 mb-2">Choose Fields</h2>
          <p className="text-gray-600">
            For fields that differ, select which value to keep in the merged record.
          </p>
        </div>

        <div className="space-y-4">
          {selectableFields.map((field) => {
            const survivorValue = survivorIdKey === person1IdKey ? field.value1 : field.value2;
            const mergedValue = survivorIdKey === person1IdKey ? field.value2 : field.value1;

            return (
              <div key={field.key} className="bg-gray-50 p-4 rounded-lg">
                <h3 className="text-sm font-semibold text-gray-900 mb-3">{field.label}</h3>
                <div className="space-y-2">
                  <label className="flex items-start gap-3 cursor-pointer">
                    <input
                      type="radio"
                      name={field.key}
                      checked={fieldSelections[field.key] === 'survivor'}
                      onChange={() =>
                        setFieldSelections({ ...fieldSelections, [field.key]: 'survivor' })
                      }
                      className="mt-1"
                    />
                    <div className="flex-1">
                      <div className="font-medium text-gray-900">
                        {survivorValue !== undefined && survivorValue !== null
                          ? String(survivorValue)
                          : <span className="text-gray-400 italic">Not set</span>}
                      </div>
                      <div className="text-xs text-gray-500">From survivor record</div>
                    </div>
                  </label>
                  <label className="flex items-start gap-3 cursor-pointer">
                    <input
                      type="radio"
                      name={field.key}
                      checked={fieldSelections[field.key] === 'merged'}
                      onChange={() =>
                        setFieldSelections({ ...fieldSelections, [field.key]: 'merged' })
                      }
                      className="mt-1"
                    />
                    <div className="flex-1">
                      <div className="font-medium text-gray-900">
                        {mergedValue !== undefined && mergedValue !== null
                          ? String(mergedValue)
                          : <span className="text-gray-400 italic">Not set</span>}
                      </div>
                      <div className="text-xs text-gray-500">From merged record</div>
                    </div>
                  </label>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    );
  };

  const renderPreview = () => {
    const finalPerson: Partial<PersonDto> = { ...survivor };

    // Apply field selections
    Object.entries(fieldSelections).forEach(([key, selection]) => {
      if (selection === 'merged') {
        const mergedObj = merged as unknown as Record<string, unknown>;
        const finalObj = finalPerson as unknown as Record<string, unknown>;
        finalObj[key] = mergedObj[key];
      }
    });

    return (
      <div className="space-y-6">
        <div>
          <h2 className="text-xl font-semibold text-gray-900 mb-2">Preview Merged Person</h2>
          <p className="text-gray-600">
            Review the final merged person record before confirming.
          </p>
        </div>

        <div className="bg-white border border-gray-200 rounded-lg p-6">
          <div className="flex items-start gap-6 mb-6">
            {finalPerson.photoUrl ? (
              <img
                src={finalPerson.photoUrl}
                alt="Merged person"
                className="w-24 h-24 rounded-full object-cover"
              />
            ) : (
              <div className="w-24 h-24 rounded-full bg-gray-300 flex items-center justify-center">
                <span className="text-3xl font-semibold text-gray-600">
                  {finalPerson.firstName?.charAt(0)}{finalPerson.lastName?.charAt(0)}
                </span>
              </div>
            )}
            <div>
              <h3 className="text-2xl font-bold text-gray-900">
                {finalPerson.firstName} {finalPerson.lastName}
              </h3>
              {finalPerson.email && <p className="text-gray-600">{finalPerson.email}</p>}
            </div>
          </div>

          <dl className="grid grid-cols-2 gap-4">
            <div>
              <dt className="text-sm font-medium text-gray-500">Middle Name</dt>
              <dd className="text-sm text-gray-900">{finalPerson.middleName || '—'}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">Nick Name</dt>
              <dd className="text-sm text-gray-900">{finalPerson.nickName || '—'}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">Birth Date</dt>
              <dd className="text-sm text-gray-900">{finalPerson.birthDate || '—'}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">Gender</dt>
              <dd className="text-sm text-gray-900">{finalPerson.gender || '—'}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">Campus</dt>
              <dd className="text-sm text-gray-900">{finalPerson.primaryCampus?.name || '—'}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">Connection Status</dt>
              <dd className="text-sm text-gray-900">{finalPerson.connectionStatus?.value || '—'}</dd>
            </div>
          </dl>
        </div>

        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <h3 className="text-sm font-semibold text-blue-900 mb-2">Records to be updated:</h3>
          <ul className="text-sm text-blue-800 space-y-1">
            <li>
              {data.person1AttendanceCount + data.person2AttendanceCount} attendance records will be
              transferred to survivor
            </li>
            <li>
              {data.person1GroupMembershipCount + data.person2GroupMembershipCount} group memberships will
              be transferred to survivor
            </li>
            <li>
              ${(data.person1ContributionTotal + data.person2ContributionTotal).toFixed(2)} in contributions
              will be transferred to survivor
            </li>
          </ul>
        </div>
      </div>
    );
  };

  const renderConfirm = () => (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-gray-900 mb-2">Confirm Merge</h2>
        <p className="text-gray-600">
          This action cannot be undone. Please review and confirm.
        </p>
      </div>

      <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
        <div className="flex gap-3">
          <svg
            className="w-6 h-6 text-yellow-600 flex-shrink-0"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
            />
          </svg>
          <div>
            <h3 className="text-sm font-semibold text-yellow-900">Warning: Permanent Action</h3>
            <p className="text-sm text-yellow-800 mt-1">
              Merging will permanently combine these two person records. The merged person will be
              marked as inactive and all their relationships will be transferred to the survivor.
              This action cannot be reversed.
            </p>
          </div>
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Notes (optional)
        </label>
        <textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder="Add any notes about this merge..."
          rows={3}
          className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
        />
      </div>

      <label className="flex items-start gap-3 cursor-pointer">
        <input
          type="checkbox"
          checked={confirmed}
          onChange={(e) => setConfirmed(e.target.checked)}
          className="mt-1"
        />
        <span className="text-sm text-gray-700">
          I understand that this action cannot be undone and will permanently merge these two person
          records.
        </span>
      </label>
    </div>
  );

  return (
    <div className="space-y-8">
      {renderStepIndicator()}

      <div className="bg-white rounded-lg border border-gray-200 p-8">
        {currentStep === 'select-survivor' && renderSelectSurvivor()}
        {currentStep === 'choose-fields' && renderChooseFields()}
        {currentStep === 'preview' && renderPreview()}
        {currentStep === 'confirm' && renderConfirm()}
      </div>

      <div className="flex items-center justify-between">
        <button
          onClick={() => navigate('/admin/people/duplicates')}
          className="px-6 py-2 text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
        >
          Cancel
        </button>

        <div className="flex items-center gap-4">
          {currentStep !== 'select-survivor' && (
            <button
              onClick={handleBack}
              className="px-6 py-2 text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
            >
              Back
            </button>
          )}

          {currentStep !== 'confirm' ? (
            <button
              onClick={handleNext}
              className="px-6 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
            >
              Next
            </button>
          ) : (
            <button
              onClick={handleSubmit}
              disabled={!confirmed || mergeMutation.isPending}
              className="px-6 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {mergeMutation.isPending ? 'Merging...' : 'Confirm Merge'}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
