/**
 * UpcomingSchedules tests
 *
 * Catches:
 *  - Loading state shows spinner text.
 *  - Empty state shows "No upcoming schedules".
 *  - Schedule row links to /admin/schedules/{idKey}.
 *  - Status text + colour depends on minutesUntilCheckIn:
 *    * <= 0      → "Open" (green)
 *    * < 60      → "Opens in N min" (yellow)
 *    * >= 60     → "Opens in N hr(s)" (gray)
 *  - Singular vs plural hour label.
 */
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { UpcomingSchedules } from '../UpcomingSchedules';
import type { UpcomingScheduleDto } from '@/types';

function schedule(overrides: Partial<UpcomingScheduleDto> = {}): UpcomingScheduleDto {
  return {
    idKey: 'S1',
    name: 'Sunday Service',
    nextOccurrence: '2026-04-25T09:00:00Z',
    minutesUntilCheckIn: 30,
    ...overrides,
  };
}

function wrap(ui: React.ReactNode) {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
}

describe('UpcomingSchedules', () => {
  it('renders loading state when isLoading=true', () => {
    wrap(<UpcomingSchedules schedules={[]} isLoading />);
    expect(screen.getByText(/loading schedules/i)).toBeInTheDocument();
  });

  it('renders empty state when no schedules', () => {
    wrap(<UpcomingSchedules schedules={[]} isLoading={false} />);
    expect(screen.getByText(/no upcoming schedules/i)).toBeInTheDocument();
  });

  it('shows "Open" in green when minutesUntilCheckIn <= 0', () => {
    wrap(
      <UpcomingSchedules
        isLoading={false}
        schedules={[schedule({ minutesUntilCheckIn: 0 })]}
      />,
    );
    const badge = screen.getByText('Open');
    expect(badge.className).toMatch(/text-green-700/);
  });

  it('shows "Opens in N min" in yellow when < 60 minutes', () => {
    wrap(
      <UpcomingSchedules
        isLoading={false}
        schedules={[schedule({ minutesUntilCheckIn: 45 })]}
      />,
    );
    const badge = screen.getByText(/opens in 45 min/i);
    expect(badge.className).toMatch(/text-yellow-700/);
  });

  it('shows "Opens in 1 hr" (singular) at exactly 60 minutes', () => {
    wrap(
      <UpcomingSchedules
        isLoading={false}
        schedules={[schedule({ minutesUntilCheckIn: 60 })]}
      />,
    );
    expect(screen.getByText(/opens in 1 hr$/i)).toBeInTheDocument();
  });

  it('pluralizes "Opens in 2 hrs" at 120 minutes', () => {
    wrap(
      <UpcomingSchedules
        isLoading={false}
        schedules={[schedule({ minutesUntilCheckIn: 120 })]}
      />,
    );
    expect(screen.getByText(/opens in 2 hrs/i)).toBeInTheDocument();
  });

  it('links each row to /admin/schedules/{idKey}', () => {
    wrap(
      <UpcomingSchedules
        isLoading={false}
        schedules={[schedule({ idKey: 'S9' })]}
      />,
    );
    expect(screen.getByRole('link')).toHaveAttribute('href', '/admin/schedules/S9');
  });
});
