/**
 * Person Comparison View
 * Side-by-side comparison of two people for merge preview
 */

import { useNavigate } from 'react-router-dom';
import { usePersonComparison, useIgnoreDuplicate } from '@/hooks/usePersonMerge';
import { Loading, ErrorState } from '@/components/ui';
import { useToast } from '@/contexts/ToastContext';
import type { PersonDto } from '@/types/person';

interface PersonComparisonViewProps {
  person1IdKey: string;
  person2IdKey: string;
}

export function PersonComparisonView({ person1IdKey, person2IdKey }: PersonComparisonViewProps) {
  const navigate = useNavigate();
  const { success: showSuccess, error: showError } = useToast();
  const { data, isLoading, error } = usePersonComparison(person1IdKey, person2IdKey);
  const ignoreMutation = useIgnoreDuplicate();

  if (isLoading) {
    return <Loading text="Loading comparison..." />;
  }

  if (error) {
    return (
      <ErrorState
        title="Failed to load comparison"
        message={error instanceof Error ? error.message : 'Unknown error'}
      />
    );
  }

  if (!data) {
    return null;
  }

  const { person1, person2 } = data;

  const handleMerge = () => {
    navigate(`/admin/people/merge?person1=${person1IdKey}&person2=${person2IdKey}`);
  };

  const handleIgnore = async () => {
    try {
      await ignoreMutation.mutateAsync({
        person1IdKey,
        person2IdKey,
      });
      showSuccess('Success', 'Marked as not duplicates');
      navigate('/admin/people/duplicates');
    } catch (err) {
      showError('Error', 'Failed to mark as not duplicates');
    }
  };

  const isDifferent = (field1: unknown, field2: unknown): boolean => {
    if (field1 === undefined && field2 === undefined) return false;
    if (field1 === null && field2 === null) return false;
    return field1 !== field2;
  };

  const renderField = (
    label: string,
    value1: string | number | undefined,
    value2: string | number | undefined
  ) => {
    const different = isDifferent(value1, value2);
    return (
      <div className={`py-3 ${different ? 'bg-yellow-50' : ''}`}>
        <dt className="text-sm font-medium text-gray-500 mb-1">{label}</dt>
        <div className="grid grid-cols-2 gap-4">
          <dd className="text-sm text-gray-900">
            {value1 !== undefined && value1 !== null ? String(value1) : (
              <span className="text-gray-400 italic">Not set</span>
            )}
          </dd>
          <dd className="text-sm text-gray-900">
            {value2 !== undefined && value2 !== null ? String(value2) : (
              <span className="text-gray-400 italic">Not set</span>
            )}
          </dd>
        </div>
      </div>
    );
  };

  const renderPersonHeader = (person: PersonDto) => (
    <div className="flex flex-col items-center gap-4 p-6 bg-gray-50 rounded-lg">
      {person.photoUrl ? (
        <img
          src={person.photoUrl}
          alt={`${person.firstName} ${person.lastName}`}
          className="w-24 h-24 rounded-full object-cover"
        />
      ) : (
        <div className="w-24 h-24 rounded-full bg-gray-300 flex items-center justify-center">
          <span className="text-3xl font-semibold text-gray-600">
            {person.firstName.charAt(0)}{person.lastName.charAt(0)}
          </span>
        </div>
      )}
      <div className="text-center">
        <h3 className="text-xl font-bold text-gray-900">
          {person.firstName} {person.lastName}
        </h3>
        {person.nickName && (
          <p className="text-sm text-gray-600">"{person.nickName}"</p>
        )}
      </div>
    </div>
  );

  return (
    <div className="space-y-6">
      {/* Headers */}
      <div className="grid grid-cols-2 gap-6">
        {renderPersonHeader(person1)}
        {renderPersonHeader(person2)}
      </div>

      {/* Comparison Fields */}
      <div className="bg-white rounded-lg border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200">
          <h3 className="text-lg font-semibold text-gray-900">Field Comparison</h3>
          <p className="text-sm text-gray-600 mt-1">
            Fields highlighted in yellow differ between the two records
          </p>
        </div>

        <dl className="px-6 divide-y divide-gray-200">
          {/* Basic Info */}
          {renderField('First Name', person1.firstName, person2.firstName)}
          {renderField('Middle Name', person1.middleName, person2.middleName)}
          {renderField('Last Name', person1.lastName, person2.lastName)}
          {renderField('Nick Name', person1.nickName, person2.nickName)}

          {/* Contact */}
          {renderField('Email', person1.email, person2.email)}
          {renderField(
            'Phone Numbers',
            person1.phoneNumbers.join(', ') || undefined,
            person2.phoneNumbers.join(', ') || undefined
          )}

          {/* Demographics */}
          {renderField('Birth Date', person1.birthDate, person2.birthDate)}
          {renderField('Gender', person1.gender, person2.gender)}

          {/* Location */}
          {renderField('Campus', person1.primaryCampus?.name, person2.primaryCampus?.name)}

          {/* Status */}
          {renderField('Connection Status', person1.connectionStatus?.value, person2.connectionStatus?.value)}
          {renderField('Record Status', person1.recordStatus?.value, person2.recordStatus?.value)}

          {/* Relationships */}
          {renderField('Attendance Count', data.person1AttendanceCount, data.person2AttendanceCount)}
          {renderField('Group Memberships', data.person1GroupMembershipCount, data.person2GroupMembershipCount)}
          {renderField(
            'Contribution Total',
            data.person1ContributionTotal > 0 ? `${data.person1ContributionTotal.toFixed(2)}` : '$0.00',
            data.person2ContributionTotal > 0 ? `${data.person2ContributionTotal.toFixed(2)}` : '$0.00'
          )}
        </dl>
      </div>

      {/* Actions */}
      <div className="flex items-center justify-end gap-4">
        <button
          onClick={() => navigate('/admin/people/duplicates')}
          className="px-6 py-2 text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
        >
          Cancel
        </button>
        <button
          onClick={handleIgnore}
          disabled={ignoreMutation.isPending}
          className="px-6 py-2 text-orange-700 border border-orange-300 rounded-lg hover:bg-orange-50 transition-colors disabled:opacity-50"
        >
          Mark as Not Duplicates
        </button>
        <button
          onClick={handleMerge}
          className="px-6 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
        >
          Merge These People
        </button>
      </div>
    </div>
  );
}
