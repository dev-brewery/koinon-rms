/**
 * Individual notification item component
 * Displays a single notification with icon, title, message, and actions
 */

import { cn } from '@/lib/utils';
import { formatTimeAgo } from '@/utils/timeAgo';
import type { NotificationDto, NotificationType } from '@/types/notification';

export interface NotificationItemProps {
  notification: NotificationDto;
  onMarkAsRead: (idKey: string) => void;
  onDelete: (idKey: string) => void;
  onClick?: (notification: NotificationDto) => void;
}

/**
 * Get icon component based on notification type
 */
function getNotificationIcon(type: NotificationType): JSX.Element {
  const iconClass = 'w-5 h-5';

  switch (type) {
    case 0: // CheckinAlert
      return (
        <svg className={iconClass} fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
        </svg>
      );
    case 1: // CommunicationStatus
      return (
        <svg className={iconClass} fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
        </svg>
      );
    case 2: // SystemAlert
      return (
        <svg className={iconClass} fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      );
    case 3: // MembershipRequest
      return (
        <svg className={iconClass} fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
        </svg>
      );
    case 4: // FollowUp
      return (
        <svg className={iconClass} fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
        </svg>
      );
    default:
      return (
        <svg className={iconClass} fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      );
  }
}

/**
 * Get icon background color based on notification type
 */
function getIconColor(type: NotificationType, isRead: boolean): string {
  const baseColors = {
    0: 'bg-blue-100 text-blue-600', // CheckinAlert
    1: 'bg-purple-100 text-purple-600', // CommunicationStatus
    2: 'bg-yellow-100 text-yellow-600', // SystemAlert
    3: 'bg-green-100 text-green-600', // MembershipRequest
    4: 'bg-orange-100 text-orange-600', // FollowUp
  };

  const readColors = {
    0: 'bg-blue-50 text-blue-400',
    1: 'bg-purple-50 text-purple-400',
    2: 'bg-yellow-50 text-yellow-400',
    3: 'bg-green-50 text-green-400',
    4: 'bg-orange-50 text-orange-400',
  };

  const colors = isRead ? readColors : baseColors;
  return colors[type as keyof typeof colors] || 'bg-gray-100 text-gray-600';
}

/**
 * Get border color for unread notifications
 */
function getBorderColor(type: NotificationType): string {
  const colors = {
    0: 'border-l-blue-500', // CheckinAlert
    1: 'border-l-purple-500', // CommunicationStatus
    2: 'border-l-yellow-500', // SystemAlert
    3: 'border-l-green-500', // MembershipRequest
    4: 'border-l-orange-500', // FollowUp
  };

  return colors[type as keyof typeof colors] || 'border-l-gray-500';
}

export function NotificationItem({
  notification,
  onMarkAsRead,
  onDelete,
  onClick,
}: NotificationItemProps) {
  const handleClick = () => {
    if (!notification.isRead) {
      onMarkAsRead(notification.idKey);
    }
    if (onClick) {
      onClick(notification);
    }
  };

  const handleDelete = (e: React.MouseEvent) => {
    e.stopPropagation();
    onDelete(notification.idKey);
  };

  return (
    <div
      onClick={handleClick}
      className={cn(
        'group relative bg-white border border-gray-200 rounded-lg p-4 transition-all',
        !notification.isRead && 'border-l-4',
        !notification.isRead && getBorderColor(notification.notificationType),
        notification.isRead && 'opacity-70',
        (onClick || notification.actionUrl) && 'cursor-pointer hover:shadow-md'
      )}
    >
      <div className="flex gap-3">
        {/* Icon */}
        <div
          className={cn(
            'flex-shrink-0 w-10 h-10 rounded-full flex items-center justify-center',
            getIconColor(notification.notificationType, notification.isRead)
          )}
        >
          {getNotificationIcon(notification.notificationType)}
        </div>

        {/* Content */}
        <div className="flex-1 min-w-0">
          <div className="flex items-start justify-between gap-2">
            <h4
              className={cn(
                'text-sm font-medium text-gray-900',
                !notification.isRead && 'font-semibold'
              )}
            >
              {notification.title}
            </h4>

            {/* Delete button */}
            <button
              onClick={handleDelete}
              aria-label="Delete notification"
              className="flex-shrink-0 p-1 text-gray-400 hover:text-red-600 rounded opacity-0 group-hover:opacity-100 transition-opacity"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M6 18L18 6M6 6l12 12"
                />
              </svg>
            </button>
          </div>

          <p className="mt-1 text-sm text-gray-600 line-clamp-2">{notification.message}</p>

          <div className="mt-2 flex items-center gap-2">
            <time className="text-xs text-gray-500">
              {formatTimeAgo(notification.createdDateTime)}
            </time>

            {!notification.isRead && (
              <span className="inline-flex items-center gap-1 text-xs text-blue-600">
                <span className="w-2 h-2 bg-blue-600 rounded-full" />
                New
              </span>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
