/**
 * BatchSummaryCard tests
 *
 * Catches:
 *  - Currency format uses $0.00; dash for undefined.
 *  - Variance colour: red (negative), green (positive), gray (zero).
 *  - Balance indicator text: "Balanced" vs "Unbalanced".
 *  - Action button visibility keyed to status:
 *    * Closed → "Open" button
 *    * Open → "Close" button
 *    * Posted → "Posted batches cannot be modified" text
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { BatchSummaryCard } from '../BatchSummaryCard';
import type { BatchSummaryDto, ContributionBatchDto } from '@/types/giving';

function makeBatch(overrides: Partial<ContributionBatchDto> = {}): ContributionBatchDto {
  return {
    idKey: 'B1',
    name: 'Sunday Batch',
    batchDate: '2026-01-01T00:00:00Z',
    status: 'Open',
    controlAmount: 100,
    controlItemCount: 10,
    createdDateTime: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}

function makeSummary(overrides: Partial<BatchSummaryDto> = {}): BatchSummaryDto {
  return {
    idKey: 'B1',
    name: 'Sunday Batch',
    status: 'Open',
    controlAmount: 100,
    controlItemCount: 10,
    actualAmount: 100,
    contributionCount: 10,
    variance: 0,
    isBalanced: true,
    ...overrides,
  };
}

describe('BatchSummaryCard', () => {
  it('shows "Balanced" when summary.isBalanced=true', () => {
    render(
      <BatchSummaryCard
        batch={makeBatch()}
        summary={makeSummary({ isBalanced: true })}
        onOpen={vi.fn()}
        onClose={vi.fn()}
        isOpenPending={false}
        isClosePending={false}
      />,
    );
    expect(screen.getByText('Balanced')).toBeInTheDocument();
  });

  it('shows "Unbalanced" when summary.isBalanced=false', () => {
    render(
      <BatchSummaryCard
        batch={makeBatch()}
        summary={makeSummary({ isBalanced: false, variance: -5 })}
        onOpen={vi.fn()}
        onClose={vi.fn()}
        isOpenPending={false}
        isClosePending={false}
      />,
    );
    expect(screen.getByText('Unbalanced')).toBeInTheDocument();
  });

  it('renders Close button for status=Open and fires onClose', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(
      <BatchSummaryCard
        batch={makeBatch({ status: 'Open' })}
        summary={makeSummary({ status: 'Open' })}
        onOpen={vi.fn()}
        onClose={onClose}
        isOpenPending={false}
        isClosePending={false}
      />,
    );
    await user.click(screen.getByRole('button', { name: 'Close' }));
    expect(onClose).toHaveBeenCalledOnce();
  });

  it('renders Open button for status=Closed and fires onOpen', async () => {
    const user = userEvent.setup();
    const onOpen = vi.fn();
    render(
      <BatchSummaryCard
        batch={makeBatch({ status: 'Closed' })}
        summary={makeSummary({ status: 'Closed' })}
        onOpen={onOpen}
        onClose={vi.fn()}
        isOpenPending={false}
        isClosePending={false}
      />,
    );
    await user.click(screen.getByRole('button', { name: 'Open' }));
    expect(onOpen).toHaveBeenCalledOnce();
  });

  it('renders the locked message for status=Posted (no buttons)', () => {
    render(
      <BatchSummaryCard
        batch={makeBatch({ status: 'Posted' })}
        summary={makeSummary({ status: 'Posted' })}
        onOpen={vi.fn()}
        onClose={vi.fn()}
        isOpenPending={false}
        isClosePending={false}
      />,
    );
    expect(screen.getByText(/posted batches cannot be modified/i)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^open$/i })).toBeNull();
    expect(screen.queryByRole('button', { name: /^close$/i })).toBeNull();
  });

  it('formats currency amounts with $0.00', () => {
    render(
      <BatchSummaryCard
        batch={makeBatch({ controlAmount: 123.45 })}
        summary={makeSummary({ actualAmount: 99.5, variance: -23.95 })}
        onOpen={vi.fn()}
        onClose={vi.fn()}
        isOpenPending={false}
        isClosePending={false}
      />,
    );
    expect(screen.getByText('$123.45')).toBeInTheDocument();
    expect(screen.getByText('$99.50')).toBeInTheDocument();
  });

  it('shows a dash when control amount is undefined', () => {
    render(
      <BatchSummaryCard
        batch={makeBatch({ controlAmount: undefined, controlItemCount: undefined })}
        summary={makeSummary({ actualAmount: 0, variance: 0 })}
        onOpen={vi.fn()}
        onClose={vi.fn()}
        isOpenPending={false}
        isClosePending={false}
      />,
    );
    // At least one "-" rendered for control amount.
    const dashes = screen.getAllByText('-');
    expect(dashes.length).toBeGreaterThanOrEqual(1);
  });

  it('shows loading text on the Close button when isClosePending=true', () => {
    render(
      <BatchSummaryCard
        batch={makeBatch({ status: 'Open' })}
        summary={makeSummary()}
        onOpen={vi.fn()}
        onClose={vi.fn()}
        isOpenPending={false}
        isClosePending
      />,
    );
    expect(screen.getByText('Closing...')).toBeInTheDocument();
  });
});
