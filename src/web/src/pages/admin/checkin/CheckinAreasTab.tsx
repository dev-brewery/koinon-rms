/**
 * Check-in Areas Tab
 * Manage check-in areas (groups with check-in type) — create, edit, delete
 */

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useDeleteGroup } from '@/hooks/useGroups';
import { useToast } from '@/contexts/ToastContext';
import { Loading, EmptyState, ErrorState, ConfirmDialog } from '@/components/ui';
import { getCheckinConfiguration } from '@/services/api/checkin';
import type { CheckinAreaDto } from '@/services/api/types';
import { CheckinAreaFormModal } from './CheckinAreaFormModal';

export function CheckinAreasTab() {
  const toast = useToast();
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingArea, setEditingArea] = useState<CheckinAreaDto | null>(null);
  const [deletingAreaIdKey, setDeletingAreaIdKey] = useState<string | null>(null);

  const { data: config, isLoading, error, refetch } = useQuery({
    queryKey: ['checkin', 'configuration'],
    queryFn: () => getCheckinConfiguration(),
    staleTime: 2 * 60 * 1000,
  });

  const deleteGroup = useDeleteGroup();

  const areas = config?.areas ?? [];

  const handleEdit = (area: CheckinAreaDto) => {
    setEditingArea(area);
    setIsFormOpen(true);
  };

  const handleFormClose = () => {
    setIsFormOpen(false);
    setEditingArea(null);
  };

  const handleDeleteConfirm = async () => {
    if (!deletingAreaIdKey) return;
    try {
      await deleteGroup.mutateAsync(deletingAreaIdKey);
      toast.success('Area deleted', 'The check-in area has been removed.');
    } catch {
      toast.error('Delete failed', 'Could not delete the check-in area. Please try again.');
    } finally {
      setDeletingAreaIdKey(null);
    }
  };

  if (isLoading) {
    return <Loading text="Loading check-in areas..." />;
  }

  if (error) {
    return (
      <ErrorState
        title="Failed to load check-in areas"
        message={error instanceof Error ? error.message : 'Unknown error'}
        onRetry={() => refetch()}
      />
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-gray-600">
          {areas.length} {areas.length === 1 ? 'area' : 'areas'} configured
        </p>
      </div>
      <p className="text-sm text-gray-500 bg-blue-50 border border-blue-200 rounded-lg px-4 py-3">
        Check-in areas are configured through Group Management. Use the Groups page to create groups with a check-in group type.
      </p>

      {areas.length === 0 ? (
        <EmptyState
          icon={
            <svg
              className="w-12 h-12 text-gray-400"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4"
              />
            </svg>
          }
          title="No check-in areas"
          description="Use the Groups page to create groups with a check-in group type."
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {areas.map((area) => (
            <AreaCard
              key={area.idKey}
              area={area}
              onEdit={() => handleEdit(area)}
              onDelete={() => setDeletingAreaIdKey(area.idKey)}
            />
          ))}
        </div>
      )}

      {isFormOpen && (
        <CheckinAreaFormModal
          area={editingArea}
          onClose={handleFormClose}
        />
      )}

      <ConfirmDialog
        isOpen={!!deletingAreaIdKey}
        title="Delete Check-in Area"
        description="Are you sure you want to delete this check-in area? This action cannot be undone."
        confirmLabel="Delete"
        variant="danger"
        onConfirm={handleDeleteConfirm}
        onClose={() => setDeletingAreaIdKey(null)}
      />
    </div>
  );
}

// ============================================================================
// Area Card
// ============================================================================

interface AreaCardProps {
  area: CheckinAreaDto;
  onEdit: () => void;
  onDelete: () => void;
}

function AreaCard({ area, onEdit, onDelete }: AreaCardProps) {
  return (
    <div className="bg-white rounded-lg border border-gray-200 p-5 hover:shadow-md transition-shadow">
      <div className="flex items-start justify-between mb-3">
        <div className="flex-1 min-w-0">
          <h3 className="text-base font-semibold text-gray-900 truncate">{area.name}</h3>
          {area.description && (
            <p className="mt-1 text-sm text-gray-500 line-clamp-2">{area.description}</p>
          )}
        </div>
        <span
          className={`ml-3 flex-shrink-0 px-2 py-0.5 text-xs font-medium rounded-full ${
            area.isActive
              ? 'bg-green-100 text-green-800'
              : 'bg-gray-100 text-gray-600'
          }`}
        >
          {area.isActive ? 'Active' : 'Inactive'}
        </span>
      </div>

      <div className="space-y-1.5 mb-4">
        {area.schedule && (
          <div className="flex items-center gap-2 text-sm text-gray-600">
            <svg className="w-4 h-4 text-gray-400 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
            <span className="truncate">{area.schedule.name}</span>
          </div>
        )}
        <div className="flex items-center gap-2 text-sm text-gray-600">
          <svg className="w-4 h-4 text-gray-400 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
          </svg>
          <span>{area.locations.length} {area.locations.length === 1 ? 'location' : 'locations'}</span>
        </div>
        <div className="flex items-center gap-2 text-sm text-gray-600">
          <svg className="w-4 h-4 text-gray-400 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z" />
          </svg>
          <span className="truncate">{area.groupType.name}</span>
        </div>
      </div>

      <div className="flex items-center justify-end gap-2 pt-3 border-t border-gray-100">
        <button
          onClick={onEdit}
          className="px-3 py-1.5 text-sm font-medium text-primary-600 hover:text-primary-700 hover:bg-primary-50 rounded-md transition-colors"
        >
          Edit
        </button>
        <button
          onClick={onDelete}
          className="px-3 py-1.5 text-sm font-medium text-red-600 hover:text-red-700 hover:bg-red-50 rounded-md transition-colors"
        >
          Delete
        </button>
      </div>
    </div>
  );
}
