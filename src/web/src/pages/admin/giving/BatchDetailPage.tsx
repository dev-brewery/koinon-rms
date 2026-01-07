/**
 * Batch Detail Page
 * View and manage a single financial batch with contributions
 */

import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  useBatch,
  useBatchSummary,
  useBatchContributions,
  useActiveFunds,
  useOpenBatch,
  useCloseBatch,
  useAddContribution,
  useUpdateContribution,
  useDeleteContribution,
} from '@/hooks/useGiving';
import { getDefinedTypeValues } from '@/services/api/reference';
import { BatchSummaryCard, ContributionTable, ContributionForm } from '@/components/giving';
import { Loading, ErrorState } from '@/components/ui';
import { useToast } from '@/contexts/ToastContext';
import { useQuery } from '@tanstack/react-query';
import type { ContributionDto, AddContributionRequest, UpdateContributionRequest } from '@/types/giving';

const TRANSACTION_TYPE_GUID = '2AACBE45-9C69-4D47-9F30-DDCE7D39E1B4';

export function BatchDetailPage() {
  const { idKey } = useParams<{ idKey: string }>();
  const toast = useToast();

  // State
  const [showForm, setShowForm] = useState(false);
  const [editingContribution, setEditingContribution] = useState<ContributionDto | null>(null);

  // Data queries
  const { data: batch, isLoading: batchLoading, error: batchError } = useBatch(idKey);
  const { data: summary, isLoading: summaryLoading } = useBatchSummary(idKey);
  const { data: contributionsData, isLoading: contributionsLoading } = useBatchContributions(idKey);
  const { data: fundsData } = useActiveFunds();
  const { data: transactionTypesData } = useQuery({
    queryKey: ['definedTypeValues', TRANSACTION_TYPE_GUID],
    queryFn: () => getDefinedTypeValues(TRANSACTION_TYPE_GUID),
    enabled: showForm,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  // Mutations
  const openBatch = useOpenBatch();
  const closeBatch = useCloseBatch();
  const addContribution = useAddContribution();
  const updateContribution = useUpdateContribution();
  const deleteContribution = useDeleteContribution();

  // Extract data from API responses
  const contributions = contributionsData || [];
  const funds = fundsData || [];
  const transactionTypes = transactionTypesData || [];

  // Event handlers
  const handleOpenBatch = async () => {
    if (!idKey) return;

    try {
      await openBatch.mutateAsync(idKey);
      toast.success('Batch Opened', 'The batch has been reopened for editing.');
    } catch {
      toast.error('Open Failed', 'Failed to open the batch. Please try again.');
    }
  };

  const handleCloseBatch = async () => {
    if (!idKey) return;

    try {
      await closeBatch.mutateAsync(idKey);
      toast.success('Batch Closed', 'The batch has been closed successfully.');
    } catch {
      toast.error('Close Failed', 'Failed to close the batch. Please try again.');
    }
  };

  const handleAddContribution = () => {
    setEditingContribution(null);
    setShowForm(true);
  };

  const handleEditContribution = (contribution: ContributionDto) => {
    setEditingContribution(contribution);
    setShowForm(true);
  };

  const handleDeleteContribution = async (contributionIdKey: string) => {
    if (!idKey) return;

    try {
      await deleteContribution.mutateAsync({ idKey: contributionIdKey, batchIdKey: idKey });
      toast.success('Contribution Deleted', 'The contribution has been removed from this batch.');
    } catch {
      toast.error('Delete Failed', 'Failed to delete the contribution. Please try again.');
    }
  };

  const handleSubmitContribution = async (data: AddContributionRequest | UpdateContributionRequest) => {
    if (!idKey) return;

    try {
      if (editingContribution) {
        // Update existing contribution
        await updateContribution.mutateAsync({
          idKey: editingContribution.idKey,
          data: data as UpdateContributionRequest,
        });
        toast.success('Contribution Updated', 'The contribution has been updated successfully.');
      } else {
        // Add new contribution
        await addContribution.mutateAsync({
          batchIdKey: idKey,
          request: data as AddContributionRequest,
        });
        toast.success('Contribution Added', 'The contribution has been added to this batch.');
      }
      setShowForm(false);
      setEditingContribution(null);
    } catch {
      toast.error(
        editingContribution ? 'Update Failed' : 'Add Failed',
        `Failed to ${editingContribution ? 'update' : 'add'} the contribution. Please try again.`
      );
    }
  };

  // Loading state
  if (batchLoading || summaryLoading || contributionsLoading) {
    return <Loading />;
  }

  // Error state
  if (batchError || !batch || !summary) {
    return (
      <div className="text-center py-12">
        <ErrorState
          title="Failed to load batch"
          message="The batch could not be loaded. Please try again later."
        />
        <Link
          to="/admin/giving"
          className="inline-block mt-4 px-4 py-2 text-primary-600 hover:text-primary-700"
        >
          Back to Batches
        </Link>
      </div>
    );
  }

  const isEditable = batch.status === 'Open';

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link
            to="/admin/giving"
            className="p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100 transition-colors"
            aria-label="Back to batches"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M15 19l-7-7 7-7"
              />
            </svg>
          </Link>
          <div>
            <h1 className="text-3xl font-bold text-gray-900">{batch.name}</h1>
            <p className="mt-1 text-gray-600">
              {new Date(batch.batchDate).toLocaleDateString()}
            </p>
          </div>
        </div>

        {isEditable && (
          <button
            onClick={handleAddContribution}
            className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
          >
            Add Contribution
          </button>
        )}
      </div>

      {/* Layout Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Content */}
        <div className="lg:col-span-2 space-y-6">
          {/* Contributions Table */}
          <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
            <div className="px-6 py-4 border-b border-gray-200">
              <h2 className="text-lg font-semibold text-gray-900">
                Contributions ({contributions.length})
              </h2>
            </div>
            <ContributionTable
              contributions={contributions}
              batchStatus={batch.status}
              onEdit={handleEditContribution}
              onDelete={handleDeleteContribution}
              isDeleting={deleteContribution.isPending}
            />
          </div>

          {/* Batch Notes */}
          {batch.note && (
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-2">Notes</h2>
              <p className="text-sm text-gray-700 whitespace-pre-wrap">{batch.note}</p>
            </div>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Batch Summary Card */}
          <BatchSummaryCard
            batch={batch}
            summary={summary}
            onOpen={handleOpenBatch}
            onClose={handleCloseBatch}
            isOpenPending={openBatch.isPending}
            isClosePending={closeBatch.isPending}
          />

          {/* Metadata */}
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Metadata</h2>
            <dl className="space-y-3 text-sm">
              <div>
                <dt className="text-gray-500">Created</dt>
                <dd className="text-gray-900">
                  {new Date(batch.createdDateTime).toLocaleDateString()}
                </dd>
              </div>
              {batch.modifiedDateTime && (
                <div>
                  <dt className="text-gray-500">Last Modified</dt>
                  <dd className="text-gray-900">
                    {new Date(batch.modifiedDateTime).toLocaleDateString()}
                  </dd>
                </div>
              )}
            </dl>
          </div>
        </div>
      </div>

      {/* Contribution Form Modal */}
      {showForm && (
        <ContributionForm
          isOpen={showForm}
          onClose={() => {
            setShowForm(false);
            setEditingContribution(null);
          }}
          onSubmit={handleSubmitContribution}
          funds={funds}
          transactionTypes={transactionTypes}
          contribution={editingContribution || undefined}
          isSubmitting={addContribution.isPending || updateContribution.isPending}
        />
      )}
    </div>
  );
}
