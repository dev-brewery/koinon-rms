/**
 * SecurityCodeVerify tests
 *
 * Catches:
 *  - Person name is displayed (context for the user).
 *  - Correct code fires onVerified after the constant-time delay.
 *  - Wrong code shows an error rather than calling onVerified.
 *  - Cancel fires onCancel.
 */
import { act, render, screen, fireEvent } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { SecurityCodeVerify } from '../SecurityCodeVerify';

afterEach(() => {
  vi.useRealTimers();
});

describe('SecurityCodeVerify', () => {
  it('shows the person name for context', () => {
    render(
      <SecurityCodeVerify
        expectedCode="1234"
        personName="Billy Smith"
        onVerified={vi.fn()}
        onCancel={vi.fn()}
      />,
    );
    expect(screen.getByText('Billy Smith')).toBeInTheDocument();
  });

  it('fires onVerified when the correct code is entered', () => {
    vi.useFakeTimers();
    const onVerified = vi.fn();

    render(
      <SecurityCodeVerify
        expectedCode="1234"
        personName="A"
        onVerified={onVerified}
        onCancel={vi.fn()}
      />,
    );

    // Use fireEvent to avoid userEvent's awaited scheduling interacting with
    // fake timers. fireEvent dispatches synchronously, which is fine here.
    fireEvent.click(screen.getByRole('button', { name: '1' }));
    fireEvent.click(screen.getByRole('button', { name: '2' }));
    fireEvent.click(screen.getByRole('button', { name: '3' }));
    fireEvent.click(screen.getByRole('button', { name: '4' }));

    act(() => {
      vi.advanceTimersByTime(200);
    });

    expect(onVerified).toHaveBeenCalledOnce();
  });

  it('shows an error when the code is incorrect (does not call onVerified)', () => {
    vi.useFakeTimers();
    const onVerified = vi.fn();

    render(
      <SecurityCodeVerify
        expectedCode="1234"
        personName="A"
        onVerified={onVerified}
        onCancel={vi.fn()}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: '9' }));
    fireEvent.click(screen.getByRole('button', { name: '9' }));
    fireEvent.click(screen.getByRole('button', { name: '9' }));
    fireEvent.click(screen.getByRole('button', { name: '9' }));

    act(() => {
      vi.advanceTimersByTime(200);
    });

    expect(onVerified).not.toHaveBeenCalled();
    expect(screen.getByText(/incorrect security code/i)).toBeInTheDocument();
  });

  it('Cancel fires onCancel', () => {
    const onCancel = vi.fn();
    render(
      <SecurityCodeVerify
        expectedCode="1234"
        personName="A"
        onVerified={vi.fn()}
        onCancel={onCancel}
      />,
    );
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(onCancel).toHaveBeenCalledOnce();
  });

  it('Clear button resets the entry and re-enables digit buttons', () => {
    render(
      <SecurityCodeVerify
        expectedCode="1234"
        personName="A"
        onVerified={vi.fn()}
        onCancel={vi.fn()}
      />,
    );
    fireEvent.click(screen.getByRole('button', { name: '1' }));
    fireEvent.click(screen.getByRole('button', { name: 'Clear' }));

    // After clear, "1" should still be enabled.
    expect(screen.getByRole('button', { name: '1' })).not.toBeDisabled();
  });
});
