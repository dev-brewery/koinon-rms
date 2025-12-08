/**
 * Page History Component
 * Displays the history of all messages sent to a specific pager
 */

import { usePageHistory } from './hooks';
import { PagerMessageStatus } from './api';

export interface PageHistoryProps {
  pagerNumber: number | null;
}

/**
 * Get icon and color for message status
 */
function getStatusDisplay(status: PagerMessageStatus): {
  icon: JSX.Element;
  label: string;
  colorClass: string;
} {
  switch (status) {
    case PagerMessageStatus.Delivered:
      return {
        icon: (
          <svg
            className="w-5 h-5"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M5 13l4 4L19 7"
            />
          </svg>
        ),
        label: 'Delivered',
        colorClass: 'text-green-600 bg-green-50 border-green-200',
      };
    case PagerMessageStatus.Sent:
      return {
        icon: (
          <svg
            className="w-5 h-5"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8"
            />
          </svg>
        ),
        label: 'Sent',
        colorClass: 'text-blue-600 bg-blue-50 border-blue-200',
      };
    case PagerMessageStatus.Pending:
      return {
        icon: (
          <svg
            className="w-5 h-5 animate-spin"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
            />
            <path
              className="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            />
          </svg>
        ),
        label: 'Pending',
        colorClass: 'text-gray-600 bg-gray-50 border-gray-200',
      };
    case PagerMessageStatus.Failed:
      return {
        icon: (
          <svg
            className="w-5 h-5"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M6 18L18 6M6 6l12 12"
            />
          </svg>
        ),
        label: 'Failed',
        colorClass: 'text-red-600 bg-red-50 border-red-200',
      };
  }
}

export function PageHistory({ pagerNumber }: PageHistoryProps) {
  const { data: history, isLoading, error } = usePageHistory(pagerNumber);

  if (!pagerNumber) {
    return (
      <div className="text-center py-8 text-gray-500">
        <p className="text-lg">Select a pager to view message history</p>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="flex items-center gap-3 text-gray-600">
          <svg
            className="animate-spin h-6 w-6"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
            />
            <path
              className="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            />
          </svg>
          <span className="text-lg">Loading history...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
        <p className="text-red-800 text-center">
          Failed to load page history. Please try again.
        </p>
      </div>
    );
  }

  if (!history) {
    return (
      <div className="text-center py-8 text-gray-500">
        <p className="text-lg">No history found for this pager</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="border-b border-gray-200 pb-4">
        <div className="flex items-center gap-3 mb-2">
          <span className="inline-block px-3 py-1 bg-blue-600 text-white font-bold rounded text-lg">
            P-{history.pagerNumber}
          </span>
          <span className="text-lg font-semibold text-gray-900">{history.childName}</span>
        </div>
        {history.parentPhoneNumber && (
          <div className="text-sm text-gray-600 flex items-center gap-1">
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
                d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z"
              />
            </svg>
            {history.parentPhoneNumber}
          </div>
        )}
      </div>

      {/* Messages */}
      {history.messages.length === 0 ? (
        <div className="text-center py-8 text-gray-500">
          <p className="text-base">No messages sent yet</p>
        </div>
      ) : (
        <div className="space-y-3">
          <p className="text-sm font-semibold text-gray-700">
            {history.messages.length} message{history.messages.length === 1 ? '' : 's'}{' '}
            sent
          </p>
          {history.messages.map((message) => {
            const statusDisplay = getStatusDisplay(message.status);
            return (
              <div
                key={message.idKey}
                className="p-4 border-2 border-gray-200 rounded-lg bg-white"
              >
                {/* Status and Time */}
                <div className="flex items-center justify-between mb-3">
                  <span
                    className={`inline-flex items-center gap-2 px-3 py-1 rounded text-sm font-semibold border ${statusDisplay.colorClass}`}
                  >
                    {statusDisplay.icon}
                    {statusDisplay.label}
                  </span>
                  <span className="text-sm text-gray-500">
                    {new Date(message.sentDateTime).toLocaleTimeString([], {
                      hour: '2-digit',
                      minute: '2-digit',
                    })}
                  </span>
                </div>

                {/* Message Text */}
                <p className="text-base text-gray-800 mb-2">{message.messageText}</p>

                {/* Metadata */}
                <div className="flex items-center justify-between text-xs text-gray-500">
                  <span>Sent by {message.sentByPersonName}</span>
                  {message.deliveredDateTime && (
                    <span>
                      Delivered at{' '}
                      {new Date(message.deliveredDateTime).toLocaleTimeString([], {
                        hour: '2-digit',
                        minute: '2-digit',
                      })}
                    </span>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
