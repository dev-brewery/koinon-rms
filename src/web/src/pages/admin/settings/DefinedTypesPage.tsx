/**
 * Defined Types Page
 * Admin page for managing configurable lookup values
 */

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  useDefinedTypes,
  useDefinedType,
  useCreateDefinedValue,
  useUpdateDefinedValue,
  useDeleteDefinedValue,
  useReorderDefinedValues,
} from '@/features/admin/defined-types';
import type { DefinedTypeSummary, DefinedValueDto } from '@/features/admin/defined-types';
import { Loading, EmptyState, ErrorState } from '@/components/ui';
import { getErrorMessage } from '@/lib/errorMessages';
import type { IdKey } from '@/services/api/types';

// ============================================================================
// Form Schema
// ============================================================================

const definedValueSchema = z.object({
  value: z.string().min(1, 'Value is required'),
  description: z.string().optional(),
  order: z.number().int().min(0, 'Order must be 0 or greater'),
});

type DefinedValueFormData = z.infer<typeof definedValueSchema>;

// ============================================================================
// ValueForm: inline add/edit form
// ============================================================================

interface ValueFormProps {
  typeIdKey: IdKey;
  editingValue: DefinedValueDto | null;
  onClose: () => void;
}

function ValueForm({ typeIdKey, editingValue, onClose }: ValueFormProps) {
  const createMutation = useCreateDefinedValue();
  const updateMutation = useUpdateDefinedValue();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<DefinedValueFormData>({
    resolver: zodResolver(definedValueSchema),
    defaultValues: {
      value: editingValue?.value ?? '',
      description: editingValue?.description ?? '',
      order: editingValue?.order ?? 0,
    },
  });

  const onSubmit = async (data: DefinedValueFormData) => {
    if (editingValue) {
      await updateMutation.mutateAsync({
        valueIdKey: editingValue.idKey,
        typeIdKey,
        request: {
          value: data.value,
          description: data.description || undefined,
          order: data.order,
          isActive: editingValue.isActive,
        },
      });
    } else {
      await createMutation.mutateAsync({
        typeIdKey,
        request: {
          value: data.value,
          description: data.description || undefined,
          order: data.order,
        },
      });
    }
    onClose();
  };

  return (
    <div className="bg-gray-50 border border-gray-200 rounded-lg p-4 mt-3">
      <h4 className="text-sm font-semibold text-gray-800 mb-3">
        {editingValue ? 'Edit Value' : 'Add Value'}
      </h4>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
        <div>
          <label htmlFor="form-value" className="block text-xs font-medium text-gray-700 mb-1">
            Value <span className="text-red-500">*</span>
          </label>
          <input
            id="form-value"
            type="text"
            placeholder="Enter value"
            {...register('value')}
            className="w-full rounded-md border border-gray-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          />
          {errors.value && (
            <p className="mt-1 text-xs text-red-600">{errors.value.message}</p>
          )}
        </div>

        <div>
          <label htmlFor="form-description" className="block text-xs font-medium text-gray-700 mb-1">
            Description
          </label>
          <input
            id="form-description"
            type="text"
            {...register('description')}
            className="w-full rounded-md border border-gray-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          />
        </div>

        <div>
          <label htmlFor="form-order" className="block text-xs font-medium text-gray-700 mb-1">
            Order
          </label>
          <input
            id="form-order"
            type="number"
            min={0}
            {...register('order', { valueAsNumber: true })}
            className="w-24 rounded-md border border-gray-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          />
          {errors.order && (
            <p className="mt-1 text-xs text-red-600">{errors.order.message}</p>
          )}
        </div>

        <div className="flex gap-2 pt-1">
          <button
            type="submit"
            disabled={isSubmitting}
            className="px-3 py-1.5 bg-primary-600 text-white text-sm rounded-md hover:bg-primary-700 transition-colors disabled:opacity-50"
          >
            {isSubmitting ? 'Saving...' : editingValue ? 'Save Changes' : 'Add Value'}
          </button>
          <button
            type="button"
            onClick={onClose}
            className="px-3 py-1.5 bg-white text-gray-700 text-sm border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}

// ============================================================================
// ExpandedTypeContent: shows values table + add/edit forms
// ============================================================================

interface ExpandedTypeContentProps {
  typeIdKey: IdKey;
  isSystem: boolean;
}

function ExpandedTypeContent({ typeIdKey, isSystem }: ExpandedTypeContentProps) {
  const [editingValueIdKey, setEditingValueIdKey] = useState<IdKey | null>(null);
  const [showAddForm, setShowAddForm] = useState(false);

  const { data: typeDetail, isLoading, error } = useDefinedType(typeIdKey);
  const reorderMutation = useReorderDefinedValues();
  const deleteMutation = useDeleteDefinedValue();

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="inline-block w-6 h-6 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
      </div>
    );
  }

  if (error || !typeDetail) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-3 text-sm text-red-700">
        Failed to load values for this type.
      </div>
    );
  }

  const sortedValues = [...typeDetail.definedValues].sort((a, b) => a.order - b.order);

  const handleMoveUp = async (value: DefinedValueDto) => {
    const index = sortedValues.findIndex((v) => v.idKey === value.idKey);
    if (index <= 0) return;
    const prev = sortedValues[index - 1];
    const items = sortedValues.map((v, i) => {
      if (i === index) return { idKey: v.idKey, order: prev.order };
      if (i === index - 1) return { idKey: v.idKey, order: value.order };
      return { idKey: v.idKey, order: v.order };
    });
    await reorderMutation.mutateAsync({ typeIdKey, request: { items } });
  };

  const handleMoveDown = async (value: DefinedValueDto) => {
    const index = sortedValues.findIndex((v) => v.idKey === value.idKey);
    if (index >= sortedValues.length - 1) return;
    const next = sortedValues[index + 1];
    const items = sortedValues.map((v, i) => {
      if (i === index) return { idKey: v.idKey, order: next.order };
      if (i === index + 1) return { idKey: v.idKey, order: value.order };
      return { idKey: v.idKey, order: v.order };
    });
    await reorderMutation.mutateAsync({ typeIdKey, request: { items } });
  };

  const handleDeactivate = async (value: DefinedValueDto) => {
    await deleteMutation.mutateAsync({ valueIdKey: value.idKey, typeIdKey });
  };

  const editingValue = editingValueIdKey
    ? (sortedValues.find((v) => v.idKey === editingValueIdKey) ?? null)
    : null;

  const isBusy = reorderMutation.isPending || deleteMutation.isPending;

  return (
    <div className="mt-3">
      {typeDetail.description && (
        <p className="text-sm text-gray-600 mb-3">{typeDetail.description}</p>
      )}
      {typeDetail.helpText && (
        <p className="text-xs text-gray-500 italic mb-3">{typeDetail.helpText}</p>
      )}

      {sortedValues.length === 0 ? (
        <p className="text-sm text-gray-500 py-2">No values defined yet.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="min-w-full text-left">
            <thead>
              <tr className="border-b border-gray-200">
                <th className="py-2 px-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">
                  Value
                </th>
                <th className="py-2 px-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">
                  Description
                </th>
                <th className="py-2 px-3 text-xs font-semibold text-gray-500 uppercase tracking-wide text-center">
                  Order
                </th>
                <th className="py-2 px-3 text-xs font-semibold text-gray-500 uppercase tracking-wide text-center">
                  Status
                </th>
                <th className="py-2 px-3 text-xs font-semibold text-gray-500 uppercase tracking-wide text-right">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {sortedValues.map((value, index) => {
                const isFirst = index === 0;
                const isLast = index === sortedValues.length - 1;

                return (
                  <tr key={value.idKey} className={value.isActive ? '' : 'opacity-50'}>
                    <td className="py-2 px-3 text-sm">
                      <span
                        className={
                          value.isActive ? 'text-gray-900' : 'line-through text-gray-500'
                        }
                      >
                        {value.value}
                      </span>
                    </td>
                    <td className="py-2 px-3 text-sm text-gray-600">
                      {value.description ?? (
                        <span className="text-gray-400 italic">—</span>
                      )}
                    </td>
                    <td className="py-2 px-3 text-sm text-gray-600 text-center">
                      {value.order}
                    </td>
                    <td className="py-2 px-3 text-center">
                      <span
                        className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${
                          value.isActive
                            ? 'bg-green-100 text-green-800'
                            : 'bg-gray-100 text-gray-600'
                        }`}
                      >
                        {value.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="py-2 px-3">
                      <div className="flex items-center gap-1 justify-end">
                        {/* Move Up */}
                        <button
                          type="button"
                          onClick={() => handleMoveUp(value)}
                          disabled={isFirst || isBusy}
                          className="p-1 text-gray-400 hover:text-gray-700 transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
                          title="Move up"
                        >
                          <svg
                            className="w-4 h-4"
                            fill="none"
                            stroke="currentColor"
                            viewBox="0 0 24 24"
                            aria-hidden="true"
                          >
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={2}
                              d="M5 15l7-7 7 7"
                            />
                          </svg>
                        </button>
                        {/* Move Down */}
                        <button
                          type="button"
                          onClick={() => handleMoveDown(value)}
                          disabled={isLast || isBusy}
                          className="p-1 text-gray-400 hover:text-gray-700 transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
                          title="Move down"
                        >
                          <svg
                            className="w-4 h-4"
                            fill="none"
                            stroke="currentColor"
                            viewBox="0 0 24 24"
                            aria-hidden="true"
                          >
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={2}
                              d="M19 9l-7 7-7-7"
                            />
                          </svg>
                        </button>
                        {/* Edit */}
                        <button
                          type="button"
                          onClick={() => {
                            setShowAddForm(false);
                            setEditingValueIdKey(value.idKey);
                          }}
                          className="p-1 text-gray-500 hover:text-primary-600 transition-colors"
                          title="Edit value"
                        >
                          <svg
                            className="w-4 h-4"
                            fill="none"
                            stroke="currentColor"
                            viewBox="0 0 24 24"
                            aria-hidden="true"
                          >
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={2}
                              d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
                            />
                          </svg>
                        </button>
                        {/* Deactivate */}
                        <button
                          type="button"
                          onClick={() => handleDeactivate(value)}
                          disabled={isBusy || !value.isActive}
                          className="p-1 text-gray-500 hover:text-red-600 transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
                          title={value.isActive ? 'Deactivate value' : 'Already inactive'}
                        >
                          <svg
                            className="w-4 h-4"
                            fill="none"
                            stroke="currentColor"
                            viewBox="0 0 24 24"
                            aria-hidden="true"
                          >
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={2}
                              d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636"
                            />
                          </svg>
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Edit form (inline) */}
      {editingValueIdKey !== null && editingValue !== null && (
        <ValueForm
          typeIdKey={typeIdKey}
          editingValue={editingValue}
          onClose={() => setEditingValueIdKey(null)}
        />
      )}

      {/* Add form (inline) */}
      {showAddForm && editingValueIdKey === null && (
        <ValueForm
          typeIdKey={typeIdKey}
          editingValue={null}
          onClose={() => setShowAddForm(false)}
        />
      )}

      {/* Add Value button — hidden while a form is open */}
      {!showAddForm && editingValueIdKey === null && (
        <button
          type="button"
          onClick={() => setShowAddForm(true)}
          className="mt-3 inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-primary-600 border border-primary-300 rounded-md hover:bg-primary-50 transition-colors"
        >
          <svg
            className="w-4 h-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          Add Value
          {isSystem && (
            <span className="ml-1 text-xs text-gray-500">
              (system type — values can be added)
            </span>
          )}
        </button>
      )}
    </div>
  );
}

// ============================================================================
// DefinedTypeRow: collapsible accordion row
// ============================================================================

interface DefinedTypeRowProps {
  type: DefinedTypeSummary;
  isExpanded: boolean;
  onToggle: () => void;
}

function DefinedTypeRow({ type, isExpanded, onToggle }: DefinedTypeRowProps) {
  return (
    <div
      className="bg-white rounded-lg border border-gray-200 overflow-hidden"
      data-testid="defined-type-row"
    >
      <button
        type="button"
        onClick={onToggle}
        className="w-full flex items-center justify-between p-4 text-left hover:bg-gray-50 transition-colors"
        aria-expanded={isExpanded}
      >
        <div className="flex items-center gap-3 min-w-0">
          <svg
            className={`w-4 h-4 text-gray-400 flex-shrink-0 transition-transform ${
              isExpanded ? 'rotate-90' : ''
            }`}
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
          </svg>
          <div className="flex items-center gap-2 flex-wrap min-w-0">
            <span className="font-medium text-gray-900">{type.name}</span>
            {type.isSystem && (
              <span
                className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800"
                title="System type — cannot be deleted"
                data-testid="system-type-badge"
              >
                <svg
                  className="w-3 h-3"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
                  />
                </svg>
                System
              </span>
            )}
            <span className="text-xs text-gray-500 bg-gray-100 px-2 py-0.5 rounded-full">
              {type.category}
            </span>
          </div>
        </div>
        <span className="ml-4 flex-shrink-0 text-sm text-gray-500">
          {type.valueCount} {type.valueCount === 1 ? 'value' : 'values'}
        </span>
      </button>

      {isExpanded && (
        <div className="border-t border-gray-100 px-4 pb-4" data-testid="defined-values-section">
          <ExpandedTypeContent typeIdKey={type.idKey} isSystem={type.isSystem} />
        </div>
      )}
    </div>
  );
}

// ============================================================================
// DefinedTypesPage
// ============================================================================

export function DefinedTypesPage() {
  const [expandedIdKey, setExpandedIdKey] = useState<IdKey | null>(null);

  const { data: definedTypes = [], isLoading, error, refetch } = useDefinedTypes();

  const handleToggle = (idKey: IdKey) => {
    setExpandedIdKey((prev) => (prev === idKey ? null : idKey));
  };

  // Group by category, sorted alphabetically within each group
  const categories = Array.from(new Set(definedTypes.map((t) => t.category))).sort();
  const typesByCategory = categories.reduce<Record<string, DefinedTypeSummary[]>>(
    (acc, category) => {
      acc[category] = definedTypes
        .filter((t) => t.category === category)
        .sort((a, b) => a.order - b.order || a.name.localeCompare(b.name));
      return acc;
    },
    {}
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Defined Types</h1>
        <p className="mt-2 text-gray-600">
          Manage configurable lookup values used throughout the system
        </p>
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="bg-white rounded-lg border border-gray-200">
          <Loading text="Loading defined types..." />
        </div>
      )}

      {/* Error State */}
      {error && (
        <div className="bg-white rounded-lg border border-gray-200">
          <ErrorState
            title="Failed to load defined types"
            message={getErrorMessage(error).message}
            onRetry={() => refetch()}
          />
        </div>
      )}

      {/* Empty State */}
      {!isLoading && !error && definedTypes.length === 0 && (
        <div className="bg-white rounded-lg border border-gray-200">
          <EmptyState
            title="No defined types configured"
            description="Defined types power dropdown lists throughout the system (e.g., phone types, connection statuses). These are typically seeded on install."
          />
        </div>
      )}

      {/* Grouped accordion list */}
      {!isLoading && !error && categories.length > 0 && (
        <div className="space-y-8">
          {categories.map((category) => (
            <section key={category}>
              <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-3">
                {category}
              </h2>
              <div className="space-y-2">
                {typesByCategory[category].map((type) => (
                  <DefinedTypeRow
                    key={type.idKey}
                    type={type}
                    isExpanded={expandedIdKey === type.idKey}
                    onToggle={() => handleToggle(type.idKey)}
                  />
                ))}
              </div>
            </section>
          ))}
        </div>
      )}
    </div>
  );
}
