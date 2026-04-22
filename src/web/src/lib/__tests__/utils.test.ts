/**
 * Tests for lib/utils - cn, formatDate, formatDateTime, debounce
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { cn, formatDate, formatDateTime, debounce } from '../utils';

describe('cn', () => {
  it('joins truthy class names', () => {
    expect(cn('a', 'b', 'c')).toBe('a b c');
  });

  it('ignores falsy values', () => {
    expect(cn('a', false && 'b', null, undefined, '', 'c')).toBe('a c');
  });

  it('merges conflicting tailwind classes, keeping the last', () => {
    expect(cn('p-2', 'p-4')).toBe('p-4');
    expect(cn('text-red-500', 'text-blue-500')).toBe('text-blue-500');
  });

  it('supports conditional object syntax from clsx', () => {
    expect(cn('base', { active: true, hidden: false })).toBe('base active');
  });
});

describe('formatDate', () => {
  it('formats a Date object', () => {
    const d = new Date('2024-01-15T12:00:00Z');
    const formatted = formatDate(d);
    expect(formatted).toMatch(/January/);
    expect(formatted).toMatch(/2024/);
  });

  it('accepts an ISO string', () => {
    const formatted = formatDate('2024-03-20T00:00:00Z');
    expect(formatted).toMatch(/March/);
  });
});

describe('formatDateTime', () => {
  it('formats with time components', () => {
    const formatted = formatDateTime('2024-01-15T14:30:00Z');
    expect(formatted).toMatch(/January/);
    // Has a minute (two-digit)
    expect(formatted).toMatch(/\d{1,2}:\d{2}/);
  });
});

describe('debounce', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('delays the call by the given ms', () => {
    const fn = vi.fn();
    const debounced = debounce(fn, 100);

    debounced('a');
    expect(fn).not.toHaveBeenCalled();
    vi.advanceTimersByTime(100);
    expect(fn).toHaveBeenCalledWith('a');
  });

  it('only invokes once for rapid successive calls', () => {
    const fn = vi.fn();
    const debounced = debounce(fn, 100);

    debounced('a');
    debounced('b');
    debounced('c');
    vi.advanceTimersByTime(100);

    expect(fn).toHaveBeenCalledTimes(1);
    expect(fn).toHaveBeenCalledWith('c');
  });
});
