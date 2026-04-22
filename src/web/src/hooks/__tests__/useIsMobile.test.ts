/**
 * useIsMobile tests
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useIsMobile } from '../useIsMobile';

type MqlListener = (e: { matches: boolean }) => void;

describe('useIsMobile', () => {
  const originalInnerWidth = window.innerWidth;
  const originalMatchMedia = window.matchMedia;

  let listeners: MqlListener[] = [];
  let mqlMatches = false;

  beforeEach(() => {
    listeners = [];
    mqlMatches = false;
    window.matchMedia = vi.fn().mockImplementation(() => ({
      matches: mqlMatches,
      media: '',
      onchange: null,
      addEventListener: vi.fn((_: string, listener: MqlListener) => {
        listeners.push(listener);
      }),
      removeEventListener: vi.fn((_: string, listener: MqlListener) => {
        listeners = listeners.filter((l) => l !== listener);
      }),
      addListener: vi.fn(),
      removeListener: vi.fn(),
      dispatchEvent: vi.fn(),
    })) as unknown as typeof window.matchMedia;
  });

  afterEach(() => {
    Object.defineProperty(window, 'innerWidth', {
      configurable: true,
      value: originalInnerWidth,
    });
    window.matchMedia = originalMatchMedia;
  });

  it('initializes based on window.innerWidth (desktop)', () => {
    Object.defineProperty(window, 'innerWidth', {
      configurable: true,
      value: 1920,
    });
    mqlMatches = false;
    const { result } = renderHook(() => useIsMobile(1024));
    expect(result.current).toBe(false);
  });

  it('initializes true when innerWidth below breakpoint', () => {
    Object.defineProperty(window, 'innerWidth', {
      configurable: true,
      value: 800,
    });
    mqlMatches = true;
    const { result } = renderHook(() => useIsMobile(1024));
    expect(result.current).toBe(true);
  });

  it('reacts to matchMedia change events', () => {
    Object.defineProperty(window, 'innerWidth', {
      configurable: true,
      value: 1920,
    });
    mqlMatches = false;
    const { result } = renderHook(() => useIsMobile(1024));
    expect(result.current).toBe(false);

    act(() => {
      listeners.forEach((l) => l({ matches: true }));
    });
    expect(result.current).toBe(true);

    act(() => {
      listeners.forEach((l) => l({ matches: false }));
    });
    expect(result.current).toBe(false);
  });
});
