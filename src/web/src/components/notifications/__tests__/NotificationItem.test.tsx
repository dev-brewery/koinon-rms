/**
 * NotificationItem tests
 *
 * Catches:
 *  - Clicking an unread notification calls onMarkAsRead + optional onClick.
 *  - Clicking a read notification does NOT call onMarkAsRead.
 *  - Delete button calls onDelete and stops click propagation.
 *  - Unread notifications show the "New" badge; read ones do not.
 *  - Border-left colour depends on notificationType (regression category:
 *    missing type → wrong visual indicator).
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { NotificationItem } from '../NotificationItem';
import { NotificationType, type NotificationDto } from '@/types/notification';

function notif(overrides: Partial<NotificationDto> = {}): NotificationDto {
  return {
    idKey: 'N1',
    guid: '00000000-0000-0000-0000-000000000001',
    notificationType: NotificationType.CheckinAlert,
    title: 'Hello',
    message: 'World',
    isRead: false,
    readDateTime: null,
    actionUrl: null,
    metadataJson: null,
    createdDateTime: new Date().toISOString(),
    ...overrides,
  };
}

describe('NotificationItem', () => {
  it('marks unread notification as read on click', async () => {
    const user = userEvent.setup();
    const onMarkAsRead = vi.fn();
    const onClick = vi.fn();

    render(
      <NotificationItem
        notification={notif({ isRead: false })}
        onMarkAsRead={onMarkAsRead}
        onDelete={vi.fn()}
        onClick={onClick}
      />,
    );

    await user.click(screen.getByText('Hello'));
    expect(onMarkAsRead).toHaveBeenCalledWith('N1');
    expect(onClick).toHaveBeenCalled();
  });

  it('does NOT call onMarkAsRead for already-read notification', async () => {
    const user = userEvent.setup();
    const onMarkAsRead = vi.fn();

    render(
      <NotificationItem
        notification={notif({ isRead: true })}
        onMarkAsRead={onMarkAsRead}
        onDelete={vi.fn()}
      />,
    );

    await user.click(screen.getByText('Hello'));
    expect(onMarkAsRead).not.toHaveBeenCalled();
  });

  it('delete button calls onDelete without triggering the outer click handler', async () => {
    const user = userEvent.setup();
    const onDelete = vi.fn();
    const onMarkAsRead = vi.fn();

    render(
      <NotificationItem
        notification={notif({ isRead: false })}
        onMarkAsRead={onMarkAsRead}
        onDelete={onDelete}
      />,
    );

    await user.click(screen.getByRole('button', { name: /delete notification/i }));
    expect(onDelete).toHaveBeenCalledWith('N1');
    // stopPropagation prevents the outer click from firing the mark-as-read path.
    expect(onMarkAsRead).not.toHaveBeenCalled();
  });

  it('shows "New" badge for unread notifications', () => {
    render(
      <NotificationItem
        notification={notif({ isRead: false })}
        onMarkAsRead={vi.fn()}
        onDelete={vi.fn()}
      />,
    );
    expect(screen.getByText('New')).toBeInTheDocument();
  });

  it('hides "New" badge for read notifications', () => {
    render(
      <NotificationItem
        notification={notif({ isRead: true })}
        onMarkAsRead={vi.fn()}
        onDelete={vi.fn()}
      />,
    );
    expect(screen.queryByText('New')).toBeNull();
  });

  it.each([
    [NotificationType.CheckinAlert, 'border-l-blue-500'],
    [NotificationType.CommunicationStatus, 'border-l-purple-500'],
    [NotificationType.SystemAlert, 'border-l-yellow-500'],
    [NotificationType.MembershipRequest, 'border-l-green-500'],
    [NotificationType.FollowUp, 'border-l-orange-500'],
  ])('renders correct border for type %i', (type, expectedClass) => {
    const { container } = render(
      <NotificationItem
        notification={notif({ notificationType: type, isRead: false })}
        onMarkAsRead={vi.fn()}
        onDelete={vi.fn()}
      />,
    );
    expect(container.querySelector(`.${expectedClass}`)).not.toBeNull();
  });
});
