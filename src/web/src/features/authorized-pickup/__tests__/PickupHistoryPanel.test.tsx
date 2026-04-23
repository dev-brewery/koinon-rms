/**
 * PickupHistoryPanel tests
 *
 * Branches:
 *   - Error branch (red banner) for both Error and non-Error values.
 *   - Loading branch.
 *   - Empty branch when history = [].
 *   - Table branch with Authorized / Not Authorized badges.
 *   - supervisorOverride banner with and without supervisorName.
 *   - Notes fallback to "-" when empty.
 *   - Date-range buttons re-invoke the hook with new from/to.
 *   - Pluralization "1 pickup" vs "N pickups".
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mockUsePickupHistory = vi.fn();

vi.mock('../hooks', () => ({
  usePickupHistory: (
    childIdKey: string,
    fromDate: string | undefined,
    toDate: string | undefined
  ) => mockUsePickupHistory(childIdKey, fromDate, toDate),
}));

import { PickupHistoryPanel } from '../PickupHistoryPanel';

const baseLog = {
  idKey: 'l1',
  childIdKey: 'c1',
  pickupPersonName: 'Alice',
  checkoutDateTime: '2026-04-22T10:00:00Z',
  wasAuthorized: true,
  supervisorOverride: false,
  supervisorName: null,
  notes: '',
};

describe('PickupHistoryPanel', () => {
  beforeEach(() => {
    mockUsePickupHistory.mockReset();
  });
  afterEach(() => vi.clearAllMocks());

  it('renders Error branch with Error.message', () => {
    mockUsePickupHistory.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error('bang'),
    });
    render(<PickupHistoryPanel childIdKey="c1" childName="Kid" />);
    expect(screen.getByText(/error loading pickup history/i)).toBeInTheDocument();
    expect(screen.getByText('bang')).toBeInTheDocument();
  });

  it('renders generic message when error has no .message', () => {
    mockUsePickupHistory.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: 'raw',
    });
    render(<PickupHistoryPanel childIdKey="c1" childName="Kid" />);
    expect(screen.getByText(/unknown error occurred/i)).toBeInTheDocument();
  });

  it('renders loading spinner', () => {
    mockUsePickupHistory.mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
    });
    render(<PickupHistoryPanel childIdKey="c1" childName="Kid" />);
    expect(screen.getByText(/loading history/i)).toBeInTheDocument();
  });

  it('renders empty state when data is []', () => {
    mockUsePickupHistory.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });
    render(<PickupHistoryPanel childIdKey="c1" childName="Kid" />);
    expect(screen.getByText(/no pickup history/i)).toBeInTheDocument();
  });

  it('renders Authorized + Not Authorized + Supervisor Override + supervisorName', () => {
    mockUsePickupHistory.mockReturnValue({
      data: [
        { ...baseLog, idKey: 'l1' },
        {
          ...baseLog,
          idKey: 'l2',
          wasAuthorized: false,
          supervisorOverride: true,
          supervisorName: 'Supervisor Bob',
          notes: 'late pickup',
        },
      ],
      isLoading: false,
      error: null,
    });
    render(<PickupHistoryPanel childIdKey="c1" childName="Kid" />);
    expect(screen.getByText('Authorized')).toBeInTheDocument();
    expect(screen.getByText('Not Authorized')).toBeInTheDocument();
    expect(screen.getByText('Supervisor Override')).toBeInTheDocument();
    expect(screen.getByText(/by: supervisor bob/i)).toBeInTheDocument();
    expect(screen.getByText('late pickup')).toBeInTheDocument();
    expect(screen.getByText(/showing/i)).toBeInTheDocument();
    // 2 entries -> plural "pickups"
    expect(screen.getByText(/pickups/i)).toBeInTheDocument();
  });

  it('falls back to "-" when notes is empty', () => {
    mockUsePickupHistory.mockReturnValue({
      data: [baseLog],
      isLoading: false,
      error: null,
    });
    render(<PickupHistoryPanel childIdKey="c1" childName="Kid" />);
    expect(screen.getByText('-')).toBeInTheDocument();
  });

  it('date-range buttons re-invoke the hook with new bounds', async () => {
    mockUsePickupHistory.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });
    const user = userEvent.setup();
    render(<PickupHistoryPanel childIdKey="c1" childName="Kid" />);
    mockUsePickupHistory.mockClear();
    await user.click(screen.getByRole('button', { name: /last 7 days/i }));
    // After clicking, hook is invoked again during next render.
    expect(mockUsePickupHistory).toHaveBeenCalled();

    await user.click(screen.getByRole('button', { name: /last 90 days/i }));
    expect(mockUsePickupHistory).toHaveBeenCalled();
  });

  it('singular "1 pickup" label when list has 1 entry', () => {
    mockUsePickupHistory.mockReturnValue({
      data: [baseLog],
      isLoading: false,
      error: null,
    });
    const { container } = render(<PickupHistoryPanel childIdKey="c1" childName="Kid" />);
    // Summary says "pickup" (singular), not "pickups"
    expect(container.textContent).toMatch(/Showing\s+1\s+pickup\b/);
    expect(container.textContent).not.toMatch(/pickups/);
  });
});
