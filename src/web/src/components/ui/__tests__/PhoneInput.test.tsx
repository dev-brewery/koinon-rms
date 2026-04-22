/**
 * PhoneInput tests
 *
 * Catches:
 *  - Digits-only output contract: onChange always receives digits, not the formatted string.
 *  - Progressive formatting as the user types (3 → 6 → 10 digits).
 *  - Caps at 10 digits even when the user types more.
 *  - Strips non-digit input characters.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { PhoneInput } from '../PhoneInput';

function Controlled({ onChange }: { onChange?: (v: string) => void }) {
  const [v, setV] = useState('');
  return (
    <PhoneInput
      label="Phone"
      value={v}
      onChange={(next) => {
        setV(next);
        onChange?.(next);
      }}
    />
  );
}

describe('PhoneInput', () => {
  it('formats progressively and emits digits-only values', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<Controlled onChange={onChange} />);

    const input = screen.getByPlaceholderText('(555) 123-4567') as HTMLInputElement;

    await user.type(input, '5551234567');

    // Last emitted value is digits-only, 10 chars.
    expect(onChange).toHaveBeenLastCalledWith('5551234567');
    // Displayed value is formatted.
    expect(input.value).toBe('(555) 123-4567');
  });

  it('formats after 3 digits as "(XXX) "', async () => {
    const user = userEvent.setup();
    render(<Controlled />);
    const input = screen.getByPlaceholderText('(555) 123-4567') as HTMLInputElement;
    await user.type(input, '555');
    expect(input.value).toBe('555');
    await user.type(input, '1');
    expect(input.value).toBe('(555) 1');
  });

  it('formats after 6 digits as "(XXX) XXX-"', async () => {
    const user = userEvent.setup();
    render(<Controlled />);
    const input = screen.getByPlaceholderText('(555) 123-4567') as HTMLInputElement;
    await user.type(input, '555123');
    expect(input.value).toBe('(555) 123');
    await user.type(input, '4');
    expect(input.value).toBe('(555) 123-4');
  });

  it('caps input at 10 digits (extra characters ignored)', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<Controlled onChange={onChange} />);
    const input = screen.getByPlaceholderText('(555) 123-4567') as HTMLInputElement;

    await user.type(input, '55512345679999');
    expect(input.value).toBe('(555) 123-4567');
    expect(onChange).toHaveBeenLastCalledWith('5551234567');
  });

  it('strips non-digit characters from input', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<Controlled onChange={onChange} />);
    const input = screen.getByPlaceholderText('(555) 123-4567') as HTMLInputElement;

    await user.type(input, 'abc555-def');
    expect(onChange).toHaveBeenLastCalledWith('555');
    expect(input.value).toBe('555');
  });

  it('renders with tel input type and a placeholder', () => {
    render(<Controlled />);
    const input = screen.getByPlaceholderText('(555) 123-4567') as HTMLInputElement;
    expect(input.type).toBe('tel');
    expect(input.placeholder).toBe('(555) 123-4567');
  });
});
