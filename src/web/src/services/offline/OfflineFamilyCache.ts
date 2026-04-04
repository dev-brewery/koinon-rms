/**
 * Offline Family Cache Service
 * Caches family search results in IndexedDB for offline use
 */

import { openDB, type IDBPDatabase } from 'idb';
import type { CheckinFamilyDto } from '@/services/api/types';

const DB_NAME = 'checkin-cache';
const STORE_NAME = 'families';
const DB_VERSION = 1;

interface CachedFamily {
  /** Phone number or search query used to find this family */
  query: string;
  families: CheckinFamilyDto[];
  timestamp: number;
}

class OfflineFamilyCache {
  private db: IDBPDatabase | null = null;

  private async getDB(): Promise<IDBPDatabase> {
    if (this.db) {
      return this.db;
    }

    this.db = await openDB(DB_NAME, DB_VERSION, {
      upgrade(db) {
        if (!db.objectStoreNames.contains(STORE_NAME)) {
          db.createObjectStore(STORE_NAME, { keyPath: 'query' });
        }
      },
    });

    return this.db;
  }

  /**
   * Cache family search results for a query
   */
  async cacheResults(query: string, families: CheckinFamilyDto[]): Promise<void> {
    const db = await this.getDB();
    const entry: CachedFamily = {
      query,
      families,
      timestamp: Date.now(),
    };
    await db.put(STORE_NAME, entry);
  }

  /**
   * Get cached family results for a query
   * Returns null if not found
   */
  async getCachedResults(query: string): Promise<CheckinFamilyDto[] | null> {
    const db = await this.getDB();
    const entry = await db.get(STORE_NAME, query);
    if (!entry) return null;
    return (entry as CachedFamily).families;
  }

  /**
   * Clear all cached data
   */
  async clearCache(): Promise<void> {
    const db = await this.getDB();
    await db.clear(STORE_NAME);
  }

  /**
   * Get cache timestamp for staleness checks
   */
  async getCacheTimestamp(query: string): Promise<number | null> {
    const db = await this.getDB();
    const entry = await db.get(STORE_NAME, query);
    if (!entry) return null;
    return (entry as CachedFamily).timestamp;
  }
}

export const offlineFamilyCache = new OfflineFamilyCache();
