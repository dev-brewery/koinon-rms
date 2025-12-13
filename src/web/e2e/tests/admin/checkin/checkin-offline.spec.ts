/**
 * E2E Tests: Check-in Offline Mode
 * Tests offline queue and sync on reconnect
 *
 * ASSUMPTIONS:
 * - ServiceWorker caches check-in API endpoints
 * - IndexedDB stores pending check-ins
 * - Background sync API triggers on reconnect
 * - UI shows sync status indicators
 */

import { test, expect } from '@playwright/test';
import { CheckinPage } from '../../../fixtures/page-objects/checkin.page';

test.describe('Check-in Offline Mode', () => {
  test.beforeEach(async ({ page }) => {
    const checkin = new CheckinPage(page);
    await checkin.goto();

    // Wait for ServiceWorker to be ready
    await page.waitForLoadState('networkidle');
  });

  test('should cache family data when online', async ({ page, context: _context }) => {
    const checkin = new CheckinPage(page);

    // Search while online to populate cache
    await checkin.searchByPhone('5551234567');
    await expect(checkin.familyMemberCards.first()).toBeVisible();

    // Verify cache populated (check IndexedDB contents)
    const cacheStatus = await page.evaluate(async () => {
      return new Promise((resolve) => {
        const request = window.indexedDB.open('checkin-cache');
        request.onsuccess = (event) => {
          const db = (event.target as IDBOpenDBRequest).result;
          const tx = db.transaction('families', 'readonly');
          const store = tx.objectStore('families');
          const countRequest = store.count();
          countRequest.onsuccess = () => {
            resolve(countRequest.result > 0);
          };
          countRequest.onerror = () => resolve(false);
        };
        request.onerror = () => resolve(false);
      });
    });
    expect(cacheStatus).toBeTruthy();
  });

  test('should queue check-in when offline', async ({ page, context }) => {
    const checkin = new CheckinPage(page);

    // Search and select member while online
    await checkin.searchByPhone('5551234567');
    await checkin.selectMember(0);

    // Go offline
    await context.setOffline(true);

    // Attempt check-in
    await checkin.confirmCheckin();

    // Should show queued status
    await expect(page.getByText(/queued|pending|will sync/i)).toBeVisible();

    // Verify stored in IndexedDB
    const queuedCount = await page.evaluate(async () => {
      return new Promise<number>((resolve) => {
        const request = window.indexedDB.open('checkin-queue');
        request.onsuccess = (event) => {
          const db = (event.target as IDBOpenDBRequest).result;
          const tx = db.transaction('pending', 'readonly');
          const store = tx.objectStore('pending');
          const countRequest = store.count();

          // Wait for transaction to complete
          tx.oncomplete = () => {
            resolve(countRequest.result);
          };
          tx.onerror = () => resolve(0);
        };
        request.onerror = () => resolve(0);
      });
    });
    expect(queuedCount).toBeGreaterThan(0);
  });

  test('should sync queued check-ins on reconnect', async ({ page, context }) => {
    const checkin = new CheckinPage(page);

    // Search and select member
    await checkin.searchByPhone('5551234567');
    await checkin.selectMember(0);

    // Go offline and check-in
    await context.setOffline(true);
    await checkin.confirmCheckin();
    await expect(page.getByText(/queued|pending/i)).toBeVisible();

    // Reconnect
    await context.setOffline(false);

    // Wait for background sync event or UI sync indicator
    await expect(page.getByText(/syncing|sync in progress/i).or(page.getByText(/synced|completed/i))).toBeVisible({ timeout: 5000 });

    // Verify sync completed
    await expect(page.getByText(/synced|completed/i)).toBeVisible({ timeout: 10000 });

    // Queue should be empty
    const queuedCount = await page.evaluate(async () => {
      const db = await window.indexedDB.open('checkin-queue');
      const tx = db.transaction('pending', 'readonly');
      const store = tx.objectStore('pending');
      return await store.count();
    });
    expect(queuedCount).toBe(0);
  });

  test('should work offline with cached data', async ({ page, context }) => {
    const checkin = new CheckinPage(page);

    // Search while online to cache
    await checkin.searchByPhone('5551234567');
    await expect(checkin.familyMemberCards.first()).toBeVisible();

    // Go offline
    await context.setOffline(true);

    // Search again (should use cache)
    await checkin.goto();
    await checkin.searchByPhone('5551234567');

    // Should still show family members
    await expect(checkin.familyMemberCards.first()).toBeVisible();
  });

  test('should show offline indicator when disconnected', async ({ page, context }) => {
    const checkin = new CheckinPage(page);

    // Go offline
    await context.setOffline(true);

    // Trigger a network request
    await checkin.searchByPhone('5551234567');

    // Should show offline indicator
    await expect(page.getByTestId('offline-indicator')).toBeVisible();
    await expect(page.getByText(/offline|no connection/i)).toBeVisible();
  });

  test('should handle multiple queued check-ins', async ({ page, context }) => {
    const checkin = new CheckinPage(page);

    // Go offline
    await context.setOffline(true);

    // Queue first check-in
    await checkin.searchByPhone('5551234567');
    await checkin.selectMember(0);
    await checkin.confirmCheckin();
    await expect(page.getByText(/queued/i)).toBeVisible();

    // Reset and queue second check-in
    await checkin.goto();
    await checkin.searchByPhone('5559876543');
    await checkin.selectMember(0);
    await checkin.confirmCheckin();

    // Verify both in queue
    const queuedCount = await page.evaluate(async () => {
      const db = await window.indexedDB.open('checkin-queue');
      const tx = db.transaction('pending', 'readonly');
      const store = tx.objectStore('pending');
      return await store.count();
    });
    expect(queuedCount).toBe(2);

    // Reconnect and verify both sync
    await context.setOffline(false);
    await page.waitForTimeout(3000);

    const remainingCount = await page.evaluate(async () => {
      const db = await window.indexedDB.open('checkin-queue');
      const tx = db.transaction('pending', 'readonly');
      const store = tx.objectStore('pending');
      return await store.count();
    });
    expect(remainingCount).toBe(0);
  });

  test('should preserve check-in timestamp when queued', async ({ page, context }) => {
    const checkin = new CheckinPage(page);

    // Go offline
    await context.setOffline(true);

    // Record time before check-in
    const beforeTime = Date.now();

    // Queue check-in
    await checkin.searchByPhone('5551234567');
    await checkin.selectMember(0);
    await checkin.confirmCheckin();

    // Verify timestamp in queue
    const timestamp = await page.evaluate(async () => {
      const db = await window.indexedDB.open('checkin-queue');
      const tx = db.transaction('pending', 'readonly');
      const store = tx.objectStore('pending');
      const items = await store.getAll();
      return items[0]?.timestamp;
    });

    expect(timestamp).toBeGreaterThanOrEqual(beforeTime);
    expect(timestamp).toBeLessThanOrEqual(Date.now());
  });

  test('should retry failed sync attempts', async ({ page, context }) => {
    const checkin = new CheckinPage(page);

    // Queue check-in offline
    await context.setOffline(true);
    await checkin.searchByPhone('5551234567');
    await checkin.selectMember(0);
    await checkin.confirmCheckin();

    // Go online but intercept API to fail
    await context.setOffline(false);
    await page.route('**/api/v1/checkin', (route) => {
      route.abort('failed');
    });

    // Wait for first retry
    await page.waitForTimeout(2000);

    // Should show retry status
    await expect(page.getByText(/retrying|sync failed/i)).toBeVisible();

    // Remove intercept and allow success
    await page.unroute('**/api/v1/checkin');

    // Wait for retry to succeed
    await expect(page.getByText(/synced/i)).toBeVisible({ timeout: 10000 });
  });

  test('@smoke should complete full offline workflow', async ({ page, context }) => {
    const checkin = new CheckinPage(page);

    // Step 1: Cache data while online
    await checkin.searchByPhone('5551234567');
    await expect(checkin.familyMemberCards.first()).toBeVisible();

    // Step 2: Go offline
    await context.setOffline(true);

    // Step 3: Perform check-in (queued)
    await checkin.selectMember(0);
    await checkin.confirmCheckin();
    await expect(page.getByText(/queued/i)).toBeVisible();

    // Step 4: Verify UI shows offline mode
    await expect(page.getByTestId('offline-indicator')).toBeVisible();

    // Step 5: Reconnect and auto-sync
    await context.setOffline(false);
    await expect(page.getByText(/synced/i)).toBeVisible({ timeout: 10000 });

    // Step 6: Verify online indicator
    await expect(page.getByTestId('offline-indicator')).not.toBeVisible();
  });

  test('should clear cache on manual reset', async ({ page }) => {
    const checkin = new CheckinPage(page);

    // Cache data
    await checkin.searchByPhone('5551234567');
    await expect(checkin.familyMemberCards.first()).toBeVisible();

    // Trigger cache clear (admin function)
    await page.getByTestId('admin-menu-button').click();
    await page.getByRole('button', { name: /clear cache/i }).click();

    // Confirm
    await page.getByRole('button', { name: /confirm/i }).click();

    // Verify cache cleared
    const cacheCleared = await page.evaluate(async () => {
      const databases = await window.indexedDB.databases();
      return databases.length === 0;
    });
    expect(cacheCleared).toBeTruthy();
  });
});

