/**
 * Recent Communications Component
 * Displays list of recent communications with type and status indicators
 */

import { Link } from 'react-router-dom';
import type { CommunicationSummary } from '@/types';

export interface RecentCommunicationsProps {
  communications: CommunicationSummary[];
}

export function RecentCommunications({ communications }: RecentCommunicationsProps) {
  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  const getTypeColor = (type: CommunicationSummary['type']): string => {
    switch (type) {
      case 'Email':
        return 'text-blue-700 bg-blue-50 border-blue-200';
      case 'SMS':
        return 'text-green-700 bg-green-50 border-green-200';
      case 'Push':
        return 'text-purple-700 bg-purple-50 border-purple-200';
      default:
        return 'text-gray-700 bg-gray-50 border-gray-200';
    }
  };

  const getStatusColor = (status: CommunicationSummary['status']): string => {
    switch (status) {
      case 'Draft':
        return 'text-gray-700 bg-gray-50 border-gray-200';
      case 'Pending':
        return 'text-yellow-700 bg-yellow-50 border-yellow-200';
      case 'Sent':
        return 'text-green-700 bg-green-50 border-green-200';
      case 'Failed':
        return 'text-red-700 bg-red-50 border-red-200';
      default:
        return 'text-gray-700 bg-gray-50 border-gray-200';
    }
  };

  if (communications.length === 0) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Recent Communications</h2>
        <div className="text-center py-12">
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
              d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
            />
          </svg>
          <p className="text-gray-500">No recent communications</p>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <h2 className="text-lg font-semibold text-gray-900 mb-4">Recent Communications</h2>
      <div className="space-y-3">
        {communications.slice(0, 5).map((communication) => (
          <Link
            key={communication.idKey}
            to={`/admin/communications/${communication.idKey}`}
            className="flex items-center justify-between p-4 rounded-lg border border-gray-200 hover:border-primary-300 hover:bg-primary-50 transition-all group"
          >
            <div className="flex-1 min-w-0">
              <h3 className="text-sm font-semibold text-gray-900 group-hover:text-primary-700">
                {communication.subject}
              </h3>
              <p className="mt-1 text-sm text-gray-500">
                {formatDate(communication.createdDateTime)}
              </p>
            </div>
            <div className="flex items-center gap-2">
              <div className={`px-3 py-1 text-xs font-medium rounded-full border ${getTypeColor(communication.type)}`}>
                {communication.type}
              </div>
              <div className={`px-3 py-1 text-xs font-medium rounded-full border ${getStatusColor(communication.status)}`}>
                {communication.status}
              </div>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
