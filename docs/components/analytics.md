# First-Time Visitors Analytics Components

This directory contains analytics components for tracking and displaying first-time visitor data.

## Components

### FirstTimeVisitorWidget

A compact dashboard widget that displays today's first-time visitors.

**Features:**
- Shows count of first-time visitors today
- Lists the 5 most recent visitors
- Displays visitor name, group, check-in time, and campus
- Badge indicator for follow-up status
- Optional "View All" button when more than 5 visitors
- Loading skeleton and error states
- Empty state when no visitors

**Usage:**
```tsx
import { FirstTimeVisitorWidget } from '@/components/admin/analytics';

function DashboardPage() {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
      <FirstTimeVisitorWidget
        campusIdKey="optional-campus-id"
        onViewAll={() => navigate('/analytics/first-time-visitors')}
      />
    </div>
  );
}
```

### FirstTimeVisitorList

A full page component for viewing all first-time visitors with filtering and sorting.

**Features:**
- Date range picker (last 7 days, 30 days, 90 days, this year, custom)
- Campus filter
- Sortable table columns (Name, Check-in Time, Group, Campus, Follow-up Status)
- Loading and error states
- Empty state when no data

**Usage:**
```tsx
import { FirstTimeVisitorList } from '@/components/admin/analytics';

function FirstTimeVisitorsPage() {
  const [campusFilter, setCampusFilter] = useState('');

  return (
    <FirstTimeVisitorList
      campusFilter={campusFilter}
      onCampusFilterChange={setCampusFilter}
    />
  );
}
```

## API Endpoints

The components expect the following API endpoints to be implemented:

### GET /api/v1/analytics/first-time-visitors/today
Returns today's first-time visitors.

**Query Parameters:**
- `campusIdKey` (optional): Filter by campus

**Response:**
```json
{
  "data": [
    {
      "personIdKey": "abc123",
      "personName": "John Doe",
      "checkInDateTime": "2025-12-08T10:30:00Z",
      "groupName": "Kids Ministry",
      "campusName": "Main Campus",
      "hasFollowUp": true,
      "followUpIdKey": "def456"
    }
  ]
}
```

### GET /api/v1/analytics/first-time-visitors
Returns first-time visitors for a date range.

**Query Parameters:**
- `startDate` (required): Start date (YYYY-MM-DD)
- `endDate` (required): End date (YYYY-MM-DD)
- `campusIdKey` (optional): Filter by campus

**Response:** Same as above

## Data Hooks

Custom React Query hooks are available in `/src/hooks/useAnalytics.ts`:

- `useTodaysFirstTimeVisitors(campusIdKey?)` - Fetches today's visitors, 2-minute cache
- `useFirstTimeVisitorsByDateRange(startDate, endDate, campusIdKey?)` - Fetches visitors by date range, 5-minute cache

## Styling

Components use TailwindCSS and follow the existing design system:
- Purple theme for first-time visitor elements
- Consistent card layouts and spacing
- Responsive design (mobile-first)
- Loading skeletons and error states
- Hover effects and transitions
