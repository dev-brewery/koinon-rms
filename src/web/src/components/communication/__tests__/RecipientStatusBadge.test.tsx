/**
 * RecipientStatusBadge tests
 *
 * Catches status → colour mapping regressions (Pending/Delivered/Failed/Opened)
 * and the unknown-status fallback.
 */
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { RecipientStatusBadge } from '../RecipientStatusBadge';

const cases: Array<[string, string]> = [
  ['Pending', 'bg-blue-100'],
  ['Delivered', 'bg-green-100'],
  ['Failed', 'bg-red-100'],
  ['Opened', 'bg-purple-100'],
];

describe('RecipientStatusBadge', () => {
  it.each(cases)('maps %s status to the expected colour class', (status, cls) => {
    const { container } = render(<RecipientStatusBadge status={status} />);
    expect(container.firstChild).toHaveClass(cls);
    expect(screen.getByText(status)).toBeInTheDocument();
  });

  it('falls back to gray styling for unknown statuses', () => {
    const { container } = render(<RecipientStatusBadge status="Unknown" />);
    expect(container.firstChild).toHaveClass('bg-gray-100');
  });
});
