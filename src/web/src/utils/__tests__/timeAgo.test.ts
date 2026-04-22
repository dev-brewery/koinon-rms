/**
 * timeAgo tests
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { formatTimeAgo } from '../timeAgo';

describe('formatTimeAgo', () => {
  const NOW = new Date('2026-04-22T12:00:00Z');

  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(NOW);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('returns "just now" for <60s ago', () => {
    const recent = new Date(NOW.getTime() - 30_000);
    expect(formatTimeAgo(recent)).toBe('just now');
  });

  it('formats minutes (singular)', () => {
    const t = new Date(NOW.getTime() - 60 * 1000);
    expect(formatTimeAgo(t)).toBe('1 minute ago');
  });

  it('formats minutes (plural)', () => {
    const t = new Date(NOW.getTime() - 5 * 60 * 1000);
    expect(formatTimeAgo(t)).toBe('5 minutes ago');
  });

  it('formats hours (singular/plural)', () => {
    const oneHour = new Date(NOW.getTime() - 60 * 60 * 1000);
    expect(formatTimeAgo(oneHour)).toBe('1 hour ago');
    const threeHours = new Date(NOW.getTime() - 3 * 60 * 60 * 1000);
    expect(formatTimeAgo(threeHours)).toBe('3 hours ago');
  });

  it('formats days', () => {
    const twoDays = new Date(NOW.getTime() - 2 * 24 * 60 * 60 * 1000);
    expect(formatTimeAgo(twoDays)).toBe('2 days ago');
    const oneDay = new Date(NOW.getTime() - 24 * 60 * 60 * 1000);
    expect(formatTimeAgo(oneDay)).toBe('1 day ago');
  });

  it('formats weeks', () => {
    const twoWeeks = new Date(NOW.getTime() - 14 * 24 * 60 * 60 * 1000);
    expect(formatTimeAgo(twoWeeks)).toBe('2 weeks ago');
  });

  it('formats months', () => {
    const threeMonths = new Date(NOW.getTime() - 95 * 24 * 60 * 60 * 1000);
    expect(formatTimeAgo(threeMonths)).toBe('3 months ago');
  });

  it('formats years', () => {
    const twoYears = new Date(NOW.getTime() - 2 * 365 * 24 * 60 * 60 * 1000);
    expect(formatTimeAgo(twoYears)).toBe('2 years ago');
  });

  it('accepts ISO strings as well as Date objects', () => {
    const iso = new Date(NOW.getTime() - 60 * 60 * 1000).toISOString();
    expect(formatTimeAgo(iso)).toBe('1 hour ago');
  });
});
