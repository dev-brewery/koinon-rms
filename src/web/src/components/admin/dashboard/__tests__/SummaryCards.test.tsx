/**
 * GivingSummaryCard + CommunicationsSummaryCard tests
 *
 * Catches:
 *  - Currency formatting uses USD with no fractional digits
 *    (regression category: floating-point display artefacts like "$12345.67").
 *  - Zero values render correctly (fallback safety).
 *  - Both MTD and YTD totals appear.
 *  - Communications card renders pending + sent counts.
 */
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { GivingSummaryCard } from '../GivingSummaryCard';
import { CommunicationsSummaryCard } from '../CommunicationsSummaryCard';

describe('GivingSummaryCard', () => {
  it('renders month-to-date and year-to-date totals as USD currency', () => {
    render(<GivingSummaryCard monthToDateTotal={12345} yearToDateTotal={98765} />);

    expect(screen.getByText('Month to Date')).toBeInTheDocument();
    expect(screen.getByText('Year to Date')).toBeInTheDocument();
    expect(screen.getByText('$12,345')).toBeInTheDocument();
    expect(screen.getByText('$98,765')).toBeInTheDocument();
  });

  it('handles zero values', () => {
    render(<GivingSummaryCard monthToDateTotal={0} yearToDateTotal={0} />);
    // Two "$0" renders.
    const zeros = screen.getAllByText('$0');
    expect(zeros).toHaveLength(2);
  });

  it('rounds fractional amounts (no decimals shown)', () => {
    render(<GivingSummaryCard monthToDateTotal={100.49} yearToDateTotal={100.51} />);
    expect(screen.getByText('$100')).toBeInTheDocument();
    expect(screen.getByText('$101')).toBeInTheDocument();
  });
});

describe('CommunicationsSummaryCard', () => {
  it('renders pending and sent-this-week counts', () => {
    render(<CommunicationsSummaryCard pendingCount={3} sentThisWeekCount={12} />);

    expect(screen.getByText('Pending')).toBeInTheDocument();
    expect(screen.getByText('Sent This Week')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
    expect(screen.getByText('12')).toBeInTheDocument();
  });

  it('handles zero counts', () => {
    render(<CommunicationsSummaryCard pendingCount={0} sentThisWeekCount={0} />);
    expect(screen.getAllByText('0')).toHaveLength(2);
  });
});
