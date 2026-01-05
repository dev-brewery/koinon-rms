/**
 * Tests for OfflineCheckinQueue
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { offlineCheckinQueue } from '../OfflineCheckinQueue';
import type { CheckinRequestItem } from '@/services/api/types';

describe('OfflineCheckinQueue', () => {
  beforeEach(async () => {
    // Clear queue before each test
    await offlineCheckinQueue.clearQueue();
  });

  it('should add check-in to queue', async () => {
    const items: CheckinRequestItem[] = [
      {
        personIdKey: 'test-person',
        groupIdKey: 'test-group',
        locationIdKey: 'test-location',
        scheduleIdKey: 'test-schedule',
      },
    ];

    const id = await offlineCheckinQueue.addToQueue(items);
    expect(id).toBeTruthy();

    const count = await offlineCheckinQueue.getQueuedCount();
    expect(count).toBe(1);
  });

  it('should retrieve queued item', async () => {
    const items: CheckinRequestItem[] = [
      {
        personIdKey: 'test-person',
        groupIdKey: 'test-group',
        locationIdKey: 'test-location',
        scheduleIdKey: 'test-schedule',
      },
    ];

    const id = await offlineCheckinQueue.addToQueue(items);
    const item = await offlineCheckinQueue.getQueuedItem(id);

    expect(item).toBeTruthy();
    expect(item?.id).toBe(id);
    expect(item?.items).toEqual(items);
    expect(item?.status).toBe('pending');
    expect(item?.attempts).toBe(0);
  });

  it('should clear queue', async () => {
    const items: CheckinRequestItem[] = [
      {
        personIdKey: 'test-person',
        groupIdKey: 'test-group',
        locationIdKey: 'test-location',
        scheduleIdKey: 'test-schedule',
      },
    ];

    await offlineCheckinQueue.addToQueue(items);
    await offlineCheckinQueue.addToQueue(items);

    let count = await offlineCheckinQueue.getQueuedCount();
    expect(count).toBe(2);

    await offlineCheckinQueue.clearQueue();
    count = await offlineCheckinQueue.getQueuedCount();
    expect(count).toBe(0);
  });

  it('should remove specific item from queue', async () => {
    const items: CheckinRequestItem[] = [
      {
        personIdKey: 'test-person',
        groupIdKey: 'test-group',
        locationIdKey: 'test-location',
        scheduleIdKey: 'test-schedule',
      },
    ];

    const id1 = await offlineCheckinQueue.addToQueue(items);
    const id2 = await offlineCheckinQueue.addToQueue(items);

    await offlineCheckinQueue.removeFromQueue(id1);

    const count = await offlineCheckinQueue.getQueuedCount();
    expect(count).toBe(1);

    const item = await offlineCheckinQueue.getQueuedItem(id2);
    expect(item).toBeTruthy();
  });

  it('should handle multiple queued items', async () => {
    const items1: CheckinRequestItem[] = [
      {
        personIdKey: 'person-1',
        groupIdKey: 'group-1',
        locationIdKey: 'location-1',
        scheduleIdKey: 'schedule-1',
      },
    ];

    const items2: CheckinRequestItem[] = [
      {
        personIdKey: 'person-2',
        groupIdKey: 'group-2',
        locationIdKey: 'location-2',
        scheduleIdKey: 'schedule-2',
      },
    ];

    await offlineCheckinQueue.addToQueue(items1);
    // Small delay to ensure distinct timestamps for ordering
    await new Promise(resolve => setTimeout(resolve, 1));
    await offlineCheckinQueue.addToQueue(items2);

    const queued = await offlineCheckinQueue.getAllQueued();
    expect(queued).toHaveLength(2);
    expect(queued[0].items).toEqual(items1);
    expect(queued[1].items).toEqual(items2);
  });
});
