import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import { CheckinConfirmation } from '../CheckinConfirmation';

describe('CheckinConfirmation', () => {
  const mockSelectedMembers = [
    {
      id: 1,
      idKey: 'ABC123',
      firstName: 'John',
      lastName: 'Smith',
      age: 42,
    },
  ];

  it('should display selected members summary', () => {
    render(
      <CheckinConfirmation
        selectedMembers={mockSelectedMembers}
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />
    );

    expect(screen.getByText('John Smith')).toBeInTheDocument();
  });

  it('should show confirm button', () => {
    render(
      <CheckinConfirmation
        selectedMembers={mockSelectedMembers}
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />
    );

    expect(screen.getByRole('button', { name: /confirm|check in/i })).toBeInTheDocument();
  });

  it('should call onConfirm when confirmed', async () => {
    const user = userEvent.setup();
    const mockOnConfirm = vi.fn();

    render(
      <CheckinConfirmation
        selectedMembers={mockSelectedMembers}
        onConfirm={mockOnConfirm}
        onCancel={vi.fn()}
      />
    );

    const confirmButton = screen.getByRole('button', { name: /confirm|check in/i });
    await user.click(confirmButton);

    expect(mockOnConfirm).toHaveBeenCalled();
  });

  it('should call onCancel when cancelled', async () => {
    const user = userEvent.setup();
    const mockOnCancel = vi.fn();

    render(
      <CheckinConfirmation
        selectedMembers={mockSelectedMembers}
        onConfirm={vi.fn()}
        onCancel={mockOnCancel}
      />
    );

    const cancelButton = screen.getByRole('button', { name: /cancel|back/i });
    await user.click(cancelButton);

    expect(mockOnCancel).toHaveBeenCalled();
  });

  it('should show loading state', () => {
    render(
      <CheckinConfirmation
        selectedMembers={mockSelectedMembers}
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
        isLoading={true}
      />
    );

    const confirmButton = screen.getByRole('button', { name: /confirm|check in/i });
    expect(confirmButton).toBeDisabled();
    expect(screen.getByText(/processing|checking in/i)).toBeInTheDocument();
  });

  it('should show success state', () => {
    render(
      <CheckinConfirmation
        selectedMembers={mockSelectedMembers}
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
        isSuccess={true}
      />
    );

    expect(screen.getByTestId('success-message')).toBeInTheDocument();
    expect(screen.getByText(/checked in|success/i)).toBeInTheDocument();
  });

  it('should display error message', () => {
    const errorMessage = 'Check-in failed';
    render(
      <CheckinConfirmation
        selectedMembers={mockSelectedMembers}
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
        error={errorMessage}
      />
    );

    expect(screen.getByText(errorMessage)).toBeInTheDocument();
    expect(screen.getByRole('alert')).toBeInTheDocument();
  });

  it('should show member count', () => {
    const multipleMembers = [
      ...mockSelectedMembers,
      { id: 2, idKey: 'DEF456', firstName: 'Jane', lastName: 'Smith', age: 40 },
    ];

    render(
      <CheckinConfirmation
        selectedMembers={multipleMembers}
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />
    );

    expect(screen.getByText(/2 members?/i)).toBeInTheDocument();
  });

  it('should have large touch targets', () => {
    render(
      <CheckinConfirmation
        selectedMembers={mockSelectedMembers}
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />
    );

    const confirmButton = screen.getByRole('button', { name: /confirm|check in/i });
    expect(confirmButton).toHaveClass('min-h-[48px]');
  });

  it('should have proper ARIA attributes', () => {
    render(
      <CheckinConfirmation
        selectedMembers={mockSelectedMembers}
        onConfirm={vi.fn()}
        onCancel={vi.fn()}
      />
    );

    const confirmButton = screen.getByRole('button', { name: /confirm|check in/i });
    expect(confirmButton).toHaveAttribute('aria-label');
  });
});
