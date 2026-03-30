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
  const { person, role } = member;
  const isAdult = role.name === 'Adult';

  return (
    <div data-testid="family-member-card" className="flex items-center gap-4 p-4 bg-white border border-gray-200 rounded-lg hover:shadow-sm transition-shadow">
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
              <span className="text-xs text-gray-500">({person.age}y)</span>
            )}
          </div>
        </Link>

        <div className="flex flex-wrap items-center gap-2">
          {/* Role Badge - colored indicator with tooltip */}
          <span
            className={`inline-flex items-center w-2.5 h-2.5 rounded-full ${
              isAdult
                ? 'bg-blue-500'
                : 'bg-green-500'
            }`}
            title={role.name}
            aria-label={role.name}
          />

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
