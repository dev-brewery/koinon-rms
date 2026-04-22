/**
 * RecentCommunications tests
 *
 * Catches:
 *  - Empty state renders when the list is empty.
 *  - Limits display to 5 rows.
 *  - Links each row to /admin/communications/{idKey}.
 *  - Type → colour (Email=blue, SMS=green, Push=purple).
 *  - Status → colour (Draft=gray, Pending=yellow, Sent=green, Failed=red).
 */
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { RecentCommunications } from '../RecentCommunications';
import type { CommunicationSummary } from '@/types';

function make(overrides: Partial<CommunicationSummary> = {}): CommunicationSummary {
  return {
    idKey: 'C1',
    subject: 'Hello',
    type: 'Email',
    status: 'Draft',
    createdDateTime: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}

function wrap(ui: React.ReactNode) {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
}

describe('RecentCommunications', () => {
  it('renders the empty state when there are no communications', () => {
    wrap(<RecentCommunications communications={[]} />);
    expect(screen.getByText(/no recent communications/i)).toBeInTheDocument();
  });

  it('limits the list to at most 5 rows', () => {
    const many: CommunicationSummary[] = Array.from({ length: 8 }, (_, i) =>
      make({ idKey: `C${i}`, subject: `Subject ${i}` }),
    );
    wrap(<RecentCommunications communications={many} />);
    expect(screen.getAllByRole('link')).toHaveLength(5);
  });

  it('links each row to the communication detail page', () => {
    wrap(<RecentCommunications communications={[make({ idKey: 'ABC' })]} />);
    expect(screen.getByRole('link')).toHaveAttribute(
      'href',
      '/admin/communications/ABC',
    );
  });

  it.each([
    ['Email', 'text-blue-700'],
    ['SMS', 'text-green-700'],
    ['Push', 'text-purple-700'],
  ] as const)('maps type=%s to %s', (type, cls) => {
    wrap(<RecentCommunications communications={[make({ type })]} />);
    expect(screen.getByText(type).className).toMatch(cls);
  });

  it.each([
    ['Draft', 'text-gray-700'],
    ['Pending', 'text-yellow-700'],
    ['Sent', 'text-green-700'],
    ['Failed', 'text-red-700'],
  ] as const)('maps status=%s to %s', (status, cls) => {
    wrap(<RecentCommunications communications={[make({ status })]} />);
    expect(screen.getByText(status).className).toMatch(cls);
  });
});
