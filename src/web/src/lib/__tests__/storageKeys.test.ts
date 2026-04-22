/**
 * Tests for lib/storageKeys - ensures constants don't silently drift
 */

import { describe, it, expect } from 'vitest';
import { STORAGE_KEYS } from '../storageKeys';

describe('STORAGE_KEYS', () => {
  it('exposes a stable selected-location key name (LocationPicker/RosterPage rely on this string)', () => {
    expect(STORAGE_KEYS.SELECTED_LOCATION_ID_KEY).toBe('selectedLocationIdKey');
  });

  it('is a readonly-style object (as const - object structure is stable)', () => {
    // At minimum, all values must be non-empty strings
    for (const v of Object.values(STORAGE_KEYS)) {
      expect(typeof v).toBe('string');
      expect(v.length).toBeGreaterThan(0);
    }
  });
});
