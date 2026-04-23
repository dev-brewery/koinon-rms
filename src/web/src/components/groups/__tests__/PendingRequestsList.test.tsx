/**
 * PendingRequestsList tests
 *
 * Branches:
 *   - Loading -> spinner.
 *   - Empty requests -> "No pending requests".
 *   - Renders one button per request with requester name + date.
 *   - Shows request note preview when requestNote present.
 *   - Clicking a request opens the review modal with that request.
 *   - Approve / Deny handlers call processRequest mutation with correct shape.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mockUsePendingRequests = vi.fn();
const mockMutateAsync = vi.fn();

vi.mock('@/hooks/useMembershipRequests', () => ({
  usePendingRequests: (groupIdKey: string) => mockUsePendingRequests(groupIdKey),
  useProcessRequest: () => ({ mutateAsync: mockMutateAsync }),
}));

// Capture the props passed into the modal.
vi.mock('../RequestReviewModal', () => ({
  RequestReviewModal: ({
    isOpen,
    request,
    onApprove,
    onDeny,
    onClose,
  }: {
    isOpen: boolean;
    request: { idKey: string } | null;
    onApprove: (id: string, note?: string) => void;
    onDeny: (id: string, note?: string) => void;
    onClose: () => void;
  }) =>
    isOpen ? (
      <div data-testid="review-modal">
        <span>modal:{request?.idKey}</span>
        <button onClick={() => onApprove(request!.idKey, 'ok')}>approve</button>
        <button onClick={() => onDeny(request!.idKey, 'no')}>deny</button>
        <button onClick={onClose}>close</button>
      </div>
    ) : null,
}));

import { PendingRequestsList } from '../PendingRequestsList';

describe('PendingRequestsList', () => {
  beforeEach(() => {
    mockUsePendingRequests.mockReset();
    mockMutateAsync.mockReset();
  });
  afterEach(() => vi.clearAllMocks());

  it('renders loading spinner', () => {
    mockUsePendingRequests.mockReturnValue({ data: undefined, isLoading: true });
    const { container } = render(<PendingRequestsList groupIdKey="g1" />);
    // Spinner has animate-spin class.
    expect(container.querySelector('.animate-spin')).toBeTruthy();
  });

  it('renders "No pending requests" when empty', () => {
    mockUsePendingRequests.mockReturnValue({ data: [], isLoading: false });
    render(<PendingRequestsList groupIdKey="g1" />);
    expect(screen.getByText(/no pending requests/i)).toBeInTheDocument();
  });

  it('renders one button per request with requester name + request note', () => {
    mockUsePendingRequests.mockReturnValue({
      data: [
        {
          idKey: 'r1',
          requester: { fullName: 'Alice Req' },
          createdDateTime: '2026-04-01T10:00:00Z',
          requestNote: 'please',
        },
        {
          idKey: 'r2',
          requester: { fullName: 'Bob Req' },
          createdDateTime: '2026-04-01T11:00:00Z',
          requestNote: '',
        },
      ],
      isLoading: false,
    });
    render(<PendingRequestsList groupIdKey="g1" />);
    expect(screen.getByText('Alice Req')).toBeInTheDocument();
    expect(screen.getByText('Bob Req')).toBeInTheDocument();
    expect(screen.getByText('please')).toBeInTheDocument();
  });

  it('clicking a request opens the modal with that request', async () => {
    mockUsePendingRequests.mockReturnValue({
      data: [
        {
          idKey: 'r1',
          requester: { fullName: 'Alice Req' },
          createdDateTime: '2026-04-01T10:00:00Z',
          requestNote: '',
        },
      ],
      isLoading: false,
    });
    const user = userEvent.setup();
    render(<PendingRequestsList groupIdKey="g1" />);
    await user.click(screen.getByText('Alice Req'));
    expect(screen.getByTestId('review-modal')).toBeInTheDocument();
    expect(screen.getByText('modal:r1')).toBeInTheDocument();
  });

  it('Approve in modal mutates with status=Approved and passes note', async () => {
    mockUsePendingRequests.mockReturnValue({
      data: [
        {
          idKey: 'r1',
          requester: { fullName: 'Alice' },
          createdDateTime: '2026-04-01T10:00:00Z',
          requestNote: '',
        },
      ],
      isLoading: false,
    });
    mockMutateAsync.mockResolvedValueOnce(undefined);
    const user = userEvent.setup();
    render(<PendingRequestsList groupIdKey="g1" />);
    await user.click(screen.getByText('Alice'));
    await user.click(screen.getByText('approve'));
    expect(mockMutateAsync).toHaveBeenCalledWith({
      requestIdKey: 'r1',
      request: { status: 'Approved', note: 'ok' },
    });
  });

  it('Deny in modal mutates with status=Denied', async () => {
    mockUsePendingRequests.mockReturnValue({
      data: [
        {
          idKey: 'r1',
          requester: { fullName: 'Alice' },
          createdDateTime: '2026-04-01T10:00:00Z',
          requestNote: '',
        },
      ],
      isLoading: false,
    });
    mockMutateAsync.mockResolvedValueOnce(undefined);
    const user = userEvent.setup();
    render(<PendingRequestsList groupIdKey="g1" />);
    await user.click(screen.getByText('Alice'));
    await user.click(screen.getByText('deny'));
    expect(mockMutateAsync).toHaveBeenCalledWith({
      requestIdKey: 'r1',
      request: { status: 'Denied', note: 'no' },
    });
  });

  it('close in modal clears selection (modal unmounts)', async () => {
    mockUsePendingRequests.mockReturnValue({
      data: [
        {
          idKey: 'r1',
          requester: { fullName: 'Alice' },
          createdDateTime: '2026-04-01T10:00:00Z',
          requestNote: '',
        },
      ],
      isLoading: false,
    });
    const user = userEvent.setup();
    render(<PendingRequestsList groupIdKey="g1" />);
    await user.click(screen.getByText('Alice'));
    expect(screen.getByTestId('review-modal')).toBeInTheDocument();
    await user.click(screen.getByText('close'));
    expect(screen.queryByTestId('review-modal')).toBeNull();
  });

  it('uses empty list fallback when data is undefined', () => {
    mockUsePendingRequests.mockReturnValue({ data: undefined, isLoading: false });
    render(<PendingRequestsList groupIdKey="g1" />);
    expect(screen.getByText(/no pending requests/i)).toBeInTheDocument();
  });
});
