# Offline Check-in Queue

This module provides offline support for the Koinon RMS check-in kiosk, allowing check-ins to be queued when network connectivity is unavailable and automatically synced when the connection is restored.

## Overview

The offline check-in queue system consists of three main components:

1. **OfflineCheckinQueue** - IndexedDB-based storage and sync logic
2. **useOfflineCheckin** - React hook for online/offline check-in management
3. **OfflineQueueIndicator** - UI component showing queue status

## Architecture

### Storage Layer (IndexedDB)

Check-ins are persisted in IndexedDB with the following schema:

```typescript
interface QueuedCheckin {
  id: string;                     // UUID for deduplication
  request: RecordAttendanceRequest; // Original check-in request
  timestamp: number;               // Queue time
  attempts: number;                // Retry counter
  status: 'pending' | 'syncing' | 'failed' | 'success';
  error?: string;                  // Last error message
  lastAttemptTime?: number;        // For exponential backoff
}
```

### Retry Logic

- **Max attempts:** 3
- **Backoff strategy:** Exponential (1s, 2s, 4s)
- **Conflict handling:** 409 responses (duplicate check-in) are treated as success

### Privacy

Successfully synced items are marked for removal and cleaned up on the next sync cycle. This avoids memory leaks from delayed cleanup timers while ensuring PII is not retained indefinitely.

### Queue Limits

Maximum queue size is 100 items to prevent unbounded growth if the device stays offline for extended periods.

## Usage

### In a Component

```typescript
import { useOfflineCheckin } from '@/hooks/useOfflineCheckin';
import { OfflineQueueIndicator } from '@/components/checkin';

function CheckinPage() {
  const {
    recordCheckin,
    state,
    syncQueue,
    isPending
  } = useOfflineCheckin();

  const handleCheckIn = async () => {
    const response = await recordCheckin({
      checkins: [{ personIdKey, groupIdKey, locationIdKey, scheduleIdKey }]
    });

    if (response) {
      // Online - got immediate response
      console.log('Check-in successful:', response);
    } else {
      // Offline - queued for later sync
      console.log('Check-in queued');
    }
  };

  return (
    <>
      <OfflineQueueIndicator state={state} onSync={syncQueue} />
      {/* Your UI */}
    </>
  );
}
```

### Direct Queue Access

```typescript
import { offlineCheckinQueue } from '@/services/offline/OfflineCheckinQueue';

// Add to queue
const id = await offlineCheckinQueue.addToQueue(request);

// Get queue count
const count = await offlineCheckinQueue.getQueuedCount();

// Process queue manually
const results = await offlineCheckinQueue.processQueue();

// Clear queue
await offlineCheckinQueue.clearQueue();
```

## State Management

The `useOfflineCheckin` hook provides:

```typescript
interface OfflineCheckinState {
  mode: 'online' | 'offline';        // Current network mode
  queuedCount: number;                // Items in queue
  syncStatus: SyncStatus;             // Sync progress
  lastSyncResults?: SyncResult[];     // Last sync results
  isOnline: boolean;                  // Navigator online status
}
```

## Automatic Sync

The hook automatically:

1. **Detects network changes** - Listens to `online`/`offline` events
2. **Triggers sync on reconnection** - Starts processing queue when online
3. **Updates queue count** - Polls every 5 seconds

## UI Indicators

The `OfflineQueueIndicator` component shows:

- **Offline mode banner** - Orange alert when offline
- **Queue count** - Blue badge showing queued items
- **Sync progress** - Purple spinner during sync
- **Success message** - Green checkmark on successful sync
- **Error message** - Red alert on sync failure

## Error Handling

### 409 Conflict (Already Checked In)

When the server returns 409, it means the person is already checked in. This is treated as a successful sync and the item is removed from the queue.

### Network Errors

Network errors trigger exponential backoff retry logic. After 3 failed attempts, the item is marked as `failed` and won't retry automatically.

### Manual Retry

Users can manually trigger sync via the "Sync Now" or "Retry" buttons in the UI.

## Performance

- **Offline check-in:** <50ms (local IndexedDB write)
- **Online check-in:** <200ms (API call)
- **Queue processing:** Sequential (prevents race conditions)

## Testing

```bash
npm test -- src/services/offline/__tests__/OfflineCheckinQueue.test.ts
```

Tests use `fake-indexeddb` for IndexedDB simulation in Node.js environment.

## Implementation Files

| File | Purpose |
|------|---------|
| `OfflineCheckinQueue.ts` | Core queue logic and IndexedDB persistence |
| `useOfflineCheckin.ts` | React hook for offline-aware check-in |
| `OfflineQueueIndicator.tsx` | UI component for queue status |
| `__tests__/OfflineCheckinQueue.test.ts` | Unit tests |

## Known Limitations

1. **Service Worker BackgroundSync** - Not currently implemented (manual sync on online event instead)
2. **Queue size limits** - Limited to 100 items (configurable in OfflineCheckinQueue.ts)
3. **Label printing** - Offline check-ins cannot print labels (queued for later)

## Future Enhancements

- [ ] Service Worker BackgroundSync API integration
- [ ] Queue size monitoring and alerts
- [ ] Export/import queue for debugging
- [ ] Analytics for offline usage patterns
- [ ] Batch sync optimization for large queues
