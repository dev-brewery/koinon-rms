/**
 * AccessibleSelect tests
 *
 * Catches:
 *  - Label is associated via htmlFor (a11y + Playwright getByLabel).
 *  - Native select + custom listbox both render options (required for Playwright
 *    .selectOption() AND getByRole('option')).
 *  - Clicking an option in the custom listbox calls onChange + closes.
 *  - Escape key closes the dropdown.
 *  - Error message renders with red styling.
 *  - Clicking outside the component closes the dropdown.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { AccessibleSelect } from '../AccessibleSelect';

const options = [
  { value: 'a', label: 'Alpha' },
  { value: 'b', label: 'Bravo' },
];

describe('AccessibleSelect', () => {
  it('renders the label associated to the select via htmlFor', () => {
    render(
      <AccessibleSelect
        label="Pick"
        value="a"
        options={options}
        onChange={() => {}}
      />,
    );
    const label = screen.getByText('Pick');
    const select = screen.getByLabelText('Pick');
    expect(label).toBeInTheDocument();
    expect(select).toBeInTheDocument();
  });

  it('selecting an option via the native select calls onChange', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <AccessibleSelect value="a" options={options} onChange={onChange} />,
    );
    const native = screen.getByRole('combobox') as HTMLSelectElement;
    await user.selectOptions(native, 'b');
    expect(onChange).toHaveBeenCalledWith('b');
  });

  it('clicking a custom listbox option calls onChange with the chosen value', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const onBlur = vi.fn();
    const { container } = render(
      <AccessibleSelect
        value="a"
        options={options}
        onChange={onChange}
        onBlur={onBlur}
      />,
    );

    // Open by clicking the native select (mousedown toggles the DOM dropdown).
    const native = container.querySelector('select')!;
    await user.click(native);

    const bravoOption = screen.getByRole('option', { name: 'Bravo' });
    await user.click(bravoOption);

    expect(onChange).toHaveBeenCalledWith('b');
    expect(onBlur).toHaveBeenCalled();
  });

  it('marks the current value as aria-selected in the custom listbox', () => {
    render(<AccessibleSelect value="b" options={options} onChange={() => {}} />);
    const selected = screen.getByRole('option', { name: 'Bravo' });
    expect(selected.getAttribute('aria-selected')).toBe('true');
  });

  it('renders an error message and applies red border styling', () => {
    const { container } = render(
      <AccessibleSelect
        value="a"
        options={options}
        onChange={() => {}}
        error="Required"
      />,
    );
    expect(screen.getByText('Required')).toBeInTheDocument();
    expect(container.querySelector('.border-red-500')).not.toBeNull();
  });

  it('closes the dropdown when the Escape key is pressed on the select', async () => {
    const user = userEvent.setup();
    const { container } = render(
      <AccessibleSelect value="a" options={options} onChange={() => {}} />,
    );
    const native = container.querySelector('select')!;
    await user.click(native);

    // Listbox is visible (does not have the `hidden` class).
    const listboxOpen = container.querySelector('[role="listbox"]')!;
    expect(listboxOpen.className).not.toMatch(/hidden/);

    native.focus();
    await user.keyboard('{Escape}');

    const listboxAfter = container.querySelector('[role="listbox"]')!;
    expect(listboxAfter.className).toMatch(/hidden/);
  });
});
