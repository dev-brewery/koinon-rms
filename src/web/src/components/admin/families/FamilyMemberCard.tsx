/**
 * Family Member Card Component
 * Card for displaying a family member with remove option
 */

import { Link } from 'react-router-dom';
import type { FamilyMemberDto } from '@/services/api/types';

interface FamilyMemberCardProps {
  member: FamilyMemberDto;
  onRemove?: () => void;
  readOnly?: boolean;
}

export function FamilyMemberCard({ member, onRemove, readOnly = false }: FamilyMemberCardProps) {
  const { person, role, isPersonPrimaryFamily } = member;
  const isAdult = role.name === 'Adult';

  return (
    <div className="flex items-center gap-4 p-4 bg-white border border-gray-200 rounded-lg hover:shadow-sm transition-shadow">
      {/* Avatar */}
      <Link to={`/admin/people/${person.idKey}`} className="flex-shrink-0">
        {person.photoUrl ? (
          <img
            src={person.photoUrl}
            alt={person.fullName}
            className="w-12 h-12 rounded-full object-cover"
          />
        ) : (
          <div className="w-12 h-12 rounded-full bg-gray-200 flex items-center justify-center">
            <svg
              className="w-6 h-6 text-gray-400"
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
      </Link>

      {/* Info */}
      <div className="flex-1 min-w-0">
        <Link
          to={`/admin/people/${person.idKey}`}
          className="block hover:text-primary-600 transition-colors"
        >
          <div className="flex items-center gap-2 mb-1">
            <h3 className="text-sm font-medium text-gray-900 truncate">
              {person.fullName}
            </h3>
            {person.age !== undefined && (
              <span className="text-xs text-gray-500">({person.age})</span>
            )}
          </div>
        </Link>

        <div className="flex flex-wrap items-center gap-2">
          {/* Role Badge */}
          <span
            className={`inline-flex items-center px-2 py-0.5 text-xs font-medium rounded-full ${
              isAdult
                ? 'bg-blue-100 text-blue-800'
                : 'bg-green-100 text-green-800'
            }`}
          >
            {role.name}
          </span>

          {/* Primary Family Badge */}
          {isPersonPrimaryFamily && (
            <span className="inline-flex items-center px-2 py-0.5 text-xs font-medium rounded-full bg-purple-100 text-purple-800">
              Primary Family
            </span>
          )}

          {person.email && (
            <span className="text-xs text-gray-500 truncate">{person.email}</span>
          )}
        </div>
      </div>

      {/* Remove Button */}
      {!readOnly && onRemove && (
        <button
          onClick={onRemove}
          className="flex-shrink-0 p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-full transition-colors"
          aria-label={`Remove ${person.fullName} from family`}
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
              d="M6 18L18 6M6 6l12 12"
            />
          </svg>
        </button>
      )}
    </div>
  );
}
