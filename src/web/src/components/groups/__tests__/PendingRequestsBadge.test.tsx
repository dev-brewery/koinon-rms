/**
 * PendingRequestsBadge tests
 *
 * Catches:
 *  - Hidden when count=0 (silent state).
 *  - Renders the number for reasonable counts.
 *  - Caps display at "99+" for overflow counts.
 */
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { PendingRequestsBadge } from '../PendingRequestsBadge';

describe('PendingRequestsBadge', () => {
  it('renders nothing when count is 0', () => {
    const { container } = render(<PendingRequestsBadge count={0} />);
    expect(container.firstChild).toBeNull();
  });

  it('displays the numeric count when between 1 and 99', () => {
    render(<PendingRequestsBadge count={7} />);
    expect(screen.getByText('7')).toBeInTheDocument();
  });

  it('caps display at 99+ for very large counts', () => {
    render(<PendingRequestsBadge count={150} />);
    expect(screen.getByText('99+')).toBeInTheDocument();
    expect(screen.queryByText('150')).toBeNull();
  });

  it('displays exactly "99" at the boundary', () => {
    render(<PendingRequestsBadge count={99} />);
    expect(screen.getByText('99')).toBeInTheDocument();
  });
});
