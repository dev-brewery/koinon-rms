/**
 * checkinHelpers tests
 */

import { describe, it, expect } from 'vitest';
import {
  createSelectionKey,
  getTotalActivitiesCount,
} from '../checkinHelpers';
import type { OpportunitySelection } from '@/components/checkin';

describe('createSelectionKey', () => {
  it('joins the three ids with a pipe separator', () => {
    expect(createSelectionKey('g1', 'l1', 's1')).toBe('g1|l1|s1');
  });

  it('is deterministic for a given triple', () => {
    expect(createSelectionKey('a', 'b', 'c')).toBe(
      createSelectionKey('a', 'b', 'c')
    );
  });

  it('differentiates different orderings / values', () => {
    expect(createSelectionKey('a', 'b', 'c')).not.toBe(
      createSelectionKey('c', 'b', 'a')
    );
  });
});

describe('getTotalActivitiesCount', () => {
  const selection = (n: number): OpportunitySelection[] =>
    Array.from({ length: n }, (_, i) => ({
      groupId: `g${i}`,
      locationId: `l${i}`,
      scheduleId: `s${i}`,
      groupName: `G${i}`,
      locationName: `L${i}`,
      scheduleName: `S${i}`,
      startTime: '09:00:00',
    }));

  it('returns 0 for empty map', () => {
    expect(getTotalActivitiesCount(new Map())).toBe(0);
  });

  it('sums array lengths across people', () => {
    const map = new Map<string, OpportunitySelection[]>();
    map.set('p1', selection(2));
    map.set('p2', selection(3));
    expect(getTotalActivitiesCount(map)).toBe(5);
  });

  it('ignores non-array values gracefully', () => {
    const map = new Map<string, OpportunitySelection[]>();
    map.set('p1', selection(2));
    // simulate a bad entry without crashing
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    map.set('p2', null as any);
    expect(getTotalActivitiesCount(map)).toBe(2);
  });
});
