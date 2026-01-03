import type { CampusDto } from '@/types';

export interface CampusCardProps {
  campus: CampusDto;
  onEdit: () => void;
  onDelete: () => void;
}

export function CampusCard({ campus, onEdit, onDelete }: CampusCardProps) {
  return (
    <div className="bg-white rounded-lg border border-gray-200 p-4 hover:shadow-md transition-shadow">
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-2">
            <h3 className="font-semibold text-gray-900">{campus.name}</h3>
            {campus.shortCode && (
              <span className="px-2 py-0.5 bg-gray-100 text-gray-600 text-xs rounded">
                {campus.shortCode}
              </span>
            )}
            {!campus.isActive && (
              <span className="px-2 py-0.5 bg-yellow-100 text-yellow-800 text-xs rounded">
                Inactive
              </span>
            )}
          </div>
          {campus.description && (
            <p className="mt-1 text-sm text-gray-600 line-clamp-2">{campus.description}</p>
          )}
          <div className="mt-2 flex flex-wrap gap-2 text-xs text-gray-500">
            {campus.timeZoneId && <span>{campus.timeZoneId}</span>}
            {campus.phoneNumber && <span>{campus.phoneNumber}</span>}
          </div>
        </div>
        <div className="flex gap-2 ml-4">
          <button
            onClick={onEdit}
            className="p-2 text-gray-400 hover:text-primary-600 transition-colors"
            title="Edit campus"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
          <button
            onClick={onDelete}
            className="p-2 text-gray-400 hover:text-red-600 transition-colors"
            title="Delete campus"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          </button>
        </div>
      </div>
    </div>
  );
}
