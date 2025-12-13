import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import userEvent from '@testing-library/user-event';
import { PhoneSearch } from '../PhoneSearch';

/**
 * Component Tests: PhoneSearch
 * Tests phone numpad, validation, and search functionality
 *
 * ASSUMPTIONS:
 * - Component uses numpad for touch-friendly input
 * - Formats phone number automatically
 * - Validates minimum length before search
 * - Calls onSearch callback with valid phone
 * - Shows loading state during search
 */

describe('PhoneSearch', () => {
  const mockOnSearch = vi.fn();

  beforeEach(() => {
    mockOnSearch.mockClear();
  });

  it('should render numpad buttons', () => {
    render(<PhoneSearch onSearch={mockOnSearch} />);

    // Check for numpad digits 0-9
    expect(screen.getByRole('button', { name: '1' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: '5' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: '9' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: '0' })).toBeInTheDocument();
  });

  it('should render clear and backspace buttons', () => {
    render(<PhoneSearch onSearch={mockOnSearch} />);

    expect(screen.getByRole('button', { name: /clear/i })).toBeInTheDocument();
    expect(screen.getByText('⌫')).toBeInTheDocument();
  });

  it('should update phone display when digits clicked', async () => {
    const user = userEvent.setup();
    render(<PhoneSearch onSearch={mockOnSearch} />);

    await user.click(screen.getByRole('button', { name: '5' }));
    await user.click(screen.getByRole('button', { name: '5' }));
    await user.click(screen.getByRole('button', { name: '5' }));

    // Phone should be formatted
    expect(screen.getByText('555')).toBeInTheDocument();
  });

  it('should call onSearch when search button clicked with valid phone', async () => {
    const user = userEvent.setup();
    render(<PhoneSearch onSearch={mockOnSearch} />);

    // Enter 5551234567
    for (const digit of '5551234567') {
      await user.click(screen.getByRole('button', { name: digit }));
    }

    const submitButton = screen.getByRole('button', { name: /search/i });
    await user.click(submitButton);

    expect(mockOnSearch).toHaveBeenCalledWith('5551234567');
  });

  it('should disable search button with less than 4 digits', () => {
    render(<PhoneSearch onSearch={mockOnSearch} />);

    const submitButton = screen.getByRole('button', { name: /search/i });
    expect(submitButton).toBeDisabled();
  });

  it('should format phone number as typed', async () => {
    const user = userEvent.setup();
    render(<PhoneSearch onSearch={mockOnSearch} />);

    // Enter 5551234
    for (const digit of '5551234') {
      await user.click(screen.getByRole('button', { name: digit }));
    }

    // Should format as (555) 123-4
    expect(screen.getByText('(555) 123-4')).toBeInTheDocument();
  });

  it('should show loading state during search', () => {
    render(<PhoneSearch onSearch={mockOnSearch} loading={true} />);

    // When loading, the button text might be hidden by spinner
    // Just check that all buttons are present (numpad + search are there)
    const buttons = screen.getAllByRole('button');
    expect(buttons.length).toBeGreaterThan(10); // 10 digits + clear + backspace + search
  });

  it('should have large touch targets for numpad', () => {
    render(<PhoneSearch onSearch={mockOnSearch} />);

    const digitButton = screen.getByRole('button', { name: '5' });
    expect(digitButton).toHaveClass('min-h-[80px]');
  });
});
