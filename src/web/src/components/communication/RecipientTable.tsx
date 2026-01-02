/**
 * Recipient Table Component
 * Displays communication recipients with delivery status and details
 */

import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { cn } from '@/lib/utils';
import { formatDateTime } from '@/lib/utils';
import { EmptyState } from '@/components/ui';
import { RecipientStatusBadge } from './RecipientStatusBadge';
import type { CommunicationRecipientDto } from '@/types/communication';

// ============================================================================
// Types
// ============================================================================

export interface RecipientTableProps {
  recipients: CommunicationRecipientDto[];
  communicationType: 'Email' | 'Sms';
}

// ============================================================================
// Component
// ============================================================================

export function RecipientTable({
  recipients,
  communicationType,
}: RecipientTableProps) {
  const [expandedRowId, setExpandedRowId] = useState<string | null>(null);

  const handleRowClick = (idKey: string) => {
    // Only toggle if the recipient has an error message
    const recipient = recipients.find((r) => r.idKey === idKey);
    if (recipient?.errorMessage) {
      setExpandedRowId((current) => (current === idKey ? null : idKey));
    }
  };

  const formatDateTimeOrEmpty = (dateTime?: string): string => {
    if (!dateTime) {
      return '-';
    }
    return formatDateTime(dateTime);
  };

  const getAddressLabel = (): string => {
    return communicationType === 'Email' ? 'Email' : 'Phone';
  };

  // Empty state
  if (recipients.length === 0) {
    return (
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
              d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
            />
          </svg>
        }
        title="No recipients"
        description="Add recipients to this communication to get started"
      />
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="min-w-full divide-y divide-gray-200">
        <thead className="bg-gray-50">
          <tr>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Recipient
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              {getAddressLabel()}
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Status
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Delivered
            </th>
            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Opened
            </th>
          </tr>
        </thead>
        <tbody className="bg-white divide-y divide-gray-200">
          {recipients.map((recipient) => {
            const isExpanded = expandedRowId === recipient.idKey;
            const hasError = !!recipient.errorMessage;
            const isClickable = hasError;

            return (
              <React.Fragment key={recipient.idKey}>
                {/* Main Row */}
                <tr
                  onClick={() => handleRowClick(recipient.idKey)}
                  className={cn(
                    'transition-colors',
                    isClickable && 'cursor-pointer',
                    isExpanded ? 'bg-red-50' : isClickable ? 'hover:bg-gray-50' : ''
                  )}
                >
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center gap-2">
                      {hasError && (
                        <svg
                          className={cn(
                            'w-4 h-4 text-gray-400 transition-transform',
                            isExpanded && 'rotate-90'
                          )}
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M9 5l7 7-7 7"
                          />
                        </svg>
                      )}
                      <Link
                        to={`/admin/people/${recipient.personIdKey}`}
                        className="text-sm font-medium text-primary-600 hover:text-primary-700 transition-colors"
                        onClick={(e) => e.stopPropagation()}
                      >
                        {recipient.recipientName || 'Unknown'}
                      </Link>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {recipient.address}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <RecipientStatusBadge status={recipient.status} />
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {formatDateTimeOrEmpty(recipient.deliveredDateTime)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {formatDateTimeOrEmpty(recipient.openedDateTime)}
                  </td>
                </tr>

                {/* Expanded Error Row */}
                {isExpanded && hasError && (
                  <tr className="bg-red-50">
                    <td colSpan={5} className="px-6 py-4">
                      <div className="ml-10">
                        <div className="flex items-start gap-3">
                          <svg
                            className="w-5 h-5 text-red-600 mt-0.5 flex-shrink-0"
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
                          <div className="flex-1">
                            <h4 className="text-xs font-semibold text-red-900 uppercase tracking-wider mb-1">
                              Delivery Error
                            </h4>
                            <p className="text-sm text-red-800">
                              {recipient.errorMessage}
                            </p>
                          </div>
                        </div>
                      </div>
                    </td>
                  </tr>
                )}
              </React.Fragment>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
