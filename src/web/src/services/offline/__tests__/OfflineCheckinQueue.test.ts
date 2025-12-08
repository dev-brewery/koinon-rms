/**
 * Tests for OfflineCheckinQueue
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { offlineCheckinQueue } from '../OfflineCheckinQueue';
import type { RecordAttendanceRequest } from '@/services/api/types';

describe('OfflineCheckinQueue', () => {
  beforeEach(async () => {
    // Clear queue before each test
    await offlineCheckinQueue.clearQueue();
  });

  it('should add check-in to queue', async () => {
    const request: RecordAttendanceRequest = {
      checkins: [
        {
          personIdKey: 'test-person',
          groupIdKey: 'test-group',
          locationIdKey: 'test-location',
          scheduleIdKey: 'test-schedule',
        },
      ],
    };

    const id = await offlineCheckinQueue.addToQueue(request);
    expect(id).toBeTruthy();

    const count = await offlineCheckinQueue.getQueuedCount();
    expect(count).toBe(1);
  });

  it('should retrieve queued item', async () => {
    const request: RecordAttendanceRequest = {
      checkins: [
        {
          personIdKey: 'test-person',
          groupIdKey: 'test-group',
          locationIdKey: 'test-location',
          scheduleIdKey: 'test-schedule',
        },
      ],
    };

    const id = await offlineCheckinQueue.addToQueue(request);
    const item = await offlineCheckinQueue.getQueuedItem(id);

    expect(item).toBeTruthy();
    expect(item?.id).toBe(id);
    expect(item?.request).toEqual(request);
    expect(item?.status).toBe('pending');
    expect(item?.attempts).toBe(0);
  });

  it('should clear queue', async () => {
    const request: RecordAttendanceRequest = {
      checkins: [
        {
          personIdKey: 'test-person',
          groupIdKey: 'test-group',
          locationIdKey: 'test-location',
          scheduleIdKey: 'test-schedule',
        },
      ],
    };

    await offlineCheckinQueue.addToQueue(request);
    await offlineCheckinQueue.addToQueue(request);

    let count = await offlineCheckinQueue.getQueuedCount();
    expect(count).toBe(2);

    await offlineCheckinQueue.clearQueue();
    count = await offlineCheckinQueue.getQueuedCount();
    expect(count).toBe(0);
  });

  it('should remove specific item from queue', async () => {
    const request: RecordAttendanceRequest = {
      checkins: [
        {
          personIdKey: 'test-person',
          groupIdKey: 'test-group',
          locationIdKey: 'test-location',
          scheduleIdKey: 'test-schedule',
        },
      ],
    };

    const id1 = await offlineCheckinQueue.addToQueue(request);
    const id2 = await offlineCheckinQueue.addToQueue(request);

    await offlineCheckinQueue.removeFromQueue(id1);

    const count = await offlineCheckinQueue.getQueuedCount();
    expect(count).toBe(1);

    const item = await offlineCheckinQueue.getQueuedItem(id2);
    expect(item).toBeTruthy();
  });

  it('should handle multiple queued items', async () => {
    const request1: RecordAttendanceRequest = {
      checkins: [
        {
          personIdKey: 'person-1',
          groupIdKey: 'group-1',
          locationIdKey: 'location-1',
          scheduleIdKey: 'schedule-1',
        },
      ],
    };

    const request2: RecordAttendanceRequest = {
      checkins: [
        {
          personIdKey: 'person-2',
          groupIdKey: 'group-2',
          locationIdKey: 'location-2',
          scheduleIdKey: 'schedule-2',
        },
      ],
    };

    await offlineCheckinQueue.addToQueue(request1);
    // Small delay to ensure distinct timestamps for ordering
    await new Promise(resolve => setTimeout(resolve, 1));
    await offlineCheckinQueue.addToQueue(request2);

    const queued = await offlineCheckinQueue.getAllQueued();
    expect(queued).toHaveLength(2);
    expect(queued[0].request).toEqual(request1);
    expect(queued[1].request).toEqual(request2);
  });
});
