/**
 * Person Card Component
 * Card for displaying person in list view
 */

import { Link } from 'react-router-dom';
import type { PersonSummaryDto } from '@/services/api/types';

interface PersonCardProps {
  person: PersonSummaryDto;
}

export function PersonCard({ person }: PersonCardProps) {
  return (
    <Link
      to={`/admin/people/${person.idKey}`}
      className="block p-4 hover:bg-gray-50 transition-colors"
    >
      <div className="flex items-center gap-4">
        {/* Avatar */}
        <div className="flex-shrink-0">
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
        </div>

        {/* Info */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-1">
            <h3 className="text-sm font-medium text-gray-900 truncate">
              {person.fullName}
            </h3>
            {person.age !== undefined && (
              <span className="text-xs text-gray-500">({person.age})</span>
            )}
          </div>

          <div className="flex flex-wrap items-center gap-2 text-xs text-gray-500">
            {person.email && (
              <span className="truncate">{person.email}</span>
            )}
            {person.gender !== 'Unknown' && (
              <span className="px-2 py-0.5 bg-gray-100 rounded-full">
                {person.gender}
              </span>
            )}
          </div>

          {/* Badges */}
          <div className="flex flex-wrap gap-2 mt-2">
            {person.connectionStatus && (
              <span className="inline-flex items-center px-2 py-0.5 text-xs font-medium rounded-full bg-blue-100 text-blue-800">
                {person.connectionStatus.value}
              </span>
            )}
            {person.recordStatus && person.recordStatus.value !== 'Active' && (
              <span className="inline-flex items-center px-2 py-0.5 text-xs font-medium rounded-full bg-yellow-100 text-yellow-800">
                {person.recordStatus.value}
              </span>
            )}
            {person.primaryCampus && (
              <span className="inline-flex items-center px-2 py-0.5 text-xs font-medium rounded-full bg-purple-100 text-purple-800">
                {person.primaryCampus.name}
              </span>
            )}
          </div>
        </div>

        {/* Arrow */}
        <div className="flex-shrink-0">
          <svg
            className="w-5 h-5 text-gray-400"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M9 5l7 7-7 7"
            />
          </svg>
        </div>
      </div>
    </Link>
  );
}
