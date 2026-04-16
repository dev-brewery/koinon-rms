/**
 * Funds Admin Page
 * Admin page for managing giving funds
 */

import { useState } from 'react';
import { useAdminFunds, useCreateFund, useUpdateFund, useDeactivateFund } from '@/hooks/useGiving';
import type { FundAdminDto, CreateFundRequest, UpdateFundRequest } from '@/types/giving';

// ============================================================================
// Fund Form Modal
// ============================================================================

interface FundFormValues {
  name: string;
  publicName: string;
  description: string;
  glCode: string;
  isPublic: boolean;
  isTaxDeductible: boolean;
}

interface FundFormModalProps {
  onClose: () => void;
  fund?: FundAdminDto;
}

/**
 * Always rendered when mounted — parent controls visibility via conditional render.
 * This ensures useState initialises fresh for each open.
 */
function FundFormModal({ onClose, fund }: FundFormModalProps) {
  const isEditing = fund !== undefined;

  const [values, setValues] = useState<FundFormValues>({
    name: fund?.name ?? '',
    publicName: fund?.publicName ?? '',
    description: fund?.description ?? '',
    glCode: fund?.glCode ?? '',
    isPublic: fund?.isPublic ?? false,
    isTaxDeductible: fund?.isTaxDeductible ?? false,
  });

  const [nameError, setNameError] = useState('');

  const createMutation = useCreateFund();
  const updateMutation = useUpdateFund();

  const isPending = createMutation.isPending || updateMutation.isPending;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!values.name.trim()) {
      setNameError('Name is required');
      return;
    }
    setNameError('');

    const payload: CreateFundRequest | UpdateFundRequest = {
      name: values.name.trim(),
      publicName: values.publicName.trim() || undefined,
      description: values.description.trim() || undefined,
      glCode: values.glCode.trim() || undefined,
      isPublic: values.isPublic,
      isTaxDeductible: values.isTaxDeductible,
    };

    try {
      if (isEditing && fund) {
        await updateMutation.mutateAsync({ idKey: fund.idKey, request: payload });
      } else {
        await createMutation.mutateAsync(payload as CreateFundRequest);
      }
      onClose();
    } catch {
      // Error surfaced via mutation error state below
    }
  };

  const mutationError = createMutation.error ?? updateMutation.error;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-lg mx-4">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
          <h2 className="text-lg font-semibold text-gray-900">
            {isEditing ? 'Edit Fund' : 'Create Fund'}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 transition-colors"
            aria-label="Close"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <form data-testid="fund-form" onSubmit={handleSubmit} className="p-6 space-y-4">
          {mutationError && (
            <div className="bg-red-50 border border-red-200 rounded-lg p-3">
              <p className="text-sm text-red-800">
                {mutationError instanceof Error ? mutationError.message : 'An error occurred. Please try again.'}
              </p>
            </div>
          )}

          {/* Name */}
          <div>
            <label htmlFor="fund-name" className="block text-sm font-medium text-gray-700 mb-1">
              Name <span className="text-red-500">*</span>
            </label>
            <input
              id="fund-name"
              data-testid="fund-name-input"
              type="text"
              value={values.name}
              onChange={(e) => setValues((v) => ({ ...v, name: e.target.value }))}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent"
              placeholder="e.g. General Fund"
              disabled={isPending}
            />
            {nameError && <p className="mt-1 text-sm text-red-600">{nameError}</p>}
          </div>

          {/* Public Name */}
          <div>
            <label htmlFor="fund-public-name" className="block text-sm font-medium text-gray-700 mb-1">
              Public Name
            </label>
            <input
              id="fund-public-name"
              type="text"
              value={values.publicName}
              onChange={(e) => setValues((v) => ({ ...v, publicName: e.target.value }))}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent"
              placeholder="Shown publicly (defaults to Name if blank)"
              disabled={isPending}
            />
          </div>

          {/* Description */}
          <div>
            <label htmlFor="fund-description" className="block text-sm font-medium text-gray-700 mb-1">
              Description
            </label>
            <textarea
              id="fund-description"
              value={values.description}
              onChange={(e) => setValues((v) => ({ ...v, description: e.target.value }))}
              rows={3}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent resize-none"
              placeholder="Optional description"
              disabled={isPending}
            />
          </div>

          {/* GL Code */}
          <div>
            <label htmlFor="fund-gl-code" className="block text-sm font-medium text-gray-700 mb-1">
              GL Code
            </label>
            <input
              id="fund-gl-code"
              type="text"
              value={values.glCode}
              onChange={(e) => setValues((v) => ({ ...v, glCode: e.target.value }))}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent"
              placeholder="General ledger account code"
              disabled={isPending}
            />
          </div>

          {/* Checkboxes */}
          <div className="space-y-3">
            <div className="flex items-center gap-3">
              <input
                id="fund-is-public"
                type="checkbox"
                checked={values.isPublic}
                onChange={(e) => setValues((v) => ({ ...v, isPublic: e.target.checked }))}
                className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                disabled={isPending}
              />
              <label htmlFor="fund-is-public" className="text-sm text-gray-700">
                Public — visible in online giving
              </label>
            </div>

            <div className="flex items-center gap-3">
              <input
                id="fund-is-tax-deductible"
                type="checkbox"
                checked={values.isTaxDeductible}
                onChange={(e) => setValues((v) => ({ ...v, isTaxDeductible: e.target.checked }))}
                className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                disabled={isPending}
              />
              <label htmlFor="fund-is-tax-deductible" className="text-sm text-gray-700">
                Tax Deductible — included in contribution statements
              </label>
            </div>
          </div>

          {/* Actions */}
          <div className="flex items-center justify-end gap-3 pt-2 border-t border-gray-100">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-sm text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
              disabled={isPending}
            >
              Cancel
            </button>
            <button
              type="submit"
              data-testid="fund-submit-btn"
              className="px-4 py-2 text-sm text-white bg-primary-600 rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-60"
              disabled={isPending}
            >
              {isPending ? 'Saving...' : isEditing ? 'Save Changes' : 'Create Fund'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ============================================================================
// Fund List Item
// ============================================================================

interface FundItemProps {
  fund: FundAdminDto;
  onEdit: (fund: FundAdminDto) => void;
  onDeactivate: (fund: FundAdminDto) => void;
  isDeactivating: boolean;
}

function FundItem({ fund, onEdit, onDeactivate, isDeactivating }: FundItemProps) {
  return (
    <div
      data-testid="fund-item"
      className="bg-white border border-gray-200 rounded-lg px-5 py-4 flex items-start justify-between gap-4"
    >
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <span data-testid="fund-name" className="font-medium text-gray-900 truncate">
            {fund.name}
          </span>
          <span
            data-testid="fund-status-badge"
            className={
              fund.isActive
                ? 'inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800'
                : 'inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-600'
            }
          >
            {fund.isActive ? 'Active' : 'Inactive'}
          </span>
        </div>

        <div className="mt-1 flex items-center gap-4 flex-wrap">
          {fund.description && (
            <p className="text-sm text-gray-500 truncate max-w-xs">{fund.description}</p>
          )}
          {fund.glCode && (
            <span className="text-xs text-gray-400 font-mono">GL: {fund.glCode}</span>
          )}
          <div className="flex items-center gap-3 text-xs text-gray-400">
            {fund.isPublic && <span>Public</span>}
            {fund.isTaxDeductible && <span>Tax Deductible</span>}
          </div>
        </div>
      </div>

      <div className="flex items-center gap-2 shrink-0">
        <button
          data-testid="fund-edit-btn"
          type="button"
          onClick={() => onEdit(fund)}
          className="px-3 py-1.5 text-sm text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
        >
          Edit
        </button>
        {fund.isActive && (
          <button
            data-testid="fund-deactivate-btn"
            type="button"
            onClick={() => onDeactivate(fund)}
            disabled={isDeactivating}
            className="px-3 py-1.5 text-sm text-red-700 bg-white border border-red-200 rounded-lg hover:bg-red-50 transition-colors disabled:opacity-60"
          >
            Deactivate
          </button>
        )}
      </div>
    </div>
  );
}

// ============================================================================
// Funds Page
// ============================================================================

export function FundsPage() {
  const [isCreating, setIsCreating] = useState(false);
  const [editingFund, setEditingFund] = useState<FundAdminDto | null>(null);

  const { data: funds = [], isLoading, error } = useAdminFunds();
  const deactivateMutation = useDeactivateFund();

  const handleEdit = (fund: FundAdminDto) => {
    setEditingFund(fund);
  };

  const handleCloseModal = () => {
    setIsCreating(false);
    setEditingFund(null);
  };

  const handleDeactivate = async (fund: FundAdminDto) => {
    if (!window.confirm(`Are you sure you want to deactivate "${fund.name}"?`)) {
      return;
    }
    try {
      await deactivateMutation.mutateAsync(fund.idKey);
    } catch {
      // Error silently — the mutation error state handles display if needed
    }
  };

  const isModalOpen = isCreating || editingFund !== null;

  return (
    <div data-testid="funds-page" className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Funds</h1>
          <p className="mt-2 text-gray-600">Manage giving funds for contribution categorization</p>
        </div>
        <button
          data-testid="create-fund-btn"
          type="button"
          onClick={() => setIsCreating(true)}
          className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
        >
          Create Fund
        </button>
      </div>

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-12">
          <div className="inline-block w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
        </div>
      )}

      {/* Error */}
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
            <p className="text-sm font-medium text-red-800">Failed to load funds</p>
          </div>
        </div>
      )}

      {/* Fund List */}
      {!isLoading && !error && (
        <div data-testid="fund-list" className="space-y-3">
          {funds.length === 0 ? (
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
                  d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
              <p className="text-gray-500 mb-4">No funds found</p>
              <button
                type="button"
                onClick={() => setIsCreating(true)}
                className="inline-block px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
              >
                Create First Fund
              </button>
            </div>
          ) : (
            funds.map((fund) => (
              <FundItem
                key={fund.idKey}
                fund={fund}
                onEdit={handleEdit}
                onDeactivate={handleDeactivate}
                isDeactivating={deactivateMutation.isPending}
              />
            ))
          )}
        </div>
      )}

      {/* Create / Edit Modal — conditionally mounted so state resets on each open */}
      {isModalOpen && (
        <FundFormModal
          key={editingFund?.idKey ?? 'new'}
          onClose={handleCloseModal}
          fund={editingFund ?? undefined}
        />
      )}
    </div>
  );
}
