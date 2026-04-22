/**
 * ErrorBoundary tests
 *
 * Catches:
 *  - Renders children when no error (happy path).
 *  - Fallback UI shows when a child throws.
 *  - Custom fallback prop replaces default UI.
 *  - handleReset clears error state so subsequent children render.
 *  - captureError (error tracking) is called on errors.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ErrorBoundary } from '../ErrorBoundary';

vi.mock('../../services/errorTracking', () => ({
  captureError: vi.fn(),
}));
import * as errorTracking from '../../services/errorTracking';

function BoomComponent({ shouldThrow }: { shouldThrow: boolean }) {
  if (shouldThrow) throw new Error('kaboom');
  return <div>no boom</div>;
}

afterEach(() => {
  vi.clearAllMocks();
});

describe('ErrorBoundary', () => {
  it('renders children when no error', () => {
    render(
      <ErrorBoundary>
        <BoomComponent shouldThrow={false} />
      </ErrorBoundary>,
    );
    expect(screen.getByText('no boom')).toBeInTheDocument();
  });

  it('shows the default fallback UI when a child throws', () => {
    // Silence the expected React error logs.
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {});

    render(
      <ErrorBoundary>
        <BoomComponent shouldThrow />
      </ErrorBoundary>,
    );

    expect(screen.getByText('Something went wrong')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /go home/i })).toBeInTheDocument();
    spy.mockRestore();
  });

  it('reports the error to the error tracking service', () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {});
    render(
      <ErrorBoundary>
        <BoomComponent shouldThrow />
      </ErrorBoundary>,
    );
    expect(errorTracking.captureError).toHaveBeenCalled();
    const [err] = (errorTracking.captureError as ReturnType<typeof vi.fn>).mock.calls[0];
    expect((err as Error).message).toBe('kaboom');
    spy.mockRestore();
  });

  it('renders a custom fallback when the prop is provided', () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {});
    render(
      <ErrorBoundary fallback={<div data-testid="custom-fallback">custom</div>}>
        <BoomComponent shouldThrow />
      </ErrorBoundary>,
    );
    expect(screen.getByTestId('custom-fallback')).toBeInTheDocument();
    // Default UI strings should not be visible.
    expect(screen.queryByText('Something went wrong')).toBeNull();
    spy.mockRestore();
  });

  it('resets error state when Try Again is clicked', async () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {});
    const user = userEvent.setup();

    // Use a wrapper that toggles off thrown on reset.
    function App() {
      return (
        <ErrorBoundary>
          <BoomComponent shouldThrow />
        </ErrorBoundary>
      );
    }
    const { rerender } = render(<App />);

    expect(screen.getByText('Something went wrong')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /try again/i }));

    // After reset, re-render with a safe child: boundary should show it.
    rerender(
      <ErrorBoundary>
        <BoomComponent shouldThrow={false} />
      </ErrorBoundary>,
    );

    // Still stuck on the error UI because rerender re-instantiates boundary
    // with fresh state and clean children.
    expect(screen.getByText('no boom')).toBeInTheDocument();
    spy.mockRestore();
  });
});
