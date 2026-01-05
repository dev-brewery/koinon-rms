/**
 * Offline Check-in Queue Service
 * Persists check-in requests in IndexedDB when offline and syncs when online
 */

import { openDB, type IDBPDatabase } from 'idb';
import type { CheckinRequestItem, BatchCheckinResultDto } from '@/services/api/types';
import { recordAttendance } from '@/services/api/checkin';
import { ApiClientError } from '@/services/api/client';

const DB_NAME = 'koinon-checkin-offline';
const STORE_NAME = 'queue';
const DB_VERSION = 1;

export type QueueStatus = 'pending' | 'syncing' | 'failed' | 'success';

export interface QueuedCheckin {
  id: string; // UUID for deduplication
  items: CheckinRequestItem[];
  timestamp: number;
  attempts: number;
  status: QueueStatus;
  error?: string;
  lastAttemptTime?: number;
}

export interface SyncResult {
  id: string;
  success: boolean;
  response?: BatchCheckinResultDto;
  error?: string;
  isDuplicate?: boolean; // True if server returned 409 Conflict
}

/**
 * Offline check-in queue manager
 * Handles queueing, persistence, and syncing of check-ins when offline
 */
class OfflineCheckinQueue {
  private db: IDBPDatabase | null = null;
  private maxAttempts = 3;
  private maxQueueSize = 100; // Prevent unbounded growth
  private backoffDelays = [1000, 2000, 4000]; // Exponential backoff: 1s, 2s, 4s
  private pendingRemovals: Set<string> = new Set(); // Track items to remove

  /**
   * Initialize IndexedDB connection
   */
  private async getDB(): Promise<IDBPDatabase> {
    if (this.db) {
      return this.db;
    }

    this.db = await openDB(DB_NAME, DB_VERSION, {
      upgrade(db) {
        // Create object store if it doesn't exist
        if (!db.objectStoreNames.contains(STORE_NAME)) {
          const store = db.createObjectStore(STORE_NAME, { keyPath: 'id' });
          store.createIndex('timestamp', 'timestamp');
          store.createIndex('status', 'status');
        }
      },
    });

    return this.db;
  }

  /**
   * Generate unique ID for queue item using crypto-secure UUID
   */
  private generateId(): string {
    return crypto.randomUUID();
  }

  /**
   * Add check-in items to queue
   * @returns ID of queued item
   * @throws Error if queue is full (max 100 items)
   */
  async addToQueue(items: CheckinRequestItem[]): Promise<string> {
    const db = await this.getDB();

    // Check queue size limit
    const currentCount = await this.getQueuedCount();
    if (currentCount >= this.maxQueueSize) {
      throw new Error(`Queue is full (max ${this.maxQueueSize} items). Please wait for sync to complete.`);
    }

    const id = this.generateId();

    const queuedItem: QueuedCheckin = {
      id,
      items,
      timestamp: Date.now(),
      attempts: 0,
      status: 'pending',
    };

    await db.add(STORE_NAME, queuedItem);
    return id;
  }

  /**
   * Get count of queued check-ins
   */
  async getQueuedCount(): Promise<number> {
    const db = await this.getDB();
    const index = db.transaction(STORE_NAME).store.index('status');
    const pendingCount = await index.count('pending');
    const syncingCount = await index.count('syncing');
    return pendingCount + syncingCount;
  }

  /**
   * Get all queued items (for display/debugging)
   */
  async getAllQueued(): Promise<QueuedCheckin[]> {
    const db = await this.getDB();
    const tx = db.transaction(STORE_NAME, 'readonly');
    const index = tx.store.index('status');

    const pending = await index.getAll('pending');
    const syncing = await index.getAll('syncing');

    // Sort by timestamp, use ID as tiebreaker for stable ordering
    return [...pending, ...syncing].sort((a, b) => {
      const timeDiff = a.timestamp - b.timestamp;
      if (timeDiff !== 0) return timeDiff;
      return a.id.localeCompare(b.id);
    });
  }

  /**
   * Process queue and sync with server
   * Cleans up successfully synced items immediately (no delayed cleanup to avoid memory leaks)
   * @returns Array of sync results
   */
  async processQueue(): Promise<SyncResult[]> {
    const db = await this.getDB();

    // Clean up any items marked for removal from previous sync
    for (const id of this.pendingRemovals) {
      await this.removeFromQueue(id);
    }
    this.pendingRemovals.clear();

    const queued = await this.getAllQueued();

    if (queued.length === 0) {
      return [];
    }

    const results: SyncResult[] = [];

    for (const item of queued) {
      // Check if max attempts exceeded
      if (item.attempts >= this.maxAttempts) {
        results.push({
          id: item.id,
          success: false,
          error: `Max retry attempts (${this.maxAttempts}) exceeded`,
        });

        // Update status to failed
        await db.put(STORE_NAME, {
          ...item,
          status: 'failed' as QueueStatus,
          error: `Max retry attempts (${this.maxAttempts}) exceeded`,
        });
        continue;
      }

      // Apply exponential backoff if not first attempt
      if (item.attempts > 0 && item.lastAttemptTime) {
        const backoffDelay = this.backoffDelays[item.attempts - 1] || this.backoffDelays[this.backoffDelays.length - 1];
        const timeSinceLastAttempt = Date.now() - item.lastAttemptTime;

        if (timeSinceLastAttempt < backoffDelay) {
          // Skip this item, not enough time has passed
          continue;
        }
      }

      // Update to syncing status
      const updatedItem: QueuedCheckin = {
        ...item,
        status: 'syncing',
        attempts: item.attempts + 1,
        lastAttemptTime: Date.now(),
      };
      await db.put(STORE_NAME, updatedItem);

      try {
        // Attempt to sync with server
        const response = await recordAttendance(item.items);

        // Success - mark for removal on next processQueue call
        results.push({
          id: item.id,
          success: true,
          response,
        });

        // Mark as success for UI feedback
        await db.put(STORE_NAME, {
          ...updatedItem,
          status: 'success' as QueueStatus,
        });

        // Mark for cleanup on next sync (avoids setTimeout memory leak)
        this.pendingRemovals.add(item.id);

      } catch (error) {
        // Check if it's a 409 Conflict (already checked in) using proper type check
        const is409 = error instanceof ApiClientError && error.statusCode === 409;

        if (is409) {
          // Handle duplicate check-in gracefully
          results.push({
            id: item.id,
            success: true,
            isDuplicate: true,
          });

          // Remove from queue immediately - already checked in, no PII concern
          await this.removeFromQueue(item.id);
        } else {
          // Other error - will retry with backoff
          const errorMessage = error instanceof Error ? error.message : 'Unknown error';

          results.push({
            id: item.id,
            success: false,
            error: errorMessage,
          });

          // Update back to pending with error info
          await db.put(STORE_NAME, {
            ...updatedItem,
            status: 'pending' as QueueStatus,
            error: errorMessage,
          });
        }
      }
    }

    return results;
  }

  /**
   * Remove item from queue
   */
  async removeFromQueue(id: string): Promise<void> {
    const db = await this.getDB();
    await db.delete(STORE_NAME, id);
  }

  /**
   * Clear entire queue (use with caution)
   */
  async clearQueue(): Promise<void> {
    const db = await this.getDB();
    await db.clear(STORE_NAME);
  }

  /**
   * Get specific queued item by ID
   */
  async getQueuedItem(id: string): Promise<QueuedCheckin | undefined> {
    const db = await this.getDB();
    return await db.get(STORE_NAME, id);
  }
}

// Export singleton instance
export const offlineCheckinQueue = new OfflineCheckinQueue();
