/**
 * OfflineIndicator tests
 *
 * Catches:
 *  - Renders nothing when online and never was offline.
 *  - Renders offline banner when navigator.onLine goes false.
 *  - Renders "Back online" banner after going offline then online.
 *  - role="status" + aria-live="polite" preserved (screen readers rely on this).
 */
import { act, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { OfflineIndicator } from '../OfflineIndicator';

function setOnline(value: boolean) {
  Object.defineProperty(navigator, 'onLine', {
    configurable: true,
    get: () => value,
  });
}

beforeEach(() => {
  setOnline(true);
});

afterEach(() => {
  vi.useRealTimers();
});

describe('OfflineIndicator', () => {
  it('renders nothing when online and never went offline', () => {
    setOnline(true);
    render(<OfflineIndicator />);
    expect(screen.queryByTestId('offline-indicator')).toBeNull();
  });

  it('renders offline banner when the browser goes offline', () => {
    setOnline(true);
    render(<OfflineIndicator />);

    act(() => {
      setOnline(false);
      window.dispatchEvent(new Event('offline'));
    });

    const banner = screen.getByTestId('offline-indicator');
    expect(banner).toBeInTheDocument();
    expect(banner.getAttribute('role')).toBe('status');
    expect(banner.getAttribute('aria-live')).toBe('polite');
    expect(banner.textContent).toMatch(/offline/i);
  });

  it('shows "Back online" banner after recovering from offline', () => {
    setOnline(true);
    render(<OfflineIndicator />);

    // Go offline first.
    act(() => {
      setOnline(false);
      window.dispatchEvent(new Event('offline'));
    });
    expect(screen.getByTestId('offline-indicator').textContent).toMatch(/offline/i);

    // Now come back online.
    act(() => {
      setOnline(true);
      window.dispatchEvent(new Event('online'));
    });

    const banner = screen.getByTestId('offline-indicator');
    expect(banner.textContent).toMatch(/back online/i);
  });

  it('removes the "Back online" banner after the 3-second timeout', () => {
    vi.useFakeTimers();
    setOnline(true);
    render(<OfflineIndicator />);

    act(() => {
      setOnline(false);
      window.dispatchEvent(new Event('offline'));
    });

    act(() => {
      setOnline(true);
      window.dispatchEvent(new Event('online'));
    });

    expect(screen.getByTestId('offline-indicator').textContent).toMatch(/back online/i);

    act(() => {
      vi.advanceTimersByTime(3100);
    });

    expect(screen.queryByTestId('offline-indicator')).toBeNull();
  });
});
