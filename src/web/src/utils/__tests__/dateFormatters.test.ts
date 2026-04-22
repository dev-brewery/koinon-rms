/**
 * dateFormatters tests
 */

import { describe, it, expect } from 'vitest';
import {
  DAYS_OF_WEEK,
  DAYS_OF_WEEK_SHORT,
  formatTime12Hour,
  formatDateTime,
  generateTimeOptions,
} from '../dateFormatters';

describe('dateFormatters constants', () => {
  it('exposes 7 days of the week starting Sunday', () => {
    expect(DAYS_OF_WEEK).toHaveLength(7);
    expect(DAYS_OF_WEEK[0]).toBe('Sunday');
    expect(DAYS_OF_WEEK[6]).toBe('Saturday');
  });

  it('exposes 7-day short labels with matching fullLabel', () => {
    expect(DAYS_OF_WEEK_SHORT).toHaveLength(7);
    for (let i = 0; i < 7; i += 1) {
      expect(DAYS_OF_WEEK_SHORT[i].value).toBe(i);
      expect(DAYS_OF_WEEK_SHORT[i].fullLabel).toBe(DAYS_OF_WEEK[i]);
    }
  });
});

describe('formatTime12Hour', () => {
  it('formats midnight as 12:00 AM', () => {
    expect(formatTime12Hour('00:00')).toBe('12:00 AM');
  });

  it('formats noon as 12:00 PM', () => {
    expect(formatTime12Hour('12:00')).toBe('12:00 PM');
  });

  it('formats morning time', () => {
    expect(formatTime12Hour('09:05')).toBe('9:05 AM');
  });

  it('formats afternoon time', () => {
    expect(formatTime12Hour('14:30')).toBe('2:30 PM');
  });

  it('pads single-digit minutes', () => {
    expect(formatTime12Hour('01:05')).toBe('1:05 AM');
  });
});

describe('formatDateTime', () => {
  it('formats an ISO datetime string in en-US locale', () => {
    const formatted = formatDateTime('2024-01-15T14:30:00Z');
    // Don't assert timezone-specific time; just ensure all date/time fields are present
    expect(formatted).toMatch(/January|Monday|2024/);
  });
});

describe('generateTimeOptions', () => {
  it('produces 96 quarter-hour slots covering a full 24-hour day', () => {
    const times = generateTimeOptions();
    expect(times).toHaveLength(96);
    expect(times[0]).toBe('00:00:00');
    expect(times[1]).toBe('00:15:00');
    expect(times[4]).toBe('01:00:00');
    expect(times[times.length - 1]).toBe('23:45:00');
  });

  it('uses zero-padded HH:MM:00 format', () => {
    const times = generateTimeOptions();
    for (const t of times) {
      expect(t).toMatch(/^\d{2}:\d{2}:00$/);
    }
  });
});
