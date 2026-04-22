/**
 * PinEntry tests
 *
 * Catches:
 *  - Numpad clicks append digits to the PIN (capped at 6).
 *  - Clear resets the PIN.
 *  - Backspace removes the last digit.
 *  - Submit disabled while pin < 4 and user hasn't edited.
 *  - Submit shows inline validation if too short after editing.
 *  - Successful submit fires onSubmit with the entered PIN.
 *  - Cancel fires onCancel.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { PinEntry } from '../PinEntry';

describe('PinEntry', () => {
  it('disables Submit when pin is empty and user has not edited', () => {
    render(<PinEntry onSubmit={vi.fn()} onCancel={vi.fn()} />);
    expect(screen.getByRole('button', { name: 'Submit' })).toBeDisabled();
  });

  it('appends numpad taps into the PIN (dots show)', async () => {
    const user = userEvent.setup();
    render(<PinEntry onSubmit={vi.fn()} onCancel={vi.fn()} />);

    await user.click(screen.getByRole('button', { name: '1' }));
    await user.click(screen.getByRole('button', { name: '2' }));
    await user.click(screen.getByRole('button', { name: '3' }));
    await user.click(screen.getByRole('button', { name: '4' }));

    const pinDisplay = screen.getByTestId('pin-display');
    expect(pinDisplay.textContent).toMatch(/•{4}/);
  });

  it('caps PIN input at 6 digits (numpad disables after 6)', async () => {
    const user = userEvent.setup();
    render(<PinEntry onSubmit={vi.fn()} onCancel={vi.fn()} />);

    for (let i = 0; i < 6; i++) {
      await user.click(screen.getByRole('button', { name: '1' }));
    }
    // 7th click should be no-op because buttons are disabled.
    expect(screen.getByRole('button', { name: '1' })).toBeDisabled();
  });

  it('Clear resets the PIN', async () => {
    const user = userEvent.setup();
    render(<PinEntry onSubmit={vi.fn()} onCancel={vi.fn()} />);

    await user.click(screen.getByRole('button', { name: '1' }));
    await user.click(screen.getByRole('button', { name: '2' }));
    await user.click(screen.getByRole('button', { name: 'Reset' }));

    const pinDisplay = screen.getByTestId('pin-display');
    // After reset, no • should be visible.
    expect(pinDisplay.textContent).not.toMatch(/•/);
  });

  it('Backspace removes the last digit', async () => {
    const user = userEvent.setup();
    render(<PinEntry onSubmit={vi.fn()} onCancel={vi.fn()} />);

    await user.click(screen.getByRole('button', { name: '1' }));
    await user.click(screen.getByRole('button', { name: '2' }));
    await user.click(screen.getByRole('button', { name: /backspace/i }));

    const pinDisplay = screen.getByTestId('pin-display');
    expect((pinDisplay.textContent ?? '').match(/•/g)?.length ?? 0).toBe(1);
  });

  it('calls onSubmit with the entered pin when it is valid', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(<PinEntry onSubmit={onSubmit} onCancel={vi.fn()} />);

    await user.click(screen.getByRole('button', { name: '1' }));
    await user.click(screen.getByRole('button', { name: '2' }));
    await user.click(screen.getByRole('button', { name: '3' }));
    await user.click(screen.getByRole('button', { name: '4' }));
    await user.click(screen.getByRole('button', { name: 'Submit' }));

    expect(onSubmit).toHaveBeenCalledWith('1234');
  });

  it('shows an inline validation error when Submit is clicked with too-short pin after editing', async () => {
    const user = userEvent.setup();
    render(<PinEntry onSubmit={vi.fn()} onCancel={vi.fn()} />);

    // Force "hasEdited" via backspace click.
    await user.click(screen.getByRole('button', { name: '1' }));
    await user.click(screen.getByRole('button', { name: /backspace/i }));
    await user.click(screen.getByRole('button', { name: '1' }));

    // Now pin="1" with hasEdited=true — Submit is enabled but too short.
    const submit = screen.getByRole('button', { name: 'Submit' });
    expect(submit).not.toBeDisabled();
    await user.click(submit);

    expect(screen.getByText(/please enter at least 4 digits/i)).toBeInTheDocument();
  });

  it('fires onCancel when Cancel is clicked', async () => {
    const user = userEvent.setup();
    const onCancel = vi.fn();
    render(<PinEntry onSubmit={vi.fn()} onCancel={onCancel} />);

    await user.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(onCancel).toHaveBeenCalledOnce();
  });

  it('displays the error prop when provided (server-side error)', () => {
    render(<PinEntry onSubmit={vi.fn()} onCancel={vi.fn()} error="Invalid PIN" />);
    expect(screen.getByText('Invalid PIN')).toBeInTheDocument();
  });
});
