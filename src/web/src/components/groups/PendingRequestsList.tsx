/**
 * Pending Requests List Component
 * Displays list of pending membership requests for a group
 */

import { useState } from 'react';
import { usePendingRequests, useProcessRequest } from '@/hooks/useMembershipRequests';
import { RequestReviewModal } from './RequestReviewModal';
import type { GroupMemberRequestDto } from '@/services/api/types';
import { formatDate } from '@/lib/utils';

interface PendingRequestsListProps {
  groupIdKey: string;
}

export function PendingRequestsList({ groupIdKey }: PendingRequestsListProps) {
  const [selectedRequest, setSelectedRequest] = useState<GroupMemberRequestDto | null>(null);

  const { data: requests = [], isLoading } = usePendingRequests(groupIdKey);
  const processRequestMutation = useProcessRequest(groupIdKey);

  const handleApprove = async (requestIdKey: string, note?: string) => {
    await processRequestMutation.mutateAsync({
      requestIdKey,
      request: { status: 'Approved', note },
    });
  };

  const handleDeny = async (requestIdKey: string, note?: string) => {
    await processRequestMutation.mutateAsync({
      requestIdKey,
      request: { status: 'Denied', note },
    });
  };

  if (isLoading) {
    return (
      <div className="py-4 text-center">
        <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600 mx-auto"></div>
      </div>
    );
  }

  if (requests.length === 0) {
    return (
      <div className="py-4 text-center text-sm text-gray-500">
        No pending requests
      </div>
    );
  }

  return (
    <>
      <div className="space-y-2">
        {requests.map((request) => (
          <button
            key={request.idKey}
            onClick={() => setSelectedRequest(request)}
            className="w-full text-left p-3 bg-white border border-gray-200 rounded-lg hover:bg-gray-50 hover:border-gray-300 transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <p className="font-medium text-gray-900">{request.requester.fullName}</p>
                <p className="text-sm text-gray-600 mt-1">
                  Requested {formatDate(request.createdDateTime)}
                </p>
                {request.requestNote && (
                  <p className="text-sm text-gray-700 mt-2 line-clamp-2">
                    {request.requestNote}
                  </p>
                )}
              </div>
              <svg
                className="w-5 h-5 text-gray-400 ml-2 flex-shrink-0"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 5l7 7-7 7"
                />
              </svg>
            </div>
          </button>
        ))}
      </div>

      <RequestReviewModal
        isOpen={!!selectedRequest}
        onClose={() => setSelectedRequest(null)}
        request={selectedRequest}
        onApprove={handleApprove}
        onDeny={handleDeny}
      />
    </>
  );
}
