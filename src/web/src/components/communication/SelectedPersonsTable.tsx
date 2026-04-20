/**
 * SelectedPersonsTable Component
 * Displays the list of individually selected recipients before sending
 */

import type { PersonSummaryDto } from '@/services/api/types';

interface SelectedPersonsTableProps {
  persons: PersonSummaryDto[];
  onRemove: (idKey: string) => void;
}

export function SelectedPersonsTable({ persons, onRemove }: SelectedPersonsTableProps) {
  if (persons.length === 0) {
    return null;
  }

  return (
    <div data-testid="recipient-table" className="mt-3 border border-gray-200 rounded-lg overflow-hidden">
      <div className="bg-gray-50 px-4 py-2 border-b border-gray-200">
        <p className="text-xs font-medium text-gray-600 uppercase tracking-wide">
          Individual Recipients ({persons.length})
        </p>
      </div>
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-gray-200">
            <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase tracking-wide">
              Name
            </th>
            <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase tracking-wide">
              Email
            </th>
            <th className="w-10 px-4 py-2" aria-label="Actions" />
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100">
          {persons.map((person) => (
            <tr key={person.idKey} className="hover:bg-gray-50">
              <td className="px-4 py-2 text-gray-900">{person.fullName}</td>
              <td className="px-4 py-2 text-gray-500">{person.email ?? '—'}</td>
              <td className="px-4 py-2 text-right">
                <button
                  type="button"
                  data-testid={`remove-person-btn-${person.idKey}`}
                  onClick={() => onRemove(person.idKey)}
                  className="text-gray-400 hover:text-red-600 transition-colors"
                  aria-label={`Remove ${person.fullName}`}
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M6 18L18 6M6 6l12 12"
                    />
                  </svg>
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
