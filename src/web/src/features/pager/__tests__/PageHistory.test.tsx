/**
 * PageHistory tests
 *
 * Branches covered:
 *   - No pagerNumber -> "Select a pager" prompt.
 *   - isLoading -> spinner.
 *   - error -> red banner.
 *   - history is undefined/null -> "No history found".
 *   - history with zero messages -> "No messages sent yet".
 *   - All 4 PagerMessageStatus branches (Delivered/Sent/Pending/Failed) render
 *     their respective labels.
 *   - deliveredDateTime conditional rendering.
 *   - Pluralization of "N messages sent" / "1 message sent".
 *   - parentPhoneNumber conditional rendering.
 */
import { render, screen, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mockUsePageHistory = vi.fn();

vi.mock('../hooks', () => ({
  usePageHistory: (pagerNumber: number | null) => mockUsePageHistory(pagerNumber),
}));

import { PageHistory } from '../PageHistory';
import { PagerMessageStatus, PagerMessageType } from '../api';

describe('PageHistory', () => {
  beforeEach(() => {
    mockUsePageHistory.mockReset();
  });
  afterEach(() => vi.clearAllMocks());

  it('prompts to select a pager when pagerNumber is null', () => {
    mockUsePageHistory.mockReturnValue({ data: undefined, isLoading: false, error: null });
    render(<PageHistory pagerNumber={null} />);
    expect(screen.getByText(/select a pager/i)).toBeInTheDocument();
  });

  it('renders loading state', () => {
    mockUsePageHistory.mockReturnValue({ data: undefined, isLoading: true, error: null });
    render(<PageHistory pagerNumber={127} />);
    expect(screen.getByText(/loading history/i)).toBeInTheDocument();
  });

  it('renders error banner on failure', () => {
    mockUsePageHistory.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error('x'),
    });
    render(<PageHistory pagerNumber={127} />);
    expect(screen.getByText(/failed to load page history/i)).toBeInTheDocument();
  });

  it('renders "No history found" when history is nullish', () => {
    mockUsePageHistory.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: null,
    });
    render(<PageHistory pagerNumber={127} />);
    expect(screen.getByText(/no history found/i)).toBeInTheDocument();
  });

  it('renders "No messages sent yet" when history has no messages', () => {
    mockUsePageHistory.mockReturnValue({
      data: {
        idKey: 'h1',
        pagerNumber: 127,
        childName: 'Kid',
        parentPhoneNumber: '5551234567',
        messages: [],
      },
      isLoading: false,
      error: null,
    });
    render(<PageHistory pagerNumber={127} />);
    expect(screen.getByText(/no messages sent yet/i)).toBeInTheDocument();
    expect(screen.getByText('Kid')).toBeInTheDocument();
    expect(screen.getByText('5551234567')).toBeInTheDocument();
  });

  it('hides the parent phone line when missing', () => {
    mockUsePageHistory.mockReturnValue({
      data: {
        idKey: 'h1',
        pagerNumber: 127,
        childName: 'Kid',
        parentPhoneNumber: '',
        messages: [],
      },
      isLoading: false,
      error: null,
    });
    render(<PageHistory pagerNumber={127} />);
    expect(screen.queryByText(/555/)).toBeNull();
  });

  it('renders all 4 status branches of the status display', () => {
    mockUsePageHistory.mockReturnValue({
      data: {
        idKey: 'h1',
        pagerNumber: 127,
        childName: 'Kid',
        parentPhoneNumber: '',
        messages: [
          {
            idKey: 'm1',
            messageType: PagerMessageType.PickupNeeded,
            messageText: 'Msg 1',
            status: PagerMessageStatus.Delivered,
            sentDateTime: '2026-04-22T10:00:00Z',
            deliveredDateTime: '2026-04-22T10:01:00Z',
            sentByPersonName: 'Alice',
          },
          {
            idKey: 'm2',
            messageType: PagerMessageType.NeedsAttention,
            messageText: 'Msg 2',
            status: PagerMessageStatus.Sent,
            sentDateTime: '2026-04-22T10:02:00Z',
            deliveredDateTime: null,
            sentByPersonName: 'Bob',
          },
          {
            idKey: 'm3',
            messageType: PagerMessageType.Custom,
            messageText: 'Msg 3',
            status: PagerMessageStatus.Pending,
            sentDateTime: '2026-04-22T10:03:00Z',
            deliveredDateTime: null,
            sentByPersonName: 'Carol',
          },
          {
            idKey: 'm4',
            messageType: PagerMessageType.ServiceEnding,
            messageText: 'Msg 4',
            status: PagerMessageStatus.Failed,
            sentDateTime: '2026-04-22T10:04:00Z',
            deliveredDateTime: null,
            sentByPersonName: 'Dave',
          },
        ],
      },
      isLoading: false,
      error: null,
    });
    render(<PageHistory pagerNumber={127} />);
    expect(screen.getByText('Delivered')).toBeInTheDocument();
    expect(screen.getByText('Sent')).toBeInTheDocument();
    expect(screen.getByText('Pending')).toBeInTheDocument();
    expect(screen.getByText('Failed')).toBeInTheDocument();
    // Only Delivered message exposes "Delivered at"
    expect(screen.getByText(/delivered at/i)).toBeInTheDocument();
    // 4 messages plural
    expect(screen.getByText(/4 messages sent/i)).toBeInTheDocument();
  });

  it('uses singular "1 message sent" for a single entry', () => {
    mockUsePageHistory.mockReturnValue({
      data: {
        idKey: 'h1',
        pagerNumber: 128,
        childName: 'Kid',
        parentPhoneNumber: '',
        messages: [
          {
            idKey: 'm1',
            messageType: PagerMessageType.PickupNeeded,
            messageText: 'Only Msg',
            status: PagerMessageStatus.Delivered,
            sentDateTime: '2026-04-22T10:00:00Z',
            deliveredDateTime: '2026-04-22T10:01:00Z',
            sentByPersonName: 'Alice',
          },
        ],
      },
      isLoading: false,
      error: null,
    });
    const { container } = render(<PageHistory pagerNumber={128} />);
    expect(within(container).getByText(/1 message sent/i)).toBeInTheDocument();
  });
});
