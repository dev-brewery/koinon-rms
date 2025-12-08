# Integration Guide

This guide shows how to integrate the Follow-Up Queue feature into the Koinon RMS application.

## Adding to Router

Example integration with React Router:

```tsx
// src/App.tsx or your routing configuration
import { Routes, Route } from 'react-router-dom';
import { FollowUpQueue } from '@/features/followups';

function App() {
  return (
    <Routes>
      {/* ... other routes ... */}

      {/* Follow-up queue accessible to all staff */}
      <Route path="/followups" element={<FollowUpQueue />} />

      {/* Follow-up queue filtered by current user */}
      <Route
        path="/my-followups"
        element={<FollowUpQueue assignedToIdKey={currentUser.idKey} />}
      />
    </Routes>
  );
}
```

## Adding Navigation Link

Example navigation menu item:

```tsx
// In your sidebar or navigation component
<nav>
  <Link
    to="/followups"
    className="flex items-center px-4 py-2 text-gray-700 hover:bg-gray-100"
  >
    <svg className="w-5 h-5 mr-3" /* ... icon SVG ... */ />
    Follow-up Queue
  </Link>
</nav>
```

## Dashboard Widget

Example dashboard widget showing follow-up count:

```tsx
// src/components/admin/dashboard/FollowUpWidget.tsx
import { usePendingFollowUps } from '@/features/followups';
import { Link } from 'react-router-dom';

export function FollowUpWidget() {
  const { data: followUps, isLoading } = usePendingFollowUps();

  if (isLoading) {
    return <div>Loading...</div>;
  }

  const pendingCount = followUps?.filter(f => f.status === 0).length || 0;

  return (
    <Link
      to="/followups"
      className="p-6 bg-white rounded-lg shadow hover:shadow-lg transition-shadow"
    >
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm text-gray-600">Pending Follow-ups</p>
          <p className="text-3xl font-bold text-gray-900">{pendingCount}</p>
        </div>
        <svg className="w-10 h-10 text-blue-600" /* ... */ />
      </div>
    </Link>
  );
}
```

## Notification Badge

Example notification badge in navigation:

```tsx
import { usePendingFollowUps } from '@/features/followups';

export function NavigationBadge() {
  const { data: followUps } = usePendingFollowUps();
  const pendingCount = followUps?.filter(f => f.status === 0).length || 0;

  return (
    <Link to="/followups" className="relative">
      Follow-ups
      {pendingCount > 0 && (
        <span className="absolute -top-2 -right-2 bg-red-500 text-white text-xs font-bold rounded-full h-5 w-5 flex items-center justify-center">
          {pendingCount}
        </span>
      )}
    </Link>
  );
}
```

## Auto-Assignment on Check-in

Example integration with check-in flow to auto-create follow-ups:

```tsx
// In your check-in confirmation component
import { createFollowUp } from '@/features/followups/api';

async function handleCheckinComplete(attendance) {
  // After successful check-in
  if (attendance.isFirstTime) {
    // Create a follow-up for first-time visitors
    await createFollowUp({
      personIdKey: attendance.personIdKey,
      attendanceIdKey: attendance.idKey,
      status: FollowUpStatus.Pending,
      notes: 'First-time visitor from check-in',
    });
  }
}
```

Note: You'll need to add the `createFollowUp` function to `api.ts`:

```typescript
// Add to src/web/src/features/followups/api.ts
export interface CreateFollowUpRequest {
  personIdKey: IdKey;
  attendanceIdKey?: IdKey;
  status?: FollowUpStatus;
  notes?: string;
  assignedToIdKey?: IdKey;
}

export async function createFollowUp(
  request: CreateFollowUpRequest
): Promise<FollowUpDto> {
  const response = await post<{ data: FollowUpDto }>('/followups', request);
  return response.data;
}
```

## Permission-Based Access

Example with role-based access control:

```tsx
import { Navigate } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { FollowUpQueue } from '@/features/followups';

function ProtectedFollowUpQueue() {
  const { user } = useAuth();

  // Only staff can access follow-ups
  if (!user || !user.roles.includes('Staff')) {
    return <Navigate to="/unauthorized" />;
  }

  // Volunteers only see their own assignments
  if (user.roles.includes('Volunteer')) {
    return <FollowUpQueue assignedToIdKey={user.idKey} />;
  }

  // Staff see all follow-ups
  return <FollowUpQueue />;
}
```

## TanStack Query Configuration

Ensure your app has TanStack Query configured:

```tsx
// src/main.tsx
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
      staleTime: 30 * 1000, // 30 seconds
    },
  },
});

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <YourRoutes />
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}
```

## Backend API Requirements

The backend needs to implement these endpoints:

### GET /api/v1/followups/pending

Query Parameters:
- `assignedToIdKey` (optional): Filter by assigned user

Response:
```json
{
  "data": [
    {
      "idKey": "abc123",
      "personIdKey": "person123",
      "personName": "John Doe",
      "status": 0,
      "createdDateTime": "2024-01-15T10:30:00Z"
    }
  ]
}
```

### PATCH /api/v1/followups/{idKey}/status

Request Body:
```json
{
  "status": 1,
  "notes": "Called and left voicemail"
}
```

Response:
```json
{
  "data": {
    "idKey": "abc123",
    "status": 1,
    "notes": "Called and left voicemail",
    "contactedDateTime": "2024-01-15T14:30:00Z"
  }
}
```

### POST /api/v1/followups/{idKey}/assign

Request Body:
```json
{
  "assignedToIdKey": "user123"
}
```

Response: 204 No Content

## Testing

Example test for the FollowUpQueue component:

```tsx
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { FollowUpQueue } from './FollowUpQueue';

const mockFollowUps = [
  {
    idKey: '1',
    personName: 'John Doe',
    status: 0,
    createdDateTime: '2024-01-15T10:00:00Z',
  },
];

test('renders follow-up queue', async () => {
  const queryClient = new QueryClient();

  render(
    <QueryClientProvider client={queryClient}>
      <FollowUpQueue />
    </QueryClientProvider>
  );

  await waitFor(() => {
    expect(screen.getByText('Follow-up Queue')).toBeInTheDocument();
  });
});
```
