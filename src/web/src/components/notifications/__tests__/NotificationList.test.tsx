/**
 * NotificationList tests
 *
 * Branches:
 *   - Loading -> "Loading notifications..." text.
 *   - Error -> "Failed to load notifications" panel.
 *   - Empty state with unreadOnly toggle changes title/description.
 *   - Populated state renders one NotificationItem per item.
 *   - Click with onNotificationClick prefers the prop callback.
 *   - Click without prop navigates to actionUrl (if present).
 *   - Mark-as-read and delete handlers call mutations with idKey.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { MemoryRouter } from 'react-router-dom';

const mockUseNotifications = vi.fn();
const mockMarkMutate = vi.fn();
const mockDeleteMutate = vi.fn();
const mockNavigate = vi.fn();

vi.mock('@/hooks/useNotifications', () => ({
  useNotifications: (unreadOnly?: boolean, limit?: number) =>
    mockUseNotifications(unreadOnly, limit),
  useMarkAsRead: () => ({ mutate: mockMarkMutate }),
  useDeleteNotification: () => ({ mutate: mockDeleteMutate }),
}));

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>(
    'react-router-dom'
  );
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

// Replace NotificationItem with a test-friendly stub so we can drive it.
vi.mock('../NotificationItem', () => ({
  NotificationItem: ({
    notification,
    onMarkAsRead,
    onDelete,
    onClick,
  }: {
    notification: { idKey: string; title: string };
    onMarkAsRead: (id: string) => void;
    onDelete: (id: string) => void;
    onClick: (n: unknown) => void;
  }) => (
    <div data-testid={`notif-${notification.idKey}`}>
      <span>{notification.title}</span>
      <button onClick={() => onClick(notification)}>click</button>
      <button onClick={() => onMarkAsRead(notification.idKey)}>read</button>
      <button onClick={() => onDelete(notification.idKey)}>delete</button>
    </div>
  ),
}));

import { NotificationList } from '../NotificationList';

const wrap = (ui: React.ReactElement) => (
  <MemoryRouter>{ui}</MemoryRouter>
);

describe('NotificationList', () => {
  beforeEach(() => {
    mockUseNotifications.mockReset();
    mockMarkMutate.mockReset();
    mockDeleteMutate.mockReset();
    mockNavigate.mockReset();
  });
  afterEach(() => vi.clearAllMocks());

  it('renders loading state', () => {
    mockUseNotifications.mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
    });
    render(wrap(<NotificationList />));
    expect(screen.getByText(/loading notifications/i)).toBeInTheDocument();
  });

  it('renders error state', () => {
    mockUseNotifications.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error('x'),
    });
    render(wrap(<NotificationList />));
    expect(screen.getByText(/failed to load notifications/i)).toBeInTheDocument();
  });

  it('renders "No notifications" empty state when unreadOnly=false', () => {
    mockUseNotifications.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });
    render(wrap(<NotificationList />));
    expect(screen.getByText(/^no notifications$/i)).toBeInTheDocument();
    expect(screen.getByText(/don't have any notifications yet/i)).toBeInTheDocument();
  });

  it('renders "No unread notifications" empty state when unreadOnly=true', () => {
    mockUseNotifications.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });
    render(wrap(<NotificationList unreadOnly />));
    expect(screen.getByText(/no unread notifications/i)).toBeInTheDocument();
  });

  it('renders NotificationItem per notification', () => {
    mockUseNotifications.mockReturnValue({
      data: [
        { idKey: 'n1', title: 'First' },
        { idKey: 'n2', title: 'Second' },
      ],
      isLoading: false,
      error: null,
    });
    render(wrap(<NotificationList />));
    expect(screen.getByTestId('notif-n1')).toBeInTheDocument();
    expect(screen.getByTestId('notif-n2')).toBeInTheDocument();
  });

  it('prefers onNotificationClick prop callback when provided', async () => {
    mockUseNotifications.mockReturnValue({
      data: [{ idKey: 'n1', title: 'First', actionUrl: '/somewhere' }],
      isLoading: false,
      error: null,
    });
    const user = userEvent.setup();
    const onNotificationClick = vi.fn();
    render(wrap(<NotificationList onNotificationClick={onNotificationClick} />));
    await user.click(screen.getByText('click'));
    expect(onNotificationClick).toHaveBeenCalled();
    expect(mockNavigate).not.toHaveBeenCalled();
  });

  it('navigates to actionUrl when no callback is provided', async () => {
    mockUseNotifications.mockReturnValue({
      data: [{ idKey: 'n1', title: 'First', actionUrl: '/target' }],
      isLoading: false,
      error: null,
    });
    const user = userEvent.setup();
    render(wrap(<NotificationList />));
    await user.click(screen.getByText('click'));
    expect(mockNavigate).toHaveBeenCalledWith('/target');
  });

  it('does nothing on click when no callback and no actionUrl', async () => {
    mockUseNotifications.mockReturnValue({
      data: [{ idKey: 'n1', title: 'First' }],
      isLoading: false,
      error: null,
    });
    const user = userEvent.setup();
    render(wrap(<NotificationList />));
    await user.click(screen.getByText('click'));
    expect(mockNavigate).not.toHaveBeenCalled();
  });

  it('handleMarkAsRead invokes the markAsRead mutation', async () => {
    mockUseNotifications.mockReturnValue({
      data: [{ idKey: 'n1', title: 'First' }],
      isLoading: false,
      error: null,
    });
    const user = userEvent.setup();
    render(wrap(<NotificationList />));
    await user.click(screen.getByText('read'));
    expect(mockMarkMutate).toHaveBeenCalledWith('n1');
  });

  it('handleDelete invokes the delete mutation', async () => {
    mockUseNotifications.mockReturnValue({
      data: [{ idKey: 'n1', title: 'First' }],
      isLoading: false,
      error: null,
    });
    const user = userEvent.setup();
    render(wrap(<NotificationList />));
    await user.click(screen.getByText('delete'));
    expect(mockDeleteMutate).toHaveBeenCalledWith('n1');
  });

  it('passes unreadOnly + limit to the hook', () => {
    mockUseNotifications.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });
    render(wrap(<NotificationList unreadOnly limit={5} />));
    expect(mockUseNotifications).toHaveBeenCalledWith(true, 5);
  });
});
