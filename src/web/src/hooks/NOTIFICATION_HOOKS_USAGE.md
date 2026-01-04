# Notification Hooks Usage Guide

This guide explains how to use the notification system hooks in the Koinon RMS frontend.

## Installation

Before using the SignalR real-time features, you need to install the SignalR client:

```bash
npm install @microsoft/signalr
```

**Note:** The notification hooks will work without SignalR installed, but real-time updates won't be available. The `useNotificationHub` hook will gracefully fail if the package is not installed.

## Available Hooks

### useNotifications

Fetch notifications for the current user.

```typescript
import { useNotifications } from '@/hooks/useNotifications';

function NotificationsList() {
  // Get all notifications
  const { data: notifications, isLoading } = useNotifications();

  // Get only unread notifications
  const { data: unreadNotifications } = useNotifications(true);

  // Get limited number of notifications
  const { data: recentNotifications } = useNotifications(false, 10);

  if (isLoading) return <div>Loading...</div>;

  return (
    <ul>
      {notifications?.map(notification => (
        <li key={notification.idKey}>{notification.title}</li>
      ))}
    </ul>
  );
}
```

### useUnreadCount

Fetch the unread notification count. This query is automatically updated via SignalR when `useNotificationHub` is active.

```typescript
import { useUnreadCount } from '@/hooks/useNotifications';

function NotificationBadge() {
  const { data: count } = useUnreadCount();

  if (!count) return null;

  return <span className="badge">{count}</span>;
}
```

### useNotification

Fetch a single notification by IdKey.

```typescript
import { useNotification } from '@/hooks/useNotifications';

function NotificationDetail({ idKey }: { idKey: string }) {
  const { data: notification, isLoading } = useNotification(idKey);

  if (isLoading) return <div>Loading...</div>;
  if (!notification) return <div>Not found</div>;

  return (
    <div>
      <h2>{notification.title}</h2>
      <p>{notification.message}</p>
    </div>
  );
}
```

### useMarkAsRead

Mark a notification as read.

```typescript
import { useMarkAsRead } from '@/hooks/useNotifications';

function NotificationItem({ notification }: Props) {
  const markAsRead = useMarkAsRead();

  const handleClick = () => {
    if (!notification.isRead) {
      markAsRead.mutate(notification.idKey);
    }
  };

  return (
    <div onClick={handleClick}>
      {notification.title}
    </div>
  );
}
```

### useMarkAllAsRead

Mark all notifications as read.

```typescript
import { useMarkAllAsRead } from '@/hooks/useNotifications';

function NotificationsHeader() {
  const markAllAsRead = useMarkAllAsRead();

  return (
    <button
      onClick={() => markAllAsRead.mutate()}
      disabled={markAllAsRead.isPending}
    >
      Mark all as read
    </button>
  );
}
```

### useDeleteNotification

Delete a notification.

```typescript
import { useDeleteNotification } from '@/hooks/useNotifications';

function NotificationItem({ notification }: Props) {
  const deleteNotification = useDeleteNotification();

  const handleDelete = () => {
    deleteNotification.mutate(notification.idKey);
  };

  return (
    <div>
      <span>{notification.title}</span>
      <button onClick={handleDelete}>Delete</button>
    </div>
  );
}
```

### useNotificationPreferences

Fetch and update notification preferences.

```typescript
import {
  useNotificationPreferences,
  useUpdatePreference,
} from '@/hooks/useNotifications';
import { NotificationType } from '@/types/notification';

function NotificationPreferences() {
  const { data: preferences } = useNotificationPreferences();
  const updatePreference = useUpdatePreference();

  const handleToggle = (notificationType: NotificationType, isEnabled: boolean) => {
    updatePreference.mutate({
      notificationType,
      isEnabled,
    });
  };

  return (
    <div>
      {preferences?.map(pref => (
        <label key={pref.idKey}>
          <input
            type="checkbox"
            checked={pref.isEnabled}
            onChange={(e) => handleToggle(pref.notificationType, e.target.checked)}
          />
          Notification Type: {pref.notificationType}
        </label>
      ))}
    </div>
  );
}
```

### useNotificationHub

Connect to the SignalR notification hub for real-time updates.

