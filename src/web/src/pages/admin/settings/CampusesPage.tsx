/**
 * Campuses Page
 * Admin page for managing campuses
 */

import { useState } from 'react';
import { useCampuses, useDeleteCampus } from '@/hooks/useCampuses';
import { CampusCard } from '@/components/admin/CampusCard';
import { CampusEditorModal } from '@/components/admin/CampusEditorModal';
import { Loading, EmptyState, ErrorState } from '@/components/ui';
import { getErrorMessage } from '@/lib/errorMessages';
import type { CampusDto } from '@/services/api/types';

export function CampusesPage() {
  const [includeInactive, setIncludeInactive] = useState(false);
  const [editingCampus, setEditingCampus] = useState<CampusDto | null>(null);
  const [isCreating, setIsCreating] = useState(false);

  const { data: campuses = [], isLoading, error, refetch } = useCampuses(includeInactive);
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
        <div className="bg-white rounded-lg border border-gray-200">
          <Loading text="Loading campuses..." />
        </div>
      )}

      {/* Error State */}
      {error && (
        <div className="bg-white rounded-lg border border-gray-200">
          <ErrorState
            title="Failed to load campuses"
            message={getErrorMessage(error).message}
            onRetry={() => refetch()}
          />
        </div>
      )}

      {/* Campuses Grid */}
      {!isLoading && !error && (
        <div className="space-y-4">
          {campuses.length === 0 ? (
            <div className="bg-white rounded-lg border border-gray-200">
              <EmptyState
                title="No campuses yet"
                description="Campuses represent physical or virtual locations where your ministry meets. Create one to organize people and events."
                action={{
                  label: 'Create First Campus',
                  onClick: handleCreate,
                }}
              />
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
