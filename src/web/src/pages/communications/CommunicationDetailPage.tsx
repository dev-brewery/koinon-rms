/**
 * CommunicationDetailPage
 * View details of a sent communication including recipients and statistics
 */

import { useParams, Link } from 'react-router-dom';
import { useCommunication } from '@/hooks/useCommunications';
import { CommunicationStatisticsCard, RecipientTable } from '@/components/communication';

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
    <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
      {type === 'Email' ? (
        <>
          <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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
          <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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

export function CommunicationDetailPage() {
  const { idKey } = useParams<{ idKey: string }>();
  const { data: communication, isLoading, error } = useCommunication(idKey);

  // Loading state
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="inline-block w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
      </div>
    );
  }

  // Error state
  if (error || !communication) {
    return (
      <div className="text-center py-12">
        <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-red-100 mb-4">
          <svg
            className="w-8 h-8 text-red-600"
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
        </div>
        <h2 className="text-2xl font-bold text-gray-900 mb-2">Communication Not Found</h2>
        <p className="text-gray-600 mb-4">
          The communication you're looking for could not be loaded. It may have been deleted or you
          may not have access.
        </p>
        <Link
          to="/admin/communications"
          className="inline-block px-4 py-2 text-primary-600 hover:text-primary-700 font-medium"
        >
          Back to Communications
        </Link>
      </div>
    );
  }

  const sentDate = communication.sentDateTime
    ? new Date(communication.sentDateTime)
    : new Date(communication.createdDateTime);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-start gap-4">
        <Link
          to="/admin/communications"
          className="p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100 transition-colors"
          aria-label="Back to communications"
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
        <div className="flex-1">
          <h1 className="text-3xl font-bold text-gray-900">
            {communication.subject || 'SMS Communication'}
          </h1>
          <div className="flex items-center gap-3 mt-2">
            <CommunicationTypeBadge type={communication.communicationType} />
            <CommunicationStatusBadge status={communication.status} />
            <span className="text-sm text-gray-600">
              {sentDate.toLocaleDateString()} at {sentDate.toLocaleTimeString()}
            </span>
          </div>
        </div>
      </div>

      {/* Layout Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Content */}
        <div className="lg:col-span-2 space-y-6">
          {/* Recipients Table */}
          <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
            <div className="px-6 py-4 border-b border-gray-200">
              <h2 className="text-lg font-semibold text-gray-900">
                Recipients ({communication.recipientCount})
              </h2>
            </div>
            <RecipientTable 
              recipients={communication.recipients} 
              communicationType={communication.communicationType as 'Email' | 'Sms'}
            />
          </div>

          {/* Message Body */}
          {communication.body && (
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Message</h2>
              <div className="prose max-w-none">
                <div
                  className="text-sm text-gray-700 whitespace-pre-wrap"
                  dangerouslySetInnerHTML={{ __html: communication.body }}
                />
              </div>
            </div>
          )}

          {/* Notes */}
          {communication.note && (
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-2">Notes</h2>
              <p className="text-sm text-gray-700 whitespace-pre-wrap">{communication.note}</p>
            </div>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Statistics Card */}
          <CommunicationStatisticsCard 
            recipientCount={communication.recipientCount}
            deliveredCount={communication.deliveredCount}
            failedCount={communication.failedCount}
            openedCount={communication.openedCount}
          />

          {/* Metadata */}
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Metadata</h2>
            <dl className="space-y-3 text-sm">
              <div>
                <dt className="text-gray-500">Created</dt>
                <dd className="text-gray-900">
                  {new Date(communication.createdDateTime).toLocaleDateString()}
                </dd>
              </div>

              {communication.fromName && (
                <div>
                  <dt className="text-gray-500">From Name</dt>
                  <dd className="text-gray-900">{communication.fromName}</dd>
                </div>
              )}

              {communication.fromEmail && (
                <div>
                  <dt className="text-gray-500">From Email</dt>
                  <dd className="text-gray-900">{communication.fromEmail}</dd>
                </div>
              )}

              {communication.replyToEmail && (
                <div>
                  <dt className="text-gray-500">Reply To</dt>
                  <dd className="text-gray-900">{communication.replyToEmail}</dd>
                </div>
              )}

              {communication.modifiedDateTime && (
                <div>
                  <dt className="text-gray-500">Last Modified</dt>
                  <dd className="text-gray-900">
                    {new Date(communication.modifiedDateTime).toLocaleDateString()}
                  </dd>
                </div>
              )}
            </dl>
          </div>
        </div>
      </div>
    </div>
  );
}
