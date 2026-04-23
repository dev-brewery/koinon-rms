/**
 * FollowUpQueue tests
 *
 * Covers:
 *   - Loading branch: spinner visible while isLoading=true.
 *   - Error branch: red panel + Try Again button triggers refetch().
 *   - Empty branch: nice empty state when followUps is empty.
 *   - Stats bar shows counts per status.
 *   - Clicking a status filter narrows the list.
 *   - Filtering to a status with no items shows "No follow-ups match the filter"
 *     + a "Clear Filter" button that resets to all.
 *   - "all" selected by default renders every item.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mockUsePendingFollowUps = vi.fn();
const mockRefetch = vi.fn();

vi.mock('../hooks', () => ({
  usePendingFollowUps: (assignedToIdKey?: string) =>
    mockUsePendingFollowUps(assignedToIdKey),
  useUpdateFollowUpStatus: () => ({ mutate: vi.fn(), isPending: false }),
}));

import { FollowUpQueue } from '../FollowUpQueue';
import { FollowUpStatus } from '../api';

const makeFollowUp = (status: FollowUpStatus, idKey: string) => ({
  idKey,
  personIdKey: 'p1',
  personName: `Person ${idKey}`,
  assignedToIdKey: null,
  assignedToName: null,
  status,
  notes: '',
  createdDateTime: '2026-04-01T10:00:00Z',
  contactedDateTime: null,
  completedDateTime: null,
});

describe('FollowUpQueue', () => {
  beforeEach(() => {
    mockUsePendingFollowUps.mockReset();
    mockRefetch.mockReset();
  });
  afterEach(() => vi.clearAllMocks());

  it('renders loading state', () => {
    mockUsePendingFollowUps.mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
      refetch: mockRefetch,
    });
    render(<FollowUpQueue />);
    expect(screen.getByText(/loading follow-ups/i)).toBeInTheDocument();
  });

  it('renders error state with Try Again invoking refetch', async () => {
    const user = userEvent.setup();
    mockUsePendingFollowUps.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error('boom'),
      refetch: mockRefetch,
    });
    render(<FollowUpQueue />);
    expect(screen.getByText(/error loading follow-ups/i)).toBeInTheDocument();
    expect(screen.getByText('boom')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /try again/i }));
    expect(mockRefetch).toHaveBeenCalled();
  });

  it('renders generic error fallback when error is not an Error', () => {
    mockUsePendingFollowUps.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: 'raw',
      refetch: mockRefetch,
    });
    render(<FollowUpQueue />);
    expect(screen.getByText(/unknown error occurred/i)).toBeInTheDocument();
  });

  it('renders empty state when no follow-ups', () => {
    mockUsePendingFollowUps.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
      refetch: mockRefetch,
    });
    render(<FollowUpQueue />);
    expect(screen.getByText(/no follow-ups/i)).toBeInTheDocument();
    expect(screen.getByText(/no pending follow-ups/i)).toBeInTheDocument();
  });

  it('renders full queue with stats bar and cards', () => {
    const followUps = [
      makeFollowUp(FollowUpStatus.Pending, 'fu1'),
      makeFollowUp(FollowUpStatus.Pending, 'fu2'),
      makeFollowUp(FollowUpStatus.Contacted, 'fu3'),
      makeFollowUp(FollowUpStatus.Connected, 'fu4'),
      makeFollowUp(FollowUpStatus.NoResponse, 'fu5'),
    ];
    mockUsePendingFollowUps.mockReturnValue({
      data: followUps,
      isLoading: false,
      error: null,
      refetch: mockRefetch,
    });
    render(<FollowUpQueue />);
    // Header + stats
    expect(screen.getByRole('heading', { name: /follow-up queue/i })).toBeInTheDocument();
    // Total count
    expect(screen.getByText('Total')).toBeInTheDocument();
    // One card per item
    expect(screen.getByText('Person fu1')).toBeInTheDocument();
    expect(screen.getByText('Person fu2')).toBeInTheDocument();
    expect(screen.getByText('Person fu3')).toBeInTheDocument();
  });

  it('filters by status when a stat button is clicked', async () => {
    const followUps = [
      makeFollowUp(FollowUpStatus.Pending, 'fu1'),
      makeFollowUp(FollowUpStatus.Contacted, 'fu2'),
      makeFollowUp(FollowUpStatus.Connected, 'fu3'),
    ];
    mockUsePendingFollowUps.mockReturnValue({
      data: followUps,
      isLoading: false,
      error: null,
      refetch: mockRefetch,
    });
    const user = userEvent.setup();
    render(<FollowUpQueue />);
    // Click "Contacted" filter
    await user.click(screen.getByRole('button', { name: /contacted/i }));
    // Only fu2 (Contacted) should be visible
    expect(screen.queryByText('Person fu1')).toBeNull();
    expect(screen.getByText('Person fu2')).toBeInTheDocument();
    expect(screen.queryByText('Person fu3')).toBeNull();
  });

  it('shows "no follow-ups match" + Clear Filter resets', async () => {
    const followUps = [makeFollowUp(FollowUpStatus.Pending, 'fu1')];
    mockUsePendingFollowUps.mockReturnValue({
      data: followUps,
      isLoading: false,
      error: null,
      refetch: mockRefetch,
    });
    const user = userEvent.setup();
    render(<FollowUpQueue />);
    // Click Connected — there are zero. Expect "no follow-ups match".
    await user.click(screen.getByRole('button', { name: /connected/i }));
    expect(screen.getByText(/no follow-ups match the selected filter/i)).toBeInTheDocument();
    // Clear filter restores
    await user.click(screen.getByRole('button', { name: /clear filter/i }));
    expect(screen.getByText('Person fu1')).toBeInTheDocument();
  });

  it('passes assignedToIdKey to the query hook', () => {
    mockUsePendingFollowUps.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
      refetch: mockRefetch,
    });
    render(<FollowUpQueue assignedToIdKey="u1" />);
    expect(mockUsePendingFollowUps).toHaveBeenCalledWith('u1');
  });

  it('No Response filter narrows correctly', async () => {
    const followUps = [
      makeFollowUp(FollowUpStatus.Pending, 'fu1'),
      makeFollowUp(FollowUpStatus.NoResponse, 'fu2'),
    ];
    mockUsePendingFollowUps.mockReturnValue({
      data: followUps,
      isLoading: false,
      error: null,
      refetch: mockRefetch,
    });
    const user = userEvent.setup();
    render(<FollowUpQueue />);
    await user.click(screen.getByRole('button', { name: /no response/i }));
    expect(screen.queryByText('Person fu1')).toBeNull();
    expect(screen.getByText('Person fu2')).toBeInTheDocument();
  });

  it('Pending filter narrows correctly', async () => {
    const followUps = [
      makeFollowUp(FollowUpStatus.Pending, 'fu1'),
      makeFollowUp(FollowUpStatus.Contacted, 'fu2'),
    ];
    mockUsePendingFollowUps.mockReturnValue({
      data: followUps,
      isLoading: false,
      error: null,
      refetch: mockRefetch,
    });
    const user = userEvent.setup();
    render(<FollowUpQueue />);
    await user.click(screen.getByRole('button', { name: /pending/i }));
    expect(screen.getByText('Person fu1')).toBeInTheDocument();
    expect(screen.queryByText('Person fu2')).toBeNull();
  });
});
