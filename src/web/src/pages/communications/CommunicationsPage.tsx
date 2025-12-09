/**
 * CommunicationsPage
 * List and manage communications with filtering
 */

import { useState } from 'react';
import { useCommunications } from '@/hooks/useCommunications';
import { useGroups } from '@/hooks/useGroups';
import { CommunicationComposer } from '@/components/communication/CommunicationComposer';
import type { CommunicationSummaryDto } from '@/services/api/communications';

const STATUS_OPTIONS = [
  { value: '', label: 'All' },
  { value: 'Draft', label: 'Draft' },
  { value: 'Pending', label: 'Pending' },
  { value: 'Sent', label: 'Sent' },
  { value: 'Failed', label: 'Failed' },
];

function CommunicationStatusBadge({ status }: { status: string }) {
  const colors: Record<string, string> = {
    Draft: 'bg-gray-100 text-gray-800',
    Pending: 'bg-blue-100 text-blue-800',
    Sent: 'bg-green-100 text-green-800',
    Failed: 'bg-red-100 text-red-800',
  };

  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
        colors[status] || 'bg-gray-100 text-gray-800'
      }`}
    >
      {status}
    </span>
  );
}

function CommunicationTypeBadge({ type }: { type: string }) {
  return (
    <span className="inline-flex items-center gap-1 text-sm text-gray-600">
      {type === 'Email' ? (
        <>
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
            />
          </svg>
          Email
        </>
      ) : (
        <>
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 18h.01M8 21h8a2 2 0 002-2V5a2 2 0 00-2-2H8a2 2 0 00-2 2v14a2 2 0 002 2z"
            />
          </svg>
          SMS
        </>
      )}
    </span>
  );
}

function CommunicationRow({ communication }: { communication: CommunicationSummaryDto }) {
  const date = new Date(communication.createdDateTime);
  const sentDate = communication.sentDateTime ? new Date(communication.sentDateTime) : null;

  return (
    <div className="bg-white border border-gray-200 rounded-lg p-4 hover:border-primary-300 transition-colors">
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-3 mb-2">
            <CommunicationStatusBadge status={communication.status} />
            <CommunicationTypeBadge type={communication.communicationType} />
          </div>

          {communication.subject && (
            <h3 className="text-lg font-semibold text-gray-900 mb-1 truncate">
              {communication.subject}
            </h3>
          )}

          <div className="flex items-center gap-4 text-sm text-gray-600">
            <span className="flex items-center gap-1">
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
                />
              </svg>
              {communication.recipientCount}{' '}
              {communication.recipientCount === 1 ? 'recipient' : 'recipients'}
            </span>

            {communication.status === 'Sent' && (
              <>
                <span className="text-green-600">
                  {communication.deliveredCount} delivered
                </span>
                {communication.failedCount > 0 && (
                  <span className="text-red-600">{communication.failedCount} failed</span>
                )}
              </>
            )}
          </div>
        </div>

        <div className="text-right text-sm text-gray-600">
          <div>{sentDate ? sentDate.toLocaleDateString() : date.toLocaleDateString()}</div>
          <div className="text-xs text-gray-500">
            {sentDate ? sentDate.toLocaleTimeString() : date.toLocaleTimeString()}
          </div>
        </div>
      </div>
    </div>
  );
}

export function CommunicationsPage() {
  const [statusFilter, setStatusFilter] = useState('');
  const [isComposerOpen, setIsComposerOpen] = useState(false);

  const { data: communicationsData, isLoading, error } = useCommunications({
    status: statusFilter || undefined,
    pageSize: 50,
  });

  const { data: groupsData, isLoading: isLoadingGroups } = useGroups({
    includeInactive: false,
    pageSize: 100,
  });

  const communications = communicationsData?.data || [];
  const groups = groupsData?.data || [];

  const handleSend = () => {
    setIsComposerOpen(false);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Communications</h1>
          <p className="mt-2 text-gray-600">Send and manage email and SMS communications</p>
        </div>
        <button
          onClick={() => setIsComposerOpen(true)}
          disabled={isLoadingGroups}
          className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50"
        >
          New Communication
        </button>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <div className="flex items-center gap-4">
          <label htmlFor="status-filter" className="text-sm font-medium text-gray-700">
            Status:
          </label>
          <select
            id="status-filter"
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          >
            {STATUS_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
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
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            <p className="text-sm font-medium text-red-800">Failed to load communications</p>
          </div>
        </div>
      )}

      {/* Communications List */}
      {!isLoading && !error && (
        <div className="space-y-4">
          {communications.length === 0 ? (
            <div className="bg-white rounded-lg border border-gray-200 p-12 text-center">
              <svg
                className="w-12 h-12 text-gray-400 mx-auto mb-4"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
                />
              </svg>
              <p className="text-gray-500 mb-4">No communications found</p>
              <button
                onClick={() => setIsComposerOpen(true)}
                className="inline-block px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
              >
                Send First Communication
              </button>
            </div>
          ) : (
            <div className="space-y-3">
              {communications.map((communication) => (
                <CommunicationRow key={communication.idKey} communication={communication} />
              ))}
            </div>
          )}
        </div>
      )}

      {/* Composer Modal */}
      {isComposerOpen && (
        <CommunicationComposer
          groups={groups}
          onSend={handleSend}
          onClose={() => setIsComposerOpen(false)}
        />
      )}
    </div>
  );
}
