/**
 * FamilySearch tests
 *
 * Catches:
 *  - Submit only fires when name length >= 2 (prevents abusive 1-char queries).
 *  - Button is disabled until the threshold is met.
 *  - onInputChange fires with a boolean reflecting non-empty input.
 *  - Loading prop propagates to the Button's loading state.
 *  - Submitted value is trimmed.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { FamilySearch } from '../FamilySearch';

describe('FamilySearch', () => {
  it('disables submit until 2+ chars are entered', async () => {
    const user = userEvent.setup();
    const onSearch = vi.fn();
    render(<FamilySearch onSearch={onSearch} />);

    const button = screen.getByRole('button', { name: /find family/i });
    expect(button).toBeDisabled();

    const input = screen.getByPlaceholderText('Last name...') as HTMLInputElement;
    await user.type(input, 'a');
    expect(button).toBeDisabled();

    await user.type(input, 'b');
    expect(button).not.toBeDisabled();
  });

  it('invokes onSearch with the trimmed name on submit', async () => {
    const user = userEvent.setup();
    const onSearch = vi.fn();
    render(<FamilySearch onSearch={onSearch} />);

    const input = screen.getByPlaceholderText('Last name...') as HTMLInputElement;
    await user.type(input, '   Smith   ');
    await user.click(screen.getByRole('button', { name: /find family/i }));

    expect(onSearch).toHaveBeenCalledWith('Smith');
  });

  it('does NOT invoke onSearch for a 1-char query', async () => {
    const user = userEvent.setup();
    const onSearch = vi.fn();
    render(<FamilySearch onSearch={onSearch} />);

    const input = screen.getByPlaceholderText('Last name...') as HTMLInputElement;
    await user.type(input, 'a');
    // Press Enter to submit the form even with button disabled — fallback safety.
    input.focus();
    await user.keyboard('{Enter}');

    expect(onSearch).not.toHaveBeenCalled();
  });

  it('fires onInputChange with a boolean reflecting non-empty input', async () => {
    const user = userEvent.setup();
    const onInputChange = vi.fn();
    render(<FamilySearch onSearch={vi.fn()} onInputChange={onInputChange} />);

    const input = screen.getByPlaceholderText('Last name...') as HTMLInputElement;
    await user.type(input, 'a');
    expect(onInputChange).toHaveBeenLastCalledWith(true);

    await user.clear(input);
    expect(onInputChange).toHaveBeenLastCalledWith(false);
  });

  it('passes loading=true through to the Button', () => {
    render(<FamilySearch onSearch={vi.fn()} loading />);
    // Button component renders the "Loading..." label when loading.
    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });
});
