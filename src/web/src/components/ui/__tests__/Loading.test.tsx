/**
 * Loading tests
 *
 * Catches:
 *  - Default variant is the spinner role=status.
 *  - `variant` switches to dots or skeleton (different visuals, same a11y affordance).
 *  - `aria-busy="true"` is set on the wrapper (screen readers rely on this).
 *  - Optional text renders with an aria-live region.
 */
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Loading } from '../Loading';

describe('Loading', () => {
  it('renders a spinner with role="status" by default', () => {
    render(<Loading />);
    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('wraps the variant in an aria-busy="true" container', () => {
    const { container } = render(<Loading />);
    const busy = container.querySelector('[aria-busy="true"]');
    expect(busy).not.toBeNull();
  });

  it('renders the dots variant with role="status"', () => {
    render(<Loading variant="dots" />);
    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('renders the skeleton variant with role="status"', () => {
    render(<Loading variant="skeleton" />);
    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('renders the optional text with an aria-live polite region', () => {
    render(<Loading text="Loading data..." />);
    const text = screen.getByText('Loading data...');
    expect(text).toBeInTheDocument();
    expect(text.getAttribute('aria-live')).toBe('polite');
  });

  it('does not render any text node when text is omitted', () => {
    render(<Loading />);
    expect(screen.queryByText(/Loading data/)).toBeNull();
  });

  it('respects the size prop on the spinner', () => {
    const { container } = render(<Loading size="lg" />);
    // large spinner => w-12 h-12 class.
    expect(container.querySelector('.w-12.h-12')).not.toBeNull();
  });
});