test.describe('Check-in Offline Error Handling', () => {
  test.skip('should show error when cache is full', async ({ page, context }) => {
    // SKIPPED: IndexedDB mock prevents page load - needs different approach
    // TODO(#157): Implement storage quota testing without breaking page initialization
    const checkin = new CheckinPage(page);

    // This test needs to be rewritten to mock quota at transaction time, not open time
    await context.setOffline(true);
    await checkin.searchByPhone('5551234567');
    await checkin.selectMember(0);
    await checkin.confirmCheckin();

    // Should show storage error
    await expect(page.getByText(/storage full|quota exceeded/i)).toBeVisible();
  });

  test('should warn when cache is stale', async ({ page, context }) => {
    const checkin = new CheckinPage(page);

    // Cache data
    await checkin.searchByPhone('5551234567');

    // Simulate stale cache (modify timestamp)
    await page.evaluate(() => {
      const oldTimestamp = Date.now() - 86400000 * 7; // 7 days ago
      localStorage.setItem('cache-timestamp', oldTimestamp.toString());
    });

    // Go offline
    await context.setOffline(true);

    // Search again
    await checkin.goto();
    await checkin.searchByPhone('5551234567');

    // Should show stale data warning
    await expect(page.getByText(/data may be outdated|cached/i)).toBeVisible();
  });
});
