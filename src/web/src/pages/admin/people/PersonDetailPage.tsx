/**
 * Person Detail Page
 * View detailed information about a person
 */

import { useParams, Link, useNavigate } from 'react-router-dom';
import { usePerson, usePersonFamily, usePersonGroups, useDeletePerson } from '@/hooks/usePeople';
import { CommunicationPreferences } from '@/components/admin/people/CommunicationPreferences';

export function PersonDetailPage() {
  const { idKey } = useParams<{ idKey: string }>();
  const navigate = useNavigate();

  const { data: person, isLoading, error } = usePerson(idKey);
  const { data: familyData } = usePersonFamily(idKey);
  const { data: groupsData } = usePersonGroups(idKey);

  const deleteMutation = useDeletePerson();

  const handleDelete = async () => {
    if (!idKey) return;

    const confirmed = window.confirm(
      `Are you sure you want to delete ${person?.fullName}? This will set their record status to Inactive.`
    );

    if (confirmed) {
      try {
        await deleteMutation.mutateAsync(idKey);
        navigate('/admin/people');
      } catch (err) {
        alert('Failed to delete person. Please try again.');
      }
    }
  };

  if (isLoading) {
    return (
      <div className="p-12 text-center">
        <div className="inline-block w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
        <p className="mt-4 text-gray-500">Loading person...</p>
      </div>
    );
  }

  if (error || !person) {
    return (
      <div className="p-12 text-center">
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
        <p className="text-red-600">Failed to load person</p>
        <Link to="/admin/people" className="text-primary-600 hover:text-primary-700 mt-4 inline-block">
          Back to People
        </Link>
      </div>
    );
  }

  const family = familyData?.family;
  const familyMembers = familyData?.members || [];
  const groups = groupsData?.data || [];

  return (
    <div className="space-y-6">
      {/* Back Link */}
      <Link
        to="/admin/people"
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
        Back to People
      </Link>

      {/* Header */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="flex items-start justify-between">
          <div className="flex items-center gap-4">
            {/* Avatar */}
            {person.photoUrl ? (
              <img
                src={person.photoUrl}
                alt={person.fullName}
                className="w-20 h-20 rounded-full object-cover"
              />
            ) : (
              <div className="w-20 h-20 rounded-full bg-gray-200 flex items-center justify-center">
                <svg
                  className="w-10 h-10 text-gray-400"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
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

            <div>
              <h1 className="text-2xl font-bold text-gray-900">{person.fullName}</h1>
              <div className="flex items-center gap-2 mt-1 text-gray-600">
                {person.age !== undefined && <span>{person.age} years old</span>}
                {person.gender !== 'Unknown' && (
                  <>
                    <span>•</span>
                    <span>{person.gender}</span>
                  </>
                )}
              </div>
              <div className="flex gap-2 mt-2">
                {person.connectionStatus && (
                  <span className="inline-flex items-center px-2 py-0.5 text-xs font-medium rounded-full bg-blue-100 text-blue-800">
                    {person.connectionStatus.value}
                  </span>
                )}
                {person.recordStatus && (
                  <span className="inline-flex items-center px-2 py-0.5 text-xs font-medium rounded-full bg-gray-100 text-gray-800">
                    {person.recordStatus.value}
                  </span>
                )}
              </div>
            </div>
          </div>

          <div className="flex gap-2">
            <Link
              to={`/admin/people/${idKey}/edit`}
              className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
            >
              Edit
            </Link>
            <button
              onClick={handleDelete}
              disabled={deleteMutation.isPending}
              className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50"
            >
              Delete
            </button>
          </div>
        </div>
      </div>

      {/* Demographics */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Demographics</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {person.birthDate && (
            <div>
              <label className="text-sm font-medium text-gray-700">Birth Date</label>
              <p className="text-gray-900">{person.birthDate}</p>
            </div>
          )}
          <div>
            <label className="text-sm font-medium text-gray-700">Gender</label>
            <p className="text-gray-900">{person.gender}</p>
          </div>
          {person.maritalStatus && (
            <div>
              <label className="text-sm font-medium text-gray-700">Marital Status</label>
              <p className="text-gray-900">{person.maritalStatus.value}</p>
            </div>
          )}
          {person.primaryCampus && (
            <div>
              <label className="text-sm font-medium text-gray-700">Primary Campus</label>
              <p className="text-gray-900">{person.primaryCampus.name}</p>
            </div>
          )}
        </div>
      </div>

      {/* Contact */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Contact Information</h2>
        <div className="space-y-4">
          {person.email && (
            <div>
              <label className="text-sm font-medium text-gray-700">Email</label>
              <p className="text-gray-900">
                <a href={`mailto:${person.email}`} className="text-primary-600 hover:text-primary-700">
                  {person.email}
                </a>
                {!person.isEmailActive && (
                  <span className="ml-2 text-xs text-red-600">(Inactive)</span>
                )}
              </p>
            </div>
          )}
          {person.phoneNumbers.length > 0 && (
            <div>
              <label className="text-sm font-medium text-gray-700">Phone Numbers</label>
              <ul className="space-y-1 mt-1">
                {person.phoneNumbers.map((phone) => (
                  <li key={phone.idKey} className="text-gray-900">
                    <a href={`tel:${phone.number}`} className="text-primary-600 hover:text-primary-700">
                      {phone.numberFormatted}
                    </a>
                    {phone.phoneType && (
                      <span className="ml-2 text-xs text-gray-500">({phone.phoneType.value})</span>
                    )}
                    {phone.isMessagingEnabled && (
                      <span className="ml-2 text-xs text-green-600">SMS</span>
                    )}
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      </div>

      {/* Communication Preferences */}
      <CommunicationPreferences personIdKey={idKey!} />

      {/* Family */}
      {family && (
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Family</h2>
          <p className="text-sm text-gray-600 mb-3">{family.name}</p>
          <ul className="space-y-2">
            {familyMembers.map((member) => (
              <li key={member.person.idKey} className="flex items-center gap-2">
                <Link
                  to={`/admin/people/${member.person.idKey}`}
                  className="text-primary-600 hover:text-primary-700"
                >
                  {member.person.fullName}
                </Link>
                <span className="text-xs text-gray-500">({member.role.name})</span>
                {member.isPersonPrimaryFamily && (
                  <span className="text-xs text-blue-600">(Primary)</span>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Groups */}
      {groups.length > 0 && (
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Groups</h2>
          <ul className="space-y-2">
            {groups.map((membership) => (
              <li key={membership.group.idKey} className="flex items-center gap-2">
                <Link
                  to={`/admin/groups/${membership.group.idKey}`}
                  className="text-primary-600 hover:text-primary-700"
                >
                  {membership.group.name}
                </Link>
                <span className="text-xs text-gray-500">
                  ({membership.role.name} - {membership.status})
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
