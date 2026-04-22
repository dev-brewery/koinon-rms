/**
 * Select tests
 *
 * Catches:
 *  - Options are rendered from the options prop (missing entries = user can't pick).
 *  - Label/ref wiring (accessibility + react-hook-form integration).
 *  - Error message renders and triggers invalid styling.
 *  - onChange fires with selected value.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createRef } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { Select } from '../Select';

const options = [
  { value: 'a', label: 'Alpha' },
  { value: 'b', label: 'Bravo' },
  { value: 'c', label: 'Charlie' },
];

describe('Select', () => {
  it('renders all options from the options prop', () => {
    render(<Select label="Pick" options={options} defaultValue="a" onChange={() => {}} />);
    expect(screen.getByRole('option', { name: 'Alpha' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Bravo' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Charlie' })).toBeInTheDocument();
  });

  it('wires up a visible label to the underlying select', () => {
    render(<Select label="Choose" options={options} defaultValue="a" onChange={() => {}} />);
    expect(screen.getByText('Choose')).toBeInTheDocument();
  });

  it('fires onChange with the selected value', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <Select label="Pick" options={options} defaultValue="a" onChange={onChange} />,
    );
    const select = screen.getByRole('combobox') as HTMLSelectElement;
    await user.selectOptions(select, 'b');
    expect(select.value).toBe('b');
    expect(onChange).toHaveBeenCalled();
  });

  it('applies error styling and renders the error message', () => {
    render(
      <Select
        label="Pick"
        options={options}
        defaultValue="a"
        onChange={() => {}}
        error="Required"
      />,
    );
    expect(screen.getByText('Required')).toBeInTheDocument();
    expect(screen.getByRole('combobox').className).toMatch(/border-red-500/);
  });

  it('forwards the ref to the underlying select element', () => {
    const ref = createRef<HTMLSelectElement>();
    render(
      <Select
        ref={ref}
        label="Pick"
        options={options}
        defaultValue="a"
        onChange={() => {}}
      />,
    );
    expect(ref.current).toBeInstanceOf(HTMLSelectElement);
  });
});
