/**
 * RecentBatches tests
 *
 * Catches:
 *  - Empty state renders "No recent batches" when list is empty.
 *  - Shows at most 5 batches (regression: dashboards should not render 100+ rows).
 *  - Status → colour mapping (Open=blue, Pending=yellow, Closed=green).
 *  - Each row links to the batch detail page using batch.idKey.
 *  - Currency formatting includes cents.
 */
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { RecentBatches } from '../RecentBatches';
import type { BatchSummary } from '@/types';

function batch(overrides: Partial<BatchSummary> = {}): BatchSummary {
  return {
    idKey: 'B1',
    name: 'Sunday',
    batchDate: '2026-01-01T00:00:00Z',
    status: 'Open',
    total: 1234.56,
    ...overrides,
  };
}

function wrap(ui: React.ReactNode) {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
}

describe('RecentBatches', () => {
  it('renders the empty state when there are no batches', () => {
    wrap(<RecentBatches batches={[]} />);
    expect(screen.getByText(/no recent batches/i)).toBeInTheDocument();
  });

  it('renders a row per batch (up to 5)', () => {
    const many: BatchSummary[] = Array.from({ length: 10 }, (_, i) =>
      batch({ idKey: `B${i}`, name: `Batch ${i}` }),
    );
    wrap(<RecentBatches batches={many} />);
    const links = screen.getAllByRole('link');
    expect(links).toHaveLength(5);
  });

  it('links each row to /admin/giving/batches/{idKey}', () => {
    wrap(<RecentBatches batches={[batch({ idKey: 'XYZ' })]} />);
    expect(screen.getByRole('link')).toHaveAttribute(
      'href',
      '/admin/giving/batches/XYZ',
    );
  });

  it('formats the total as USD with cents', () => {
    wrap(<RecentBatches batches={[batch({ total: 9.5 })]} />);
    expect(screen.getByText(/\$9\.50/)).toBeInTheDocument();
  });

  it('applies Open status → blue styling', () => {
    wrap(<RecentBatches batches={[batch({ status: 'Open' })]} />);
    const badge = screen.getByText('Open');
    expect(badge.className).toMatch(/text-blue-700/);
  });

  it('applies Pending status → yellow styling', () => {
    wrap(<RecentBatches batches={[batch({ status: 'Pending' })]} />);
    expect(screen.getByText('Pending').className).toMatch(/text-yellow-700/);
  });

  it('applies Closed status → green styling', () => {
    wrap(<RecentBatches batches={[batch({ status: 'Closed' })]} />);
    expect(screen.getByText('Closed').className).toMatch(/text-green-700/);
  });
});
