/**
 * Notification list component
 * Displays a list of notifications with loading and empty states
 */

import { useNavigate } from 'react-router-dom';
import { useNotifications, useMarkAsRead, useDeleteNotification } from '@/hooks/useNotifications';
import { NotificationItem } from './NotificationItem';
import { Loading } from '@/components/ui/Loading';
import { EmptyState } from '@/components/ui/EmptyState';
import type { NotificationDto } from '@/types/notification';

export interface NotificationListProps {
  unreadOnly?: boolean;
  limit?: number;
  onNotificationClick?: (notification: NotificationDto) => void;
}

export function NotificationList({
  unreadOnly,
  limit,
  onNotificationClick,
}: NotificationListProps) {
  const navigate = useNavigate();
  const { data: notifications, isLoading, error } = useNotifications(unreadOnly, limit);
  const markAsReadMutation = useMarkAsRead();
  const deleteMutation = useDeleteNotification();

  const handleMarkAsRead = (idKey: string) => {
    markAsReadMutation.mutate(idKey);
  };

  const handleDelete = (idKey: string) => {
    deleteMutation.mutate(idKey);
  };

  const handleNotificationClick = (notification: NotificationDto) => {
    if (onNotificationClick) {
      onNotificationClick(notification);
    } else if (notification.actionUrl) {
      navigate(notification.actionUrl);
    }
  };

  if (isLoading) {
    return (
      <div className="py-8">
        <Loading variant="spinner" text="Loading notifications..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className="py-8">
        <EmptyState
          icon={
            <svg className="w-12 h-12 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
          }
          title="Failed to load notifications"
          description="There was an error loading your notifications. Please try again."
        />
      </div>
    );
  }

  if (!notifications || notifications.length === 0) {
    return (
      <div className="py-8">
        <EmptyState
          icon={
            <svg className="w-12 h-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
              />
            </svg>
          }
          title={unreadOnly ? 'No unread notifications' : 'No notifications'}
          description={
            unreadOnly
              ? "You're all caught up! No new notifications to read."
              : "You don't have any notifications yet."
          }
        />
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {notifications.map((notification) => (
        <NotificationItem
          key={notification.idKey}
          notification={notification}
          onMarkAsRead={handleMarkAsRead}
          onDelete={handleDelete}
          onClick={handleNotificationClick}
        />
      ))}
    </div>
  );
}
