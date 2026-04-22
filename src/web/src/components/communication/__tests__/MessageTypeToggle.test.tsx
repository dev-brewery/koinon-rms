/**
 * MessageTypeToggle tests
 *
 * Catches:
 *  - The currently selected option has primary background (visible state).
 *  - Clicking the other option fires onChange with that value.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { MessageTypeToggle } from '../MessageTypeToggle';

describe('MessageTypeToggle', () => {
  it('marks Email as active when value=Email', () => {
    render(<MessageTypeToggle value="Email" onChange={vi.fn()} />);
    const emailBtn = screen.getByRole('button', { name: /email/i });
    const smsBtn = screen.getByRole('button', { name: /sms/i });
    expect(emailBtn.className).toMatch(/bg-primary-600/);
    expect(smsBtn.className).not.toMatch(/bg-primary-600/);
  });

  it('marks SMS as active when value=Sms', () => {
    render(<MessageTypeToggle value="Sms" onChange={vi.fn()} />);
    const emailBtn = screen.getByRole('button', { name: /email/i });
    const smsBtn = screen.getByRole('button', { name: /sms/i });
    expect(smsBtn.className).toMatch(/bg-primary-600/);
    expect(emailBtn.className).not.toMatch(/bg-primary-600/);
  });

  it('fires onChange with the clicked option', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<MessageTypeToggle value="Email" onChange={onChange} />);
    await user.click(screen.getByRole('button', { name: /sms/i }));
    expect(onChange).toHaveBeenCalledWith('Sms');
  });
});
