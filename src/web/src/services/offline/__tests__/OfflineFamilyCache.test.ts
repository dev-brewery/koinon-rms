/**
 * OfflineFamilyCache tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { offlineFamilyCache } from '../OfflineFamilyCache';
import type { CheckinFamilyDto } from '@/services/api/types';

const makeFamily = (id: string): CheckinFamilyDto =>
  ({
    idKey: id,
    familyName: `Family ${id}`,
    members: [],
  }) as unknown as CheckinFamilyDto;

describe('OfflineFamilyCache', () => {
  beforeEach(async () => {
    await offlineFamilyCache.clearCache();
  });

  it('returns null when nothing cached for a query', async () => {
    const result = await offlineFamilyCache.getCachedResults('555-0001');
    expect(result).toBeNull();
  });

  it('caches and retrieves family results for a query', async () => {
    const fams = [makeFamily('a'), makeFamily('b')];
    await offlineFamilyCache.cacheResults('555-0001', fams);
    const result = await offlineFamilyCache.getCachedResults('555-0001');
    expect(result).toEqual(fams);
  });

  it('does not leak results across different queries', async () => {
    await offlineFamilyCache.cacheResults('555-0001', [makeFamily('a')]);
    const wrong = await offlineFamilyCache.getCachedResults('555-9999');
    expect(wrong).toBeNull();
  });

  it('overwrites when caching the same query twice (put semantics)', async () => {
    await offlineFamilyCache.cacheResults('q', [makeFamily('a')]);
    await offlineFamilyCache.cacheResults('q', [makeFamily('b'), makeFamily('c')]);
    const result = await offlineFamilyCache.getCachedResults('q');
    expect(result).toHaveLength(2);
    expect(result?.[0].idKey).toBe('b');
  });

  it('stores and returns cache timestamp for staleness checks', async () => {
    const before = Date.now();
    await offlineFamilyCache.cacheResults('q', [makeFamily('a')]);
    const ts = await offlineFamilyCache.getCacheTimestamp('q');
    expect(ts).not.toBeNull();
    if (ts !== null) {
      expect(ts).toBeGreaterThanOrEqual(before);
      expect(ts).toBeLessThanOrEqual(Date.now());
    }
  });

  it('returns null timestamp for missing queries', async () => {
    const ts = await offlineFamilyCache.getCacheTimestamp('missing');
    expect(ts).toBeNull();
  });

  it('clearCache removes all entries', async () => {
    await offlineFamilyCache.cacheResults('q1', [makeFamily('a')]);
    await offlineFamilyCache.cacheResults('q2', [makeFamily('b')]);
    await offlineFamilyCache.clearCache();
    expect(await offlineFamilyCache.getCachedResults('q1')).toBeNull();
    expect(await offlineFamilyCache.getCachedResults('q2')).toBeNull();
  });
});
