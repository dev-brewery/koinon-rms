/**
 * FollowUpCard tests
 *
 * Catches regressions in:
 *   - Status badge label matches the status enum branch.
 *   - Notes preview shows "No notes yet" fallback when empty.
 *   - Edit Notes toggle swaps the notes display with a textarea.
 *   - Cancel restores original notes and exits edit mode.
 *   - Save Notes invokes mutate with preserved status.
 *   - Update Status panel exposes 4 status buttons; clicking one mutates.
 *   - Update Status button is disabled when status is Connected/Declined.
 *   - Timestamps (contactedDateTime / completedDateTime) render only when set.
 */
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mutate = vi.fn();
vi.mock('../hooks', () => ({
  useUpdateFollowUpStatus: () => ({
    mutate,
    isPending: false,
  }),
}));

import { FollowUpCard } from '../FollowUpCard';
import { FollowUpStatus } from '../api';

const baseFollowUp = {
  idKey: 'fu1',
  personIdKey: 'p1',
  personName: 'Alice Example',
  assignedToIdKey: 'u1',
  assignedToName: 'Bob Assignee',
  status: FollowUpStatus.Pending,
  notes: '',
  createdDateTime: '2026-01-01T10:00:00Z',
  contactedDateTime: null,
  completedDateTime: null,
};

describe('FollowUpCard', () => {
  beforeEach(() => {
    mutate.mockReset();
  });
  afterEach(() => vi.clearAllMocks());

  it('shows the Pending badge label', () => {
    render(<FollowUpCard followUp={baseFollowUp as never} />);
    expect(screen.getByText('Pending')).toBeInTheDocument();
    expect(screen.getByText('Alice Example')).toBeInTheDocument();
  });

  it('renders "No notes yet" when notes is empty', () => {
    render(<FollowUpCard followUp={baseFollowUp as never} />);
    expect(screen.getByText(/no notes yet/i)).toBeInTheDocument();
  });

  it('renders the notes text when provided', () => {
    render(<FollowUpCard followUp={{ ...baseFollowUp, notes: 'tried calling' } as never} />);
    expect(screen.getByText('tried calling')).toBeInTheDocument();
  });

  it('switches to editing mode and displays a textarea', async () => {
    const user = userEvent.setup();
    render(<FollowUpCard followUp={baseFollowUp as never} />);
    await user.click(screen.getByRole('button', { name: /edit notes/i }));
    expect(screen.getByRole('textbox')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /save notes/i })).toBeInTheDocument();
  });

  it('Cancel from edit mode restores original notes and hides textarea', async () => {
    const user = userEvent.setup();
    render(<FollowUpCard followUp={{ ...baseFollowUp, notes: 'orig' } as never} />);
    await user.click(screen.getByRole('button', { name: /edit notes/i }));
    const textarea = screen.getByRole('textbox');
    await user.clear(textarea);
    await user.type(textarea, 'new text');
    await user.click(screen.getByRole('button', { name: /cancel/i }));
    // Back in preview mode showing the original notes.
    expect(screen.queryByRole('textbox')).toBeNull();
    expect(screen.getByText('orig')).toBeInTheDocument();
  });

  it('Save Notes calls mutate with preserved status and current notes', async () => {
    const user = userEvent.setup();
    render(<FollowUpCard followUp={baseFollowUp as never} />);
    await user.click(screen.getByRole('button', { name: /edit notes/i }));
    await user.type(screen.getByRole('textbox'), 'added a note');
    await user.click(screen.getByRole('button', { name: /save notes/i }));
    expect(mutate).toHaveBeenCalledWith(
      expect.objectContaining({
        idKey: 'fu1',
        status: FollowUpStatus.Pending,
        notes: 'added a note',
      }),
      expect.objectContaining({ onSuccess: expect.any(Function) })
    );
  });

  it('Update Status opens action panel with 4 status buttons', async () => {
    const user = userEvent.setup();
    render(<FollowUpCard followUp={baseFollowUp as never} />);
    await user.click(screen.getByRole('button', { name: /update status/i }));
    expect(screen.getByRole('button', { name: /mark contacted/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /mark connected/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /no response/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /declined/i })).toBeInTheDocument();
  });

  it('clicking Mark Contacted mutates with Contacted status', async () => {
    const user = userEvent.setup();
    render(<FollowUpCard followUp={baseFollowUp as never} />);
    await user.click(screen.getByRole('button', { name: /update status/i }));
    await user.click(screen.getByRole('button', { name: /mark contacted/i }));
    expect(mutate).toHaveBeenCalledWith(
      expect.objectContaining({ status: FollowUpStatus.Contacted }),
      expect.any(Object)
    );
  });

  it('Update Status is disabled for Connected', () => {
    render(
      <FollowUpCard
        followUp={{ ...baseFollowUp, status: FollowUpStatus.Connected } as never}
      />
    );
    const btn = screen.getByRole('button', { name: /update status/i });
    expect(btn).toBeDisabled();
  });

  it('Update Status is disabled for Declined', () => {
    render(
      <FollowUpCard
        followUp={{ ...baseFollowUp, status: FollowUpStatus.Declined } as never}
      />
    );
    const btn = screen.getByRole('button', { name: /update status/i });
    expect(btn).toBeDisabled();
  });

  it('renders Contacted and Completed timestamps only when present', () => {
    const { rerender } = render(
      <FollowUpCard followUp={baseFollowUp as never} />
    );
    expect(screen.queryByText(/contacted:/i)).toBeNull();
    expect(screen.queryByText(/completed:/i)).toBeNull();

    rerender(
      <FollowUpCard
        followUp={
          {
            ...baseFollowUp,
            contactedDateTime: '2026-02-01T10:00:00Z',
            completedDateTime: '2026-03-01T10:00:00Z',
          } as never
        }
      />
    );
    expect(screen.getByText(/contacted:/i)).toBeInTheDocument();
    expect(screen.getByText(/completed:/i)).toBeInTheDocument();
  });

  it('shows the Assigned to line only when assignedToName is set', () => {
    const { rerender } = render(
      <FollowUpCard followUp={{ ...baseFollowUp, assignedToName: undefined } as never} />
    );
    expect(screen.queryByText(/assigned to:/i)).toBeNull();
    rerender(<FollowUpCard followUp={baseFollowUp as never} />);
    expect(screen.getByText(/assigned to:/i)).toBeInTheDocument();
  });

  it('Cancel in status panel closes it without mutating', async () => {
    const user = userEvent.setup();
    render(<FollowUpCard followUp={baseFollowUp as never} />);
    await user.click(screen.getByRole('button', { name: /update status/i }));
    // Find "Cancel" within the action panel.
    const cancels = screen.getAllByRole('button', { name: /cancel/i });
    await user.click(cancels[cancels.length - 1]);
    expect(mutate).not.toHaveBeenCalled();
    // Status panel is gone.
    expect(screen.queryByRole('button', { name: /mark contacted/i })).toBeNull();
  });

  it('renders distinct badge for each known status', () => {
    const statuses: Array<[FollowUpStatus, string]> = [
      [FollowUpStatus.Pending, 'Pending'],
      [FollowUpStatus.Contacted, 'Contacted'],
      [FollowUpStatus.NoResponse, 'No Response'],
      [FollowUpStatus.Connected, 'Connected'],
      [FollowUpStatus.Declined, 'Declined'],
    ];
    for (const [status, label] of statuses) {
      const { unmount, container } = render(
        <FollowUpCard followUp={{ ...baseFollowUp, status } as never} />
      );
      expect(within(container).getByText(label)).toBeInTheDocument();
      unmount();
    }
  });
});
