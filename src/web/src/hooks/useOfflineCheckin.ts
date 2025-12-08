/**
 * Offline Check-in Hook
 * Handles online/offline check-in with automatic queueing and syncing
 */

import { useState, useEffect, useCallback, useRef } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { RecordAttendanceRequest, RecordAttendanceResponse } from '@/services/api/types';
import { recordAttendance } from '@/services/api/checkin';
import { offlineCheckinQueue, type SyncResult } from '@/services/offline/OfflineCheckinQueue';

export type CheckinMode = 'online' | 'offline';
export type SyncStatus = 'idle' | 'syncing' | 'success' | 'error';

export interface OfflineCheckinState {
  mode: CheckinMode;
  queuedCount: number;
  syncStatus: SyncStatus;
  lastSyncResults?: SyncResult[];
  isOnline: boolean;
}

export interface UseOfflineCheckinResult {
  recordCheckin: (request: RecordAttendanceRequest) => Promise<RecordAttendanceResponse | void>;
  state: OfflineCheckinState;
  syncQueue: () => Promise<void>;
  clearQueue: () => Promise<void>;
  isPending: boolean;
}

/**
 * Hook for managing online/offline check-ins with automatic queue sync
 */
export function useOfflineCheckin(): UseOfflineCheckinResult {
  const queryClient = useQueryClient();

  // Track online/offline status
  const [isOnline, setIsOnline] = useState(navigator.onLine);
  const [queuedCount, setQueuedCount] = useState(0);
  const [syncStatus, setSyncStatus] = useState<SyncStatus>('idle');
  const [lastSyncResults, setLastSyncResults] = useState<SyncResult[]>();

  // Track timeouts for cleanup to prevent memory leaks
  const syncStatusTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Online check-in mutation
  const onlineCheckinMutation = useMutation({
    mutationFn: (request: RecordAttendanceRequest) => recordAttendance(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['checkin', 'opportunities'] });
    },
  });

  /**
   * Update queued count from IndexedDB
   */
  const updateQueuedCount = useCallback(async () => {
    const count = await offlineCheckinQueue.getQueuedCount();
    setQueuedCount(count);
  }, []);

  /**
   * Sync queue with server
   */
  const syncQueue = useCallback(async () => {
    if (!isOnline) {
      return;
    }

    // Clear any existing status timeout
    if (syncStatusTimeoutRef.current) {
      clearTimeout(syncStatusTimeoutRef.current);
      syncStatusTimeoutRef.current = null;
    }

    setSyncStatus('syncing');

    try {
      const results = await offlineCheckinQueue.processQueue();
      setLastSyncResults(results);

      // Update queued count after sync
      await updateQueuedCount();

      // Invalidate opportunities to refresh current attendance
      queryClient.invalidateQueries({ queryKey: ['checkin', 'opportunities'] });

      // Set status based on results
      const hasFailures = results.some(r => !r.success && !r.isDuplicate);
      setSyncStatus(hasFailures ? 'error' : 'success');

      // Reset to idle after showing status (tracked for cleanup)
      syncStatusTimeoutRef.current = setTimeout(() => {
        setSyncStatus('idle');
      }, 3000);
    } catch (error) {
      if (import.meta.env.DEV) {
        // eslint-disable-next-line no-console
        console.error('Failed to sync queue:', error);
      }
      setSyncStatus('error');

      // Reset to idle after showing status (tracked for cleanup)
      syncStatusTimeoutRef.current = setTimeout(() => {
        setSyncStatus('idle');
      }, 3000);
    }
  }, [isOnline, queryClient, updateQueuedCount]);

  /**
   * Clear entire queue
   */
  const clearQueue = useCallback(async () => {
    await offlineCheckinQueue.clearQueue();
    await updateQueuedCount();
  }, [updateQueuedCount]);

  /**
   * Record check-in (online or offline)
   */
  const recordCheckin = useCallback(
    async (request: RecordAttendanceRequest): Promise<RecordAttendanceResponse | void> => {
      if (isOnline) {
        // Try online check-in first
        try {
          const response = await onlineCheckinMutation.mutateAsync(request);
          return response;
        } catch (error) {
          // If online check-in fails, fall back to queue
          console.warn('Online check-in failed, adding to queue:', error);
          await offlineCheckinQueue.addToQueue(request);
          await updateQueuedCount();
          // Don't throw - let caller know it was queued
          return undefined;
        }
      } else {
        // Offline - add to queue
        await offlineCheckinQueue.addToQueue(request);
        await updateQueuedCount();
        return undefined;
      }
    },
    [isOnline, onlineCheckinMutation, updateQueuedCount]
  );

  // Keep stable reference to syncQueue for event handlers
  const syncQueueRef = useRef(syncQueue);
  syncQueueRef.current = syncQueue;

  /**
   * Handle online/offline events
   * Uses ref to avoid circular dependency between syncQueue and this effect
   */
  useEffect(() => {
    const handleOnline = () => {
      setIsOnline(true);
      // Automatically sync when coming back online (uses ref for stable reference)
      syncQueueRef.current();
    };

    const handleOffline = () => {
      setIsOnline(false);
    };

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []); // Empty deps - handlers use ref for current syncQueue

  /**
   * Update queued count on mount
   */
  useEffect(() => {
    updateQueuedCount();
  }, [updateQueuedCount]);

  // Keep stable reference to updateQueuedCount for interval
  // Note: updateQueuedCount is already stable (useCallback with empty deps) but we document this
  const updateQueuedCountRef = useRef(updateQueuedCount);
  updateQueuedCountRef.current = updateQueuedCount;

  /**
   * Periodic queue count updates (every 5 seconds)
   * Uses ref for guaranteed stability - interval is set once on mount
   */
  useEffect(() => {
    const interval = setInterval(() => {
      updateQueuedCountRef.current();
    }, 5000);

    return () => clearInterval(interval);
  }, []); // Empty deps - guaranteed single interval

  /**
   * Cleanup sync status timeout on unmount
   */
  useEffect(() => {
    return () => {
      if (syncStatusTimeoutRef.current) {
        clearTimeout(syncStatusTimeoutRef.current);
      }
    };
  }, []);

  return {
    recordCheckin,
    state: {
      mode: isOnline ? 'online' : 'offline',
      queuedCount,
      syncStatus,
      lastSyncResults,
      isOnline,
    },
    syncQueue,
    clearQueue,
    isPending: onlineCheckinMutation.isPending,
  };
}
