import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import userEvent from '@testing-library/user-event';
import { PhoneSearch } from '../PhoneSearch';

/**
 * Component Tests: PhoneSearch
 * Tests phone input, validation, and search functionality
 *
 * ASSUMPTIONS:
 * - Component accepts phone string (10 digits)
 * - Formats phone number automatically
 * - Validates on blur or submit
 * - Calls onSearch callback with valid phone
 * - Shows loading state during search
 */

describe('PhoneSearch', () => {
  const mockOnSearch = vi.fn();

  beforeEach(() => {
    mockOnSearch.mockClear();
  });

  it('should render phone input field', () => {
    render(<PhoneSearch onSearch={mockOnSearch} />);

    expect(screen.getByTestId('phone-input')).toBeInTheDocument();
    expect(screen.getByLabelText(/phone/i)).toBeInTheDocument();
  });

  it('should auto-focus phone input on mount', () => {
    render(<PhoneSearch onSearch={mockOnSearch} />);

    const input = screen.getByTestId('phone-input') as HTMLInputElement;
    expect(input).toHaveFocus();
  });

  it('should accept numeric input', async () => {
    const user = userEvent.setup();
    render(<PhoneSearch onSearch={mockOnSearch} />);

    const input = screen.getByTestId('phone-input') as HTMLInputElement;
    await user.type(input, '5551234567');

    expect(input).toHaveValue('5551234567');
  });

  it('should call onSearch with valid phone number', async () => {
    const user = userEvent.setup();
    render(<PhoneSearch onSearch={mockOnSearch} />);

    const input = screen.getByTestId('phone-input') as HTMLInputElement;
    const submitButton = screen.getByRole('button', { name: /search|find/i });

    await user.type(input, '5551234567');
    await user.click(submitButton);

    expect(mockOnSearch).toHaveBeenCalledWith('5551234567');
  });

  it('should submit on Enter key press', async () => {
    const user = userEvent.setup();
    render(<PhoneSearch onSearch={mockOnSearch} />);

    const input = screen.getByTestId('phone-input') as HTMLInputElement;

    await user.type(input, '5551234567');
    await user.keyboard('{Enter}');

    expect(mockOnSearch).toHaveBeenCalledWith('5551234567');
  });

  it('should validate phone length on submit', async () => {
    const user = userEvent.setup();
    render(<PhoneSearch onSearch={mockOnSearch} />);

    const input = screen.getByTestId('phone-input') as HTMLInputElement;
    const submitButton = screen.getByRole('button', { name: /search|find/i });

    await user.type(input, '555123');
    await user.click(submitButton);

    expect(screen.getByText(/10 digits|complete phone/i)).toBeInTheDocument();
    expect(mockOnSearch).not.toHaveBeenCalled();
  });

  it('should show loading state during search', async () => {
    render(<PhoneSearch onSearch={mockOnSearch} isLoading={true} />);

    const submitButton = screen.getByRole('button', { name: /search|find/i });

    expect(submitButton).toBeDisabled();
    expect(screen.getByText(/searching|loading/i)).toBeInTheDocument();
  });

  it('should display error message when provided', () => {
    const errorMessage = 'Family not found';
    render(<PhoneSearch onSearch={mockOnSearch} error={errorMessage} />);

    expect(screen.getByText(errorMessage)).toBeInTheDocument();
    expect(screen.getByRole('alert')).toBeInTheDocument();
  });

  it('should have large touch targets (min 48px)', () => {
    render(<PhoneSearch onSearch={mockOnSearch} />);

    const submitButton = screen.getByRole('button', { name: /search|find/i });

    expect(submitButton).toHaveClass('min-h-[48px]');
  });

  it('should have proper ARIA attributes', () => {
    render(<PhoneSearch onSearch={mockOnSearch} />);

    const input = screen.getByTestId('phone-input') as HTMLInputElement;

    expect(input).toHaveAttribute('aria-label');
    expect(input).toHaveAttribute('type', 'tel');
    expect(input).toHaveAttribute('inputMode', 'numeric');
  });
});
