/**
 * Notification bell component
 * Shows unread count badge and dropdown with recent notifications
 */

import { useState, useRef, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useUnreadCount, useMarkAllAsRead } from '@/hooks/useNotifications';
import { useNotificationHub } from '@/hooks/useNotificationHub';
import { NotificationList } from './NotificationList';
import { Button } from '@/components/ui/Button';
import { cn } from '@/lib/utils';

const MAX_DROPDOWN_NOTIFICATIONS = 5;

export function NotificationBell() {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const { data: unreadCount } = useUnreadCount();
  const markAllAsReadMutation = useMarkAllAsRead();

  // Connect to SignalR for real-time updates
  useNotificationHub(true);

  // Close dropdown when clicking outside or pressing Escape
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }

    function handleEscapeKey(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setIsOpen(false);
      }
    }

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
      document.addEventListener('keydown', handleEscapeKey);
      return () => {
        document.removeEventListener('mousedown', handleClickOutside);
        document.removeEventListener('keydown', handleEscapeKey);
      };
    }
  }, [isOpen]);

  const handleMarkAllAsRead = () => {
    markAllAsReadMutation.mutate();
  };

  const hasUnread = unreadCount !== undefined && unreadCount > 0;

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        aria-label={hasUnread ? `${unreadCount} unread notifications` : 'Notifications'}
        aria-haspopup="true"
        aria-expanded={isOpen}
        className={cn(
          'relative p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg transition-colors',
          isOpen && 'bg-gray-100 text-gray-700'
        )}
      >
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
          />
        </svg>

        {/* Unread badge */}
        {hasUnread && (
          <span className="absolute top-0.5 right-0.5 flex items-center justify-center min-w-[20px] h-5 px-1.5 text-xs font-bold text-white bg-red-600 rounded-full">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {/* Dropdown */}
      {isOpen && (
        <div
          role="menu"
          className="absolute right-0 mt-2 w-96 max-w-[calc(100vw-2rem)] bg-white rounded-lg shadow-lg border border-gray-200 max-h-[600px] overflow-hidden flex flex-col z-50"
        >
          {/* Header */}
          <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200">
            <h3 className="text-lg font-semibold text-gray-900">Notifications</h3>

            {hasUnread && (
              <Button
                variant="ghost"
                size="sm"
                onClick={handleMarkAllAsRead}
                disabled={markAllAsReadMutation.isPending}
                className="text-sm"
              >
                Mark all as read
              </Button>
            )}
          </div>

          {/* Notification list */}
          <div className="overflow-y-auto flex-1 p-4">
            <NotificationList
              unreadOnly={false}
              limit={MAX_DROPDOWN_NOTIFICATIONS}
              onNotificationClick={() => setIsOpen(false)}
            />
          </div>

          {/* Footer */}
          <div className="border-t border-gray-200 p-3">
            <Link
              to="/notifications"
              onClick={() => setIsOpen(false)}
              className="block w-full text-center text-sm font-medium text-blue-600 hover:text-blue-700 py-2 rounded hover:bg-blue-50 transition-colors"
            >
              View all notifications
            </Link>
          </div>
        </div>
      )}
    </div>
  );
}
