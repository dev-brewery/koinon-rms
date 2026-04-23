/**
 * RequestToJoinModal tests
 *
 * Branches:
 *   - Closed (isOpen=false) renders nothing.
 *   - Open renders header, groupName, textarea, counter.
 *   - Submit calls submitMutation.mutateAsync with trimmed note; empty -> undefined.
 *   - Error message displayed on mutation rejection.
 *   - Success view shown when mutation resolves.
 *   - Close button fires onClose (disabled while pending).
 *   - Escape key triggers close.
 *   - Backdrop click triggers close.
 */
import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

let mutation: {
  mutateAsync: ReturnType<typeof vi.fn>;
  isPending: boolean;
};

vi.mock('@/hooks/useMembershipRequests', () => ({
  useSubmitMembershipRequest: () => mutation,
}));

import { RequestToJoinModal } from '../RequestToJoinModal';

describe('RequestToJoinModal', () => {
  beforeEach(() => {
    mutation = {
      mutateAsync: vi.fn().mockResolvedValue(undefined),
      isPending: false,
    };
  });
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders nothing when isOpen=false', () => {
    const { container } = render(
      <RequestToJoinModal
        isOpen={false}
        onClose={vi.fn()}
        groupIdKey="g1"
        groupName="Alpha"
      />
    );
    expect(container).toBeEmptyDOMElement();
  });

  it('renders header + groupName + textarea when open', () => {
    render(
      <RequestToJoinModal
        isOpen
        onClose={vi.fn()}
        groupIdKey="g1"
        groupName="Alpha"
      />
    );
    expect(screen.getByRole('heading', { name: /request to join group/i })).toBeInTheDocument();
    expect(screen.getByText('Alpha')).toBeInTheDocument();
    expect(screen.getByLabelText(/message/i)).toBeInTheDocument();
  });

  it('submit with trimmed note calls mutateAsync with note', async () => {
    const user = userEvent.setup();
    render(
      <RequestToJoinModal
        isOpen
        onClose={vi.fn()}
        groupIdKey="g1"
        groupName="Alpha"
      />
    );
    await user.type(screen.getByLabelText(/message/i), '  please  ');
    await user.click(screen.getByRole('button', { name: /submit request/i }));
    expect(mutation.mutateAsync).toHaveBeenCalledWith({ note: 'please' });
  });

  it('submit with empty note sends undefined', async () => {
    const user = userEvent.setup();
    render(
      <RequestToJoinModal
        isOpen
        onClose={vi.fn()}
        groupIdKey="g1"
        groupName="Alpha"
      />
    );
    await user.click(screen.getByRole('button', { name: /submit request/i }));
    expect(mutation.mutateAsync).toHaveBeenCalledWith({ note: undefined });
  });

  it('displays Error.message when mutation rejects with Error', async () => {
    mutation.mutateAsync = vi.fn().mockRejectedValue(new Error('already a member'));
    const user = userEvent.setup();
    render(
      <RequestToJoinModal
        isOpen
        onClose={vi.fn()}
        groupIdKey="g1"
        groupName="Alpha"
      />
    );
    await user.click(screen.getByRole('button', { name: /submit request/i }));
    expect(await screen.findByText(/already a member/)).toBeInTheDocument();
  });

  it('displays default error when rejection is not Error', async () => {
    mutation.mutateAsync = vi.fn().mockRejectedValue('raw');
    const user = userEvent.setup();
    render(
      <RequestToJoinModal
        isOpen
        onClose={vi.fn()}
        groupIdKey="g1"
        groupName="Alpha"
      />
    );
    await user.click(screen.getByRole('button', { name: /submit request/i }));
    expect(await screen.findByText(/failed to submit request/i)).toBeInTheDocument();
  });

  it('shows success view after successful submit', async () => {
    const user = userEvent.setup();
    render(
      <RequestToJoinModal
        isOpen
        onClose={vi.fn()}
        groupIdKey="g1"
        groupName="Alpha"
      />
    );
    await user.click(screen.getByRole('button', { name: /submit request/i }));
    expect(await screen.findByText(/request submitted/i)).toBeInTheDocument();
  });

  it('Close button fires onClose', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(
      <RequestToJoinModal
        isOpen
        onClose={onClose}
        groupIdKey="g1"
        groupName="Alpha"
      />
    );
    await user.click(screen.getByRole('button', { name: /close/i }));
    expect(onClose).toHaveBeenCalled();
  });

  it('Cancel button fires onClose', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(
      <RequestToJoinModal
        isOpen
        onClose={onClose}
        groupIdKey="g1"
        groupName="Alpha"
      />
    );
    await user.click(screen.getByRole('button', { name: /^cancel$/i }));
    expect(onClose).toHaveBeenCalled();
  });

  it('Escape key triggers close', () => {
    const onClose = vi.fn();
    render(
      <RequestToJoinModal
        isOpen
        onClose={onClose}
        groupIdKey="g1"
        groupName="Alpha"
      />
    );
    act(() => {
      document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    });
    expect(onClose).toHaveBeenCalled();
  });

  it('Close button is disabled while mutation is pending', () => {
    mutation.isPending = true;
    render(
      <RequestToJoinModal
        isOpen
        onClose={vi.fn()}
        groupIdKey="g1"
        groupName="Alpha"
      />
    );
    expect(screen.getByRole('button', { name: /close/i })).toBeDisabled();
  });

  it('counter tracks note length', async () => {
    const user = userEvent.setup();
    render(
      <RequestToJoinModal
        isOpen
        onClose={vi.fn()}
        groupIdKey="g1"
        groupName="Alpha"
      />
    );
    await user.type(screen.getByLabelText(/message/i), 'hello');
    expect(screen.getByText(/5\/2000/)).toBeInTheDocument();
  });
});
