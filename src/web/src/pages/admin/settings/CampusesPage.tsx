/**
 * Campuses Page
 * Admin page for managing campuses
 */

import { useState } from 'react';
import { useCampuses, useDeleteCampus } from '@/hooks/useCampuses';
import { CampusCard } from '@/components/admin/CampusCard';
import { CampusEditorModal } from '@/components/admin/CampusEditorModal';
import type { CampusDto } from '@/services/api/types';

export function CampusesPage() {
  const [includeInactive, setIncludeInactive] = useState(false);
  const [editingCampus, setEditingCampus] = useState<CampusDto | null>(null);
  const [isCreating, setIsCreating] = useState(false);

  const { data: campuses = [], isLoading, error } = useCampuses(includeInactive);
  const deleteMutation = useDeleteCampus();

  const handleEdit = (campus: CampusDto) => {
    setEditingCampus(campus);
  };

  const handleCreate = () => {
    setIsCreating(true);
  };

  const handleCloseEditor = () => {
    setEditingCampus(null);
    setIsCreating(false);
  };

  const handleDelete = async (campus: CampusDto) => {
    if (!window.confirm(`Are you sure you want to delete "${campus.name}"?`)) {
      return;
    }

    try {
      await deleteMutation.mutateAsync(campus.idKey);
    } catch (error) {
      console.error('Failed to delete campus:', error);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Campuses</h1>
          <p className="mt-2 text-gray-600">Manage your organization's campus locations</p>
        </div>
        <button
          onClick={handleCreate}
          className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
        >
          Create Campus
        </button>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <div className="flex items-center">
          <input
            type="checkbox"
            id="includeInactive"
            checked={includeInactive}
            onChange={(e) => setIncludeInactive(e.target.checked)}
            className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
          />
          <label htmlFor="includeInactive" className="ml-2 text-sm text-gray-700">
            Include inactive campuses
          </label>
        </div>
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="flex items-center justify-center py-12">
          <div className="inline-block w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
        </div>
      )}

      {/* Error State */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex items-center gap-2">
            <svg
              className="w-5 h-5 text-red-600"
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
            <p className="text-sm font-medium text-red-800">Failed to load campuses</p>
          </div>
        </div>
      )}

      {/* Campuses Grid */}
      {!isLoading && !error && (
        <div className="space-y-4">
          {campuses.length === 0 ? (
            <div className="bg-white rounded-lg border border-gray-200 p-12 text-center">
              <svg
                className="w-12 h-12 text-gray-400 mx-auto mb-4"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
                />
              </svg>
              <p className="text-gray-500 mb-4">No campuses found</p>
              <button
                onClick={handleCreate}
                className="inline-block px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
              >
                Create First Campus
              </button>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {campuses.map((campus) => (
                <CampusCard
                  key={campus.idKey}
                  campus={campus}
                  onEdit={() => handleEdit(campus)}
                  onDelete={() => handleDelete(campus)}
                />
              ))}
            </div>
          )}
        </div>
      )}

      {/* Editor Modal */}
      <CampusEditorModal
        isOpen={isCreating || editingCampus !== null}
        onClose={handleCloseEditor}
        campus={editingCampus || undefined}
      />
    </div>
  );
}
