/**
 * SendPageDialog tests
 *
 * Branches:
 *   - Returns null when pager is null.
 *   - Renders pager info + child name + default message type preview.
 *   - No phone warning renders when parentPhoneNumber is missing.
 *   - Rate-limit warnings: approaching (2 msgs) and exceeded (3+ msgs).
 *   - Selecting "Custom" reveals textarea; preview uses textarea content.
 *   - Send button disabled conditions: no phone, over limit, empty custom,
 *     mutation pending.
 *   - handleSend mutates with pagerNumber as string and the right message
 *     type; Custom passes customMessage; non-Custom passes undefined.
 *   - Error display shows mutation.error message; falls back to generic text.
 *   - Success display shows when mutation.isSuccess=true.
 *   - handleClose resets state + calls onClose; backdrop click also closes.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

let mutationState: {
  mutateAsync: ReturnType<typeof vi.fn>;
  reset: ReturnType<typeof vi.fn>;
  isPending: boolean;
  isError: boolean;
  isSuccess: boolean;
  error: unknown;
};

vi.mock('../hooks', () => ({
  useSendPage: () => mutationState,
}));

import { SendPageDialog } from '../SendPageDialog';

const basePager = {
  idKey: 'p1',
  pagerNumber: 127,
  attendanceIdKey: 'a1',
  childName: 'Kid One',
  groupName: 'K-1',
  locationName: 'Room 1',
  parentPhoneNumber: '5551234567',
  checkedInAt: '2026-04-22T10:00:00Z',
  messagesSentCount: 0,
};

describe('SendPageDialog', () => {
  beforeEach(() => {
    mutationState = {
      mutateAsync: vi.fn().mockResolvedValue(undefined),
      reset: vi.fn(),
      isPending: false,
      isError: false,
      isSuccess: false,
      error: null,
    };
  });
  afterEach(() => vi.clearAllMocks());

  it('renders null when pager is null', () => {
    const { container } = render(
      <SendPageDialog pager={null} onClose={vi.fn()} onSuccess={vi.fn()} />
    );
    expect(container).toBeEmptyDOMElement();
  });

  it('renders header and default Pickup Needed preview', () => {
    render(
      <SendPageDialog pager={basePager} onClose={vi.fn()} onSuccess={vi.fn()} />
    );
    expect(screen.getByText('P-127')).toBeInTheDocument();
    expect(screen.getByText('Kid One')).toBeInTheDocument();
    // Preview defaults to Pickup Needed template
    expect(screen.getByText(/please come pick up your child/i)).toBeInTheDocument();
  });

  it('shows No Phone warning when pager has no phone', () => {
    render(
      <SendPageDialog
        pager={{ ...basePager, parentPhoneNumber: null }}
        onClose={vi.fn()}
        onSuccess={vi.fn()}
      />
    );
    expect(screen.getByText(/no phone number/i)).toBeInTheDocument();
  });

  it('shows approaching-limit banner when messagesSentCount === 2', () => {
    render(
      <SendPageDialog
        pager={{ ...basePager, messagesSentCount: 2 }}
        onClose={vi.fn()}
        onSuccess={vi.fn()}
      />
    );
    expect(screen.getByText(/approaching rate limit/i)).toBeInTheDocument();
  });

  it('shows rate-limit-exceeded banner when messagesSentCount >= 3', () => {
    render(
      <SendPageDialog
        pager={{ ...basePager, messagesSentCount: 3 }}
        onClose={vi.fn()}
        onSuccess={vi.fn()}
      />
    );
    expect(screen.getByText(/rate limit exceeded/i)).toBeInTheDocument();
  });

  it('selecting Custom reveals textarea and uses its content in preview', async () => {
    const user = userEvent.setup();
    render(
      <SendPageDialog pager={basePager} onClose={vi.fn()} onSuccess={vi.fn()} />
    );
    // Click the "Custom Message" radio option.
    await user.click(screen.getByLabelText(/custom message/i));
    const textarea = screen.getByPlaceholderText(/enter your custom message/i);
    await user.type(textarea, 'hello there');
    // Preview reflects the typed text.
    expect(screen.getAllByText(/hello there/i).length).toBeGreaterThan(0);
  });

  it('Send is disabled when no phone number', () => {
    render(
      <SendPageDialog
        pager={{ ...basePager, parentPhoneNumber: null }}
        onClose={vi.fn()}
        onSuccess={vi.fn()}
      />
    );
    const send = screen.getByRole('button', { name: /send page/i });
    expect(send).toBeDisabled();
  });

  it('Send is disabled when rate limit reached', () => {
    render(
      <SendPageDialog
        pager={{ ...basePager, messagesSentCount: 3 }}
        onClose={vi.fn()}
        onSuccess={vi.fn()}
      />
    );
    expect(screen.getByRole('button', { name: /send page/i })).toBeDisabled();
  });

  it('Send is disabled with empty custom message', async () => {
    const user = userEvent.setup();
    render(
      <SendPageDialog pager={basePager} onClose={vi.fn()} onSuccess={vi.fn()} />
    );
    await user.click(screen.getByLabelText(/custom message/i));
    expect(screen.getByRole('button', { name: /send page/i })).toBeDisabled();
  });

  it('Send enabled with non-empty custom, mutates with customMessage', async () => {
    const user = userEvent.setup();
    const onSuccess = vi.fn();
    const onClose = vi.fn();
    render(
      <SendPageDialog pager={basePager} onClose={onClose} onSuccess={onSuccess} />
    );
    await user.click(screen.getByLabelText(/custom message/i));
    await user.type(screen.getByPlaceholderText(/enter your custom/i), 'urgent');
    await user.click(screen.getByRole('button', { name: /send page/i }));
    expect(mutationState.mutateAsync).toHaveBeenCalledWith(
      expect.objectContaining({
        pagerNumber: '127',
        customMessage: 'urgent',
      })
    );
    expect(onSuccess).toHaveBeenCalled();
    expect(onClose).toHaveBeenCalled();
  });

  it('non-Custom messageType sends undefined customMessage', async () => {
    const user = userEvent.setup();
    render(
      <SendPageDialog pager={basePager} onClose={vi.fn()} onSuccess={vi.fn()} />
    );
    await user.click(screen.getByRole('button', { name: /send page/i }));
    expect(mutationState.mutateAsync).toHaveBeenCalledWith(
      expect.objectContaining({ customMessage: undefined })
    );
  });

  it('handleSend swallows rejections without invoking onSuccess', async () => {
    mutationState.mutateAsync = vi.fn().mockRejectedValue(new Error('nope'));
    const onSuccess = vi.fn();
    const onClose = vi.fn();
    const user = userEvent.setup();
    render(
      <SendPageDialog pager={basePager} onClose={onClose} onSuccess={onSuccess} />
    );
    await user.click(screen.getByRole('button', { name: /send page/i }));
    expect(onSuccess).not.toHaveBeenCalled();
    expect(onClose).not.toHaveBeenCalled();
  });

  it('error display uses mutation.error message when Error', () => {
    mutationState.isError = true;
    mutationState.error = new Error('sms boom');
    render(
      <SendPageDialog pager={basePager} onClose={vi.fn()} onSuccess={vi.fn()} />
    );
    expect(screen.getByText(/sms boom/)).toBeInTheDocument();
  });

  it('error display falls back to generic when error is not Error', () => {
    mutationState.isError = true;
    mutationState.error = 'raw';
    render(
      <SendPageDialog pager={basePager} onClose={vi.fn()} onSuccess={vi.fn()} />
    );
    expect(screen.getByText(/failed to send page/i)).toBeInTheDocument();
  });

  it('success banner renders when mutation.isSuccess=true', () => {
    mutationState.isSuccess = true;
    render(
      <SendPageDialog pager={basePager} onClose={vi.fn()} onSuccess={vi.fn()} />
    );
    expect(screen.getByText(/page sent successfully/i)).toBeInTheDocument();
  });

  it('Cancel button closes & resets', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(
      <SendPageDialog pager={basePager} onClose={onClose} onSuccess={vi.fn()} />
    );
    await user.click(screen.getByRole('button', { name: /^cancel$/i }));
    expect(mutationState.reset).toHaveBeenCalled();
    expect(onClose).toHaveBeenCalled();
  });

  it('close X button closes', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(
      <SendPageDialog pager={basePager} onClose={onClose} onSuccess={vi.fn()} />
    );
    await user.click(screen.getByRole('button', { name: /close/i }));
    expect(onClose).toHaveBeenCalled();
  });

  it('Send button shows loading when isPending', () => {
    mutationState.isPending = true;
    render(
      <SendPageDialog pager={basePager} onClose={vi.fn()} onSuccess={vi.fn()} />
    );
    const send = screen.getByRole('button', { name: /send page/i });
    expect(send).toBeDisabled();
  });
});
