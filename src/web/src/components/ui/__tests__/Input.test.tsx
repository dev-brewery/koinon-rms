/**
 * Input tests
 *
 * Catches:
 *  - Label text renders next to the input.
 *  - Error message renders as text and applies the invalid styling class.
 *  - forwardRef works (calling code needs this for focus / react-hook-form).
 *  - User keystrokes propagate to the onChange prop (no swallowed events).
 *  - Disabled state blocks input.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createRef, useState } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { Input } from '../Input';

describe('Input', () => {
  it('renders the label text when provided', () => {
    render(<Input label="Email" />);
    expect(screen.getByText('Email')).toBeInTheDocument();
  });

  it('omits the label node entirely when no label prop is passed', () => {
    const { container } = render(<Input placeholder="anon" />);
    expect(container.querySelector('label')).toBeNull();
  });

  it('associates label with input via htmlFor/id', () => {
    render(<Input label="Email" />);
    const label = screen.getByText('Email');
    const input = screen.getByRole('textbox');
    expect(label).toHaveAttribute('for', input.id);
  });

  it('uses provided id for label association', () => {
    render(<Input id="my-email" label="Email" />);
    const label = screen.getByText('Email');
    expect(label).toHaveAttribute('for', 'my-email');
    expect(screen.getByRole('textbox')).toHaveAttribute('id', 'my-email');
  });

  it('renders the error message and marks the field with error styling', () => {
    render(<Input label="Email" error="Required" placeholder="x" />);
    expect(screen.getByText('Required')).toBeInTheDocument();
    const input = screen.getByRole('textbox');
    expect(input.className).toMatch(/border-red-500/);
  });

  it('links error message to input via aria-describedby', () => {
    render(<Input label="Email" error="Required" />);
    const input = screen.getByRole('textbox');
    const error = screen.getByText('Required');
    expect(input).toHaveAttribute('aria-invalid', 'true');
    expect(input).toHaveAttribute('aria-describedby', error.id);
    expect(error).toHaveAttribute('role', 'alert');
  });

  it('does not set aria-invalid when no error', () => {
    render(<Input label="Email" />);
    const input = screen.getByRole('textbox');
    expect(input).not.toHaveAttribute('aria-invalid');
    expect(input).not.toHaveAttribute('aria-describedby');
  });

  it('applies neutral border styling when no error is present', () => {
    render(<Input label="Email" placeholder="x" />);
    const input = screen.getByRole('textbox');
    expect(input.className).toMatch(/border-gray-300/);
  });

  it('forwards the ref to the underlying input element', () => {
    const ref = createRef<HTMLInputElement>();
    render(<Input ref={ref} label="f" />);
    expect(ref.current).toBeInstanceOf(HTMLInputElement);
  });

  it('propagates user keystrokes to onChange', async () => {
    const user = userEvent.setup();

    function Controlled() {
      const [v, setV] = useState('');
      return <Input label="Name" value={v} onChange={(e) => setV(e.target.value)} />;
    }

    render(<Controlled />);
    const input = screen.getByRole('textbox') as HTMLInputElement;
    await user.type(input, 'abc');
    expect(input.value).toBe('abc');
  });

  it('calls onChange for each keystroke', async () => {
    const onChange = vi.fn();
    const user = userEvent.setup();
    render(<Input label="x" onChange={onChange} />);
    await user.type(screen.getByRole('textbox'), 'ab');
    expect(onChange).toHaveBeenCalledTimes(2);
  });

  it('respects the disabled prop', async () => {
    const user = userEvent.setup();
    render(<Input label="x" disabled />);
    const input = screen.getByRole('textbox') as HTMLInputElement;
    expect(input.disabled).toBe(true);
    await user.type(input, 'abc');
    expect(input.value).toBe('');
  });
});
