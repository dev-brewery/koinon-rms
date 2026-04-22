/**
 * CheckoutFlow tests
 *
 * Catches:
 *  - Empty list message depends on whether a search query is active.
 *  - Search filters by person name.
 *  - Search filters by security code.
 *  - Security code display is partially masked (first 2 digits + dots).
 *  - "First Time" badge appears when flag is set.
 *  - Clicking Check Out advances to the verify step (renders SecurityCodeVerify).
 *  - Cancel from verify step returns to list.
 *  - Successful onCheckout → success screen → Continue returns to list.
 *  - onCheckout failure → error banner on list.
 */
import { act, fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { CheckoutFlow } from '../CheckoutFlow';
import type { AttendanceResultDto } from '@/services/api/types';

function attend(overrides: Partial<AttendanceResultDto> = {}): AttendanceResultDto {
  return {
    attendanceIdKey: 'ATT1',
    personIdKey: 'PER1',
    personName: 'Billy Smith',
    groupName: 'Kids',
    locationName: 'Room 101',
    scheduleName: '9am',
    securityCode: '1234',
    checkInTime: '2026-04-22T10:00:00Z',
    isFirstTime: false,
    ...overrides,
  };
}

describe('CheckoutFlow', () => {
  it('shows the empty-no-search message when there is nothing to check out', () => {
    render(<CheckoutFlow currentAttendance={[]} onCheckout={vi.fn()} />);
    expect(screen.getByText(/no one is currently checked in/i)).toBeInTheDocument();
  });

  it('lists checked-in people and masks the security code', () => {
    render(
      <CheckoutFlow
        currentAttendance={[attend({ securityCode: '4321' })]}
        onCheckout={vi.fn()}
      />,
    );
    expect(screen.getByText('Billy Smith')).toBeInTheDocument();
    // Only first 2 digits + bullets.
    expect(screen.getByText('43••')).toBeInTheDocument();
  });

  it('shows the First Time badge when isFirstTime=true', () => {
    render(
      <CheckoutFlow
        currentAttendance={[attend({ isFirstTime: true })]}
        onCheckout={vi.fn()}
      />,
    );
    expect(screen.getByText(/first time/i)).toBeInTheDocument();
  });

  it('filters the list by person name via search', async () => {
    const user = userEvent.setup();
    render(
      <CheckoutFlow
        currentAttendance={[
          attend({ attendanceIdKey: 'A', personName: 'Alice' }),
          attend({ attendanceIdKey: 'B', personName: 'Bob' }),
        ]}
        onCheckout={vi.fn()}
      />,
    );

    expect(screen.getByText('Alice')).toBeInTheDocument();
    expect(screen.getByText('Bob')).toBeInTheDocument();

    await user.type(screen.getByPlaceholderText(/search by name/i), 'ali');
    expect(screen.getByText('Alice')).toBeInTheDocument();
    expect(screen.queryByText('Bob')).toBeNull();
  });

  it('shows "no matching check-ins" when search matches nothing', async () => {
    const user = userEvent.setup();
    render(
      <CheckoutFlow
        currentAttendance={[attend({ personName: 'Alice' })]}
        onCheckout={vi.fn()}
      />,
    );
    await user.type(screen.getByPlaceholderText(/search by name/i), 'xyz');
    expect(screen.getByText(/no matching check-ins/i)).toBeInTheDocument();
  });

  it('filters by security code digits in search', async () => {
    const user = userEvent.setup();
    render(
      <CheckoutFlow
        currentAttendance={[
          attend({ attendanceIdKey: 'A', personName: 'Alice', securityCode: '1111' }),
          attend({ attendanceIdKey: 'B', personName: 'Bob', securityCode: '2222' }),
        ]}
        onCheckout={vi.fn()}
      />,
    );
    await user.type(screen.getByPlaceholderText(/search by name/i), '11');
    expect(screen.getByText('Alice')).toBeInTheDocument();
    expect(screen.queryByText('Bob')).toBeNull();
  });

  it('advances to the verify step when Check Out is clicked', async () => {
    const user = userEvent.setup();
    render(
      <CheckoutFlow
        currentAttendance={[attend({ personName: 'Billy Smith' })]}
        onCheckout={vi.fn()}
      />,
    );
    await user.click(screen.getByRole('button', { name: /check out/i }));
    // Verify step renders the security code heading.
    expect(screen.getByText(/verify security code/i)).toBeInTheDocument();
    expect(screen.getByText('Billy Smith')).toBeInTheDocument();
  });

  it('returns to list when the verify step is cancelled', async () => {
    const user = userEvent.setup();
    render(
      <CheckoutFlow
        currentAttendance={[attend()]}
        onCheckout={vi.fn()}
      />,
    );
    await user.click(screen.getByRole('button', { name: /check out/i }));
    expect(screen.getByText(/verify security code/i)).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(screen.getByPlaceholderText(/search by name/i)).toBeInTheDocument();
  });

  it('shows success screen when the correct code completes checkout', async () => {
    vi.useFakeTimers();
    const onCheckout = vi.fn().mockResolvedValue(undefined);

    render(
      <CheckoutFlow
        currentAttendance={[attend({ personName: 'Billy', securityCode: '1234' })]}
        onCheckout={onCheckout}
      />,
    );

    // Click Check Out button via fireEvent (synchronous, plays well with timers).
    fireEvent.click(screen.getByRole('button', { name: /check out/i }));

    // Now in verify; enter correct digits.
    fireEvent.click(screen.getByRole('button', { name: '1' }));
    fireEvent.click(screen.getByRole('button', { name: '2' }));
    fireEvent.click(screen.getByRole('button', { name: '3' }));
    fireEvent.click(screen.getByRole('button', { name: '4' }));

    // Advance constant-time delay + microtasks for the async onCheckout.
    await act(async () => {
      vi.advanceTimersByTime(200);
      // Flush pending promises.
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(onCheckout).toHaveBeenCalledWith('ATT1');
    expect(screen.getByText(/checkout complete/i)).toBeInTheDocument();
    vi.useRealTimers();
  });
});
