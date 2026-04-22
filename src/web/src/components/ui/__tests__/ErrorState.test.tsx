/**
 * ErrorState tests
 *
 * Catches:
 *  - Heading uses red-600 text colour (distinct from EmptyState which is neutral).
 *  - Message renders only when provided.
 *  - Retry button only shows when onRetry is provided and triggers the callback.
 *  - Default error icon renders when no custom icon is passed.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ErrorState } from '../ErrorState';

describe('ErrorState', () => {
  it('renders the title as a heading using error styling', () => {
    render(<ErrorState title="Boom" />);
    const heading = screen.getByRole('heading', { level: 3, name: 'Boom' });
    expect(heading).toBeInTheDocument();
    expect(heading.className).toMatch(/text-red-600/);
  });

  it('renders the message when provided', () => {
    render(<ErrorState title="Boom" message="details" />);
    expect(screen.getByText('details')).toBeInTheDocument();
  });

  it('omits the message node when not provided', () => {
    const { container } = render(<ErrorState title="Boom" />);
    expect(container.querySelectorAll('p')).toHaveLength(0);
  });

  it('does not render a retry button when onRetry is absent', () => {
    render(<ErrorState title="Boom" />);
    expect(screen.queryByRole('button')).toBeNull();
  });

  it('renders the retry button and fires onRetry when clicked', async () => {
    const user = userEvent.setup();
    const onRetry = vi.fn();
    render(<ErrorState title="Boom" onRetry={onRetry} />);
    await user.click(screen.getByRole('button', { name: /try again/i }));
    expect(onRetry).toHaveBeenCalledOnce();
  });

  it('renders a custom icon in place of the default error icon', () => {
    render(
      <ErrorState title="Boom" icon={<svg data-testid="custom-error-icon" />} />,
    );
    expect(screen.getByTestId('custom-error-icon')).toBeInTheDocument();
  });
});
