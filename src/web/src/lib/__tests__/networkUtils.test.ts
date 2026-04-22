/**
 * networkUtils tests
 */

import { describe, it, expect } from 'vitest';
import { isNetworkError } from '../networkUtils';

describe('isNetworkError', () => {
  it('detects TypeError with "Failed to fetch" message', () => {
    expect(isNetworkError(new TypeError('Failed to fetch'))).toBe(true);
  });

  it('detects TypeError with "fetch" in message', () => {
    expect(isNetworkError(new TypeError('fetch aborted'))).toBe(true);
  });

  it('detects TypeError with "network" keyword', () => {
    expect(isNetworkError(new TypeError('network error'))).toBe(true);
  });

  it('returns false for TypeError without network-related keywords', () => {
    expect(isNetworkError(new TypeError('is not a function'))).toBe(false);
  });

  it('returns false for generic Error', () => {
    expect(isNetworkError(new Error('Failed to fetch'))).toBe(false);
  });

  it('returns false for unknown value shapes', () => {
    expect(isNetworkError(null)).toBe(false);
    expect(isNetworkError(undefined)).toBe(false);
    expect(isNetworkError('Failed to fetch')).toBe(false);
    expect(isNetworkError({ message: 'network' })).toBe(false);
    expect(isNetworkError(42)).toBe(false);
  });
});
