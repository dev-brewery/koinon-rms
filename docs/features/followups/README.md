# Follow-Up Queue Feature

This feature provides a comprehensive follow-up management system for tracking and managing connections with first-time visitors and attendees who need follow-up contact.

## File Structure

```
src/web/src/features/followups/
├── api.ts                  # API client functions and types
├── hooks.ts                # React Query hooks for data fetching
├── FollowUpCard.tsx        # Individual follow-up card component
├── FollowUpQueue.tsx       # Main queue page component
├── index.ts                # Public exports
└── README.md               # This file
```

## Components

### FollowUpQueue

The main page component that displays a list of pending follow-ups with filtering and status management.

**Props:**
- `assignedToIdKey?: IdKey` - Optional filter to show only follow-ups assigned to a specific user

**Features:**
- Summary statistics with clickable filters (Total, Pending, Contacted, Connected, No Response)
- Real-time filtering by status
- Optimistic updates for status changes
- Loading and error states
- Empty state handling

**Usage:**
```tsx
import { FollowUpQueue } from '@/features/followups';

// Show all follow-ups
<FollowUpQueue />

// Show only follow-ups assigned to a specific user
<FollowUpQueue assignedToIdKey="abc123" />
```

### FollowUpCard

A card component for displaying individual follow-up items with inline editing and status management.

**Props:**
- `followUp: FollowUpDto` - The follow-up data to display
- `className?: string` - Optional CSS class names

**Features:**
- Status badge with color-coded indicators
- Inline notes editing
- Quick action buttons for status updates
- Displays assignment information
- Shows contact and completion timestamps

**Usage:**
```tsx
import { FollowUpCard } from '@/features/followups';

<FollowUpCard followUp={followUpData} />
```

## Hooks

### usePendingFollowUps

Fetches the list of pending follow-ups, optionally filtered by assigned user.

```tsx
import { usePendingFollowUps } from '@/features/followups';

function MyComponent() {
  const { data, isLoading, error } = usePendingFollowUps();
  // or with filter:
  const { data, isLoading, error } = usePendingFollowUps('user-idkey');
}
```

### useUpdateFollowUpStatus

Mutation hook for updating a follow-up's status and notes.

```tsx
import { useUpdateFollowUpStatus, FollowUpStatus } from '@/features/followups';

function MyComponent() {
  const updateStatus = useUpdateFollowUpStatus();

  const handleUpdate = () => {
    updateStatus.mutate({
      idKey: 'followup-idkey',
      status: FollowUpStatus.Connected,
      notes: 'Had a great conversation!',
    });
  };
}
```

### useAssignFollowUp

Mutation hook for assigning a follow-up to a user.

```tsx
import { useAssignFollowUp } from '@/features/followups';

function MyComponent() {
  const assignFollowUp = useAssignFollowUp();

  const handleAssign = () => {
    assignFollowUp.mutate({
      idKey: 'followup-idkey',
      assignedToIdKey: 'user-idkey',
    });
  };
}
```

## API Types

### FollowUpStatus

Enum representing the current status of a follow-up:

- `Pending (0)` - Initial state, not yet contacted
- `Contacted (1)` - Contact attempt has been made
- `NoResponse (2)` - Contact attempted but no response received
- `Connected (3)` - Successfully connected with the person
- `Declined (4)` - Person declined further contact

### FollowUpDto

```typescript
interface FollowUpDto {
  idKey: IdKey;
  personIdKey: IdKey;
  personName: string;
  attendanceIdKey?: IdKey;
  status: FollowUpStatus;
  notes?: string;
  assignedToIdKey?: IdKey;
  assignedToName?: string;
  contactedDateTime?: string;
  completedDateTime?: string;
  createdDateTime: string;
}
```

## API Endpoints

The feature expects the following backend API endpoints:

- `GET /api/v1/followups/pending?assignedToIdKey={idKey}` - Get pending follow-ups
- `GET /api/v1/followups/{idKey}` - Get a specific follow-up
- `PATCH /api/v1/followups/{idKey}/status` - Update follow-up status
- `POST /api/v1/followups/{idKey}/assign` - Assign follow-up to a user

## Styling

The feature uses Tailwind CSS for styling and follows the existing design system:

- Color-coded status badges (yellow=Pending, blue=Contacted, green=Connected, red=Declined, gray=No Response)
- Responsive grid layout for statistics
- Touch-friendly button sizes (minimum 48px)
- Consistent spacing and typography

## Performance Considerations

- Uses TanStack Query for efficient data fetching and caching
- Implements optimistic updates for instant UI feedback
- Stale time set to 30 seconds for follow-up data
- Automatic cache invalidation on mutations
- Memoized filtering to prevent unnecessary re-renders

## Testing

To test the feature:

1. Ensure the backend API is running and has follow-up data
2. Import and render the FollowUpQueue component
3. Test status filtering by clicking on statistic cards
4. Test status updates using the action buttons
5. Test notes editing inline

## Future Enhancements

Potential future improvements:

- Bulk assignment of follow-ups
- Email/SMS integration for direct contact
- Activity history timeline
- Due date tracking
- Reminder notifications
- Export to CSV
- Advanced filtering (date range, assigned user dropdown)
- Search functionality
