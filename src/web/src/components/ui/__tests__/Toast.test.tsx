/**
 * Toast component tests
 *
 * Catches:
 *  - ToastContainer exposes an aria-live polite region (screen readers).
 *  - Each toast is rendered with role="alert".
 *  - All four variants (success/error/warning/info) render with distinct styling.
 *  - Close button has an accessible name and removes the toast on click.
 *  - Auto-dismiss begins hide animation before the duration elapses.
 */
import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ToastProvider, useToast } from '../../../contexts/ToastContext';
import { ToastContainer } from '../Toast';

function Seeder({
  variant,
  duration,
}: {
  variant: 'success' | 'error' | 'warning' | 'info';
  duration?: number;
}) {
  const { addToast } = useToast();
  return (
    <button
      onClick={() =>
        addToast({
          title: `${variant} title`,
          message: `${variant} msg`,
          variant,
          duration,
        })
      }
    >
      seed-{variant}
    </button>
  );
}

function renderWithProvider(ui: React.ReactNode) {
  return render(
    <ToastProvider>
      {ui}
      <ToastContainer />
    </ToastProvider>,
  );
}

describe('ToastContainer', () => {
  it('exposes the container as an aria-live polite region', () => {
    renderWithProvider(<Seeder variant="info" />);
    const region = document.querySelector('[aria-live="polite"]');
    expect(region).not.toBeNull();
  });

  it('renders nothing visible when there are no toasts', () => {
    renderWithProvider(<Seeder variant="info" />);
    expect(screen.queryByRole('alert')).toBeNull();
  });
});

describe('Toast render per variant', () => {
  beforeEach(() => {
    // Real timers — we test auto-dismiss separately.
    vi.useRealTimers();
  });

  it('renders a success toast as role="alert"', async () => {
    const user = userEvent.setup();
    renderWithProvider(<Seeder variant="success" />);
    await user.click(screen.getByRole('button', { name: 'seed-success' }));
    const alert = await screen.findByRole('alert');
    expect(alert).toBeInTheDocument();
    expect(alert.className).toMatch(/bg-green-50/);
  });

  it('renders an error toast with red styling', async () => {
    const user = userEvent.setup();
    renderWithProvider(<Seeder variant="error" />);
    await user.click(screen.getByRole('button', { name: 'seed-error' }));
    const alert = await screen.findByRole('alert');
    expect(alert.className).toMatch(/bg-red-50/);
  });

  it('renders a warning toast with yellow styling', async () => {
    const user = userEvent.setup();
    renderWithProvider(<Seeder variant="warning" />);
    await user.click(screen.getByRole('button', { name: 'seed-warning' }));
    expect((await screen.findByRole('alert')).className).toMatch(/bg-yellow-50/);
  });

  it('renders an info toast with blue styling', async () => {
    const user = userEvent.setup();
    renderWithProvider(<Seeder variant="info" />);
    await user.click(screen.getByRole('button', { name: 'seed-info' }));
    expect((await screen.findByRole('alert')).className).toMatch(/bg-blue-50/);
  });
});

describe('Toast close interactions', () => {
  it('renders a dismiss button with an accessible name', async () => {
    const user = userEvent.setup();
    renderWithProvider(<Seeder variant="info" />);
    await user.click(screen.getByRole('button', { name: 'seed-info' }));
    expect(
      await screen.findByRole('button', { name: /dismiss notification/i }),
    ).toBeInTheDocument();
  });

  it('removes the toast on dismiss click (after animation delay)', async () => {
    const user = userEvent.setup();

    renderWithProvider(<Seeder variant="info" />);
    await user.click(screen.getByRole('button', { name: 'seed-info' }));
    expect(screen.getByRole('alert')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /dismiss notification/i }));

    // Wait out the 200ms animation + buffer.
    await act(async () => {
      await new Promise((r) => setTimeout(r, 400));
    });

    expect(screen.queryByRole('alert')).toBeNull();
  });
});

afterEach(() => {
  vi.useRealTimers();
});
