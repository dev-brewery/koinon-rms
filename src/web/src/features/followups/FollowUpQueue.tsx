/**
 * Follow-up queue page component
 * Displays a list of pending follow-ups with filtering and actions
 */

import { useMemo, useState } from 'react';
import { usePendingFollowUps } from './hooks';
import { FollowUpCard } from './FollowUpCard';
import { Button } from '@/components/ui/Button';
import { FollowUpStatus } from './api';
import type { IdKey } from '@/services/api/types';

interface FollowUpQueueProps {
  assignedToIdKey?: IdKey;
}

export function FollowUpQueue({ assignedToIdKey }: FollowUpQueueProps) {
  const { data: followUps, isLoading, error, refetch } = usePendingFollowUps(assignedToIdKey);
  const [filterStatus, setFilterStatus] = useState<FollowUpStatus | 'all'>('all');

  // Filter follow-ups by status
  const filteredFollowUps = useMemo(() => {
    if (!followUps) return [];
    if (filterStatus === 'all') return followUps;
    return followUps.filter((f) => f.status === filterStatus);
  }, [followUps, filterStatus]);

  // Loading state
  if (isLoading) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="flex items-center justify-center py-12">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4" />
            <p className="text-gray-600">Loading follow-ups...</p>
          </div>
        </div>
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="bg-red-50 border border-red-200 rounded-lg p-6">
          <h3 className="text-lg font-semibold text-red-900 mb-2">
            Error Loading Follow-ups
          </h3>
          <p className="text-red-700 mb-4">
            {error instanceof Error ? error.message : 'An unknown error occurred'}
          </p>
          <Button onClick={() => refetch()}>Try Again</Button>
        </div>
      </div>
    );
  }

  // Empty state
  if (!followUps || followUps.length === 0) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="text-center py-12">
          <svg
            className="mx-auto h-12 w-12 text-gray-400"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
            />
          </svg>
          <h3 className="mt-2 text-sm font-semibold text-gray-900">No follow-ups</h3>
          <p className="mt-1 text-sm text-gray-500">
            There are no pending follow-ups at this time.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-gray-900">Follow-up Queue</h1>
        <p className="mt-2 text-sm text-gray-600">
          Manage pending follow-ups and track connection status
        </p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-5 gap-4 mb-6">
        <button
          onClick={() => setFilterStatus('all')}
          className={`p-4 rounded-lg border-2 transition-colors ${
            filterStatus === 'all'
              ? 'border-blue-500 bg-blue-50'
              : 'border-gray-200 bg-white hover:border-gray-300'
          }`}
        >
          <div className="text-2xl font-bold text-gray-900">{followUps.length}</div>
          <div className="text-sm text-gray-600">Total</div>
        </button>

        <button
          onClick={() => setFilterStatus(FollowUpStatus.Pending)}
          className={`p-4 rounded-lg border-2 transition-colors ${
            filterStatus === FollowUpStatus.Pending
              ? 'border-yellow-500 bg-yellow-50'
              : 'border-gray-200 bg-white hover:border-gray-300'
          }`}
        >
          <div className="text-2xl font-bold text-yellow-800">
            {followUps.filter((f) => f.status === FollowUpStatus.Pending).length}
          </div>
          <div className="text-sm text-gray-600">Pending</div>
        </button>

        <button
          onClick={() => setFilterStatus(FollowUpStatus.Contacted)}
          className={`p-4 rounded-lg border-2 transition-colors ${
            filterStatus === FollowUpStatus.Contacted
              ? 'border-blue-500 bg-blue-50'
              : 'border-gray-200 bg-white hover:border-gray-300'
          }`}
        >
          <div className="text-2xl font-bold text-blue-800">
            {followUps.filter((f) => f.status === FollowUpStatus.Contacted).length}
          </div>
          <div className="text-sm text-gray-600">Contacted</div>
        </button>

        <button
          onClick={() => setFilterStatus(FollowUpStatus.Connected)}
          className={`p-4 rounded-lg border-2 transition-colors ${
            filterStatus === FollowUpStatus.Connected
              ? 'border-green-500 bg-green-50'
              : 'border-gray-200 bg-white hover:border-gray-300'
          }`}
        >
          <div className="text-2xl font-bold text-green-800">
            {followUps.filter((f) => f.status === FollowUpStatus.Connected).length}
          </div>
          <div className="text-sm text-gray-600">Connected</div>
        </button>

        <button
          onClick={() => setFilterStatus(FollowUpStatus.NoResponse)}
          className={`p-4 rounded-lg border-2 transition-colors ${
            filterStatus === FollowUpStatus.NoResponse
              ? 'border-gray-500 bg-gray-50'
              : 'border-gray-200 bg-white hover:border-gray-300'
          }`}
        >
          <div className="text-2xl font-bold text-gray-800">
            {followUps.filter((f) => f.status === FollowUpStatus.NoResponse).length}
          </div>
          <div className="text-sm text-gray-600">No Response</div>
        </button>
      </div>

      {/* Follow-up List */}
      <div className="space-y-4">
        {filteredFollowUps.length === 0 ? (
          <div className="text-center py-8 bg-gray-50 rounded-lg">
            <p className="text-gray-600">
              No follow-ups match the selected filter.
            </p>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setFilterStatus('all')}
              className="mt-2"
            >
              Clear Filter
            </Button>
          </div>
        ) : (
          filteredFollowUps.map((followUp) => (
            <FollowUpCard key={followUp.idKey} followUp={followUp} />
          ))
        )}
      </div>
    </div>
  );
}
