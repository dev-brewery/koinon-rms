/**
 * BatchStatusBadge tests
 *
 * Catches:
 *  - Status → colour mapping (Open=green, Closed=gray, Posted=blue).
 *  - Unknown status falls back to the gray default.
 *  - Status text renders.
 */
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { BatchStatusBadge } from '../BatchStatusBadge';

describe('BatchStatusBadge', () => {
  it('maps Open status to green styling', () => {
    const { container } = render(<BatchStatusBadge status="Open" />);
    expect(container.firstChild).toHaveClass('bg-green-100');
    expect(screen.getByText('Open')).toBeInTheDocument();
  });

  it('maps Closed status to gray styling', () => {
    const { container } = render(<BatchStatusBadge status="Closed" />);
    expect(container.firstChild).toHaveClass('bg-gray-100');
  });

  it('maps Posted status to blue styling', () => {
    const { container } = render(<BatchStatusBadge status="Posted" />);
    expect(container.firstChild).toHaveClass('bg-blue-100');
  });

  it('falls back to gray for unknown statuses', () => {
    const { container } = render(<BatchStatusBadge status="Mystery" />);
    expect(container.firstChild).toHaveClass('bg-gray-100');
  });
});