**Important:** This hook requires `@microsoft/signalr` to be installed.

```typescript
import { useNotificationHub } from '@/hooks/useNotificationHub';
import { useUnreadCount } from '@/hooks/useNotifications';

function App() {
  // Connect to SignalR hub
  const { isConnected, isConnecting, error } = useNotificationHub();

  // This query will be automatically updated via SignalR
  const { data: unreadCount } = useUnreadCount();

  return (
    <div>
      {isConnecting && <div>Connecting to notifications...</div>}
      {error && <div>Connection error: {error}</div>}
      {isConnected && <div>✓ Real-time notifications active</div>}
      <div>Unread: {unreadCount}</div>
    </div>
  );
}
```

You can disable the hub connection conditionally:

```typescript
const { isAuthenticated } = useAuth();
const hub = useNotificationHub(isAuthenticated);
```

## Real-Time Updates

When `useNotificationHub` is active, the following happens automatically:

1. **ReceiveNotification** event - When a new notification arrives:
   - All notification queries are invalidated and refetched
   - The unread count is updated

2. **UnreadCountUpdated** event - When the count changes:
   - The unread count query data is updated directly (no refetch needed)

3. **Automatic Reconnection** - If the connection drops:
   - The hook will automatically attempt to reconnect every 5 seconds
   - All notification queries are refetched after successful reconnection

## API Endpoints

The hooks use the following API endpoints:

- `GET /api/v1/notifications` - List notifications
- `GET /api/v1/notifications/unread-count` - Get unread count
- `GET /api/v1/notifications/{idKey}` - Get single notification
- `POST /api/v1/notifications/{idKey}/mark-read` - Mark as read
- `POST /api/v1/notifications/mark-all-read` - Mark all as read
- `DELETE /api/v1/notifications/{idKey}` - Delete notification
- `GET /api/v1/notifications/preferences` - Get preferences
- `POST /api/v1/notifications/preferences` - Update preference

## SignalR Hub

- Hub URL: `/hubs/notifications`
- Events:
  - `ReceiveNotification` - New notification received
  - `UnreadCountUpdated` - Unread count changed

## Type Definitions

All notification types are available from `@/types/notification`:

```typescript
import {
  NotificationType,
  NotificationDto,
  NotificationPreferenceDto,
  UpdateNotificationPreferenceDto,
  UnreadCountResponse,
  MarkAllAsReadResponse,
} from '@/types/notification';
```

## Best Practices

1. **Use SignalR for real-time updates** - Install `@microsoft/signalr` and use `useNotificationHub` in your app root
2. **Show loading states** - All hooks return `isLoading` and `isPending` states
3. **Handle errors** - Use `error` from query hooks and `isError` from mutation hooks
4. **Optimistic updates** - Consider using optimistic updates for better UX (currently not implemented)
5. **Limit queries** - Use the `limit` parameter when you only need recent notifications

## Example: Complete Notification Bell

```typescript
import { useNotifications, useUnreadCount, useMarkAsRead, useNotificationHub } from '@/hooks/useNotifications';

function NotificationBell() {
  const [isOpen, setIsOpen] = useState(false);
  const { isConnected } = useNotificationHub();
  const { data: count } = useUnreadCount();
  const { data: notifications } = useNotifications(true, 10); // 10 most recent unread
  const markAsRead = useMarkAsRead();

  const handleNotificationClick = (notification: NotificationDto) => {
    if (!notification.isRead) {
      markAsRead.mutate(notification.idKey);
    }
    if (notification.actionUrl) {
      navigate(notification.actionUrl);
    }
  };

  return (
    <div>
      <button onClick={() => setIsOpen(!isOpen)}>
        🔔
        {count > 0 && <span className="badge">{count}</span>}
        {isConnected && <span className="online-indicator" />}
      </button>

      {isOpen && (
        <div className="dropdown">
          {notifications?.map(notification => (
            <div
              key={notification.idKey}
              onClick={() => handleNotificationClick(notification)}
            >
              <strong>{notification.title}</strong>
              <p>{notification.message}</p>
              <small>{new Date(notification.createdDateTime).toLocaleString()}</small>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
```
