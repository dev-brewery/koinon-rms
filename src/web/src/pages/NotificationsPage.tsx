/**
 * Notifications page
 * Full page view for viewing and managing all notifications
 */

import { useState } from 'react';
import { NotificationList } from '@/components/notifications/NotificationList';
import { useMarkAllAsRead } from '@/hooks/useNotifications';
import { Button } from '@/components/ui/Button';
import { cn } from '@/lib/utils';

type FilterTab = 'all' | 'unread';

export function NotificationsPage() {
  const [activeTab, setActiveTab] = useState<FilterTab>('all');
  const markAllAsReadMutation = useMarkAllAsRead();

  const handleMarkAllAsRead = () => {
    markAllAsReadMutation.mutate();
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-4xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-6">
          <h1 className="text-3xl font-bold text-gray-900">Notifications</h1>
          <p className="mt-2 text-gray-600">Stay updated with your latest notifications</p>
        </div>

        {/* Filter tabs and actions */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 mb-6">
          <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
            <div className="flex gap-4">
              <button
                onClick={() => setActiveTab('all')}
                className={cn(
                  'px-4 py-2 text-sm font-medium rounded-lg transition-colors',
                  activeTab === 'all'
                    ? 'bg-blue-100 text-blue-700'
                    : 'text-gray-600 hover:bg-gray-100'
                )}
              >
                All
              </button>
              <button
                onClick={() => setActiveTab('unread')}
                className={cn(
                  'px-4 py-2 text-sm font-medium rounded-lg transition-colors',
                  activeTab === 'unread'
                    ? 'bg-blue-100 text-blue-700'
                    : 'text-gray-600 hover:bg-gray-100'
                )}
              >
                Unread
              </button>
            </div>

            <Button
              variant="outline"
              size="sm"
              onClick={handleMarkAllAsRead}
              disabled={markAllAsReadMutation.isPending}
            >
              Mark all as read
            </Button>
          </div>

          {/* Notification list */}
          <div className="p-6">
            <NotificationList unreadOnly={activeTab === 'unread'} />
          </div>
        </div>

        {/* Success/Error feedback */}
        {markAllAsReadMutation.isSuccess && (
          <div className="fixed bottom-4 right-4 bg-green-500 text-white px-6 py-3 rounded-lg shadow-lg">
            All notifications marked as read
          </div>
        )}

        {markAllAsReadMutation.isError && (
          <div className="fixed bottom-4 right-4 bg-red-500 text-white px-6 py-3 rounded-lg shadow-lg">
            Failed to mark all as read. Please try again.
          </div>
        )}
      </div>
    </div>
  );
}
