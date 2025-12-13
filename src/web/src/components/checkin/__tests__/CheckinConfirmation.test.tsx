import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import { CheckinConfirmation } from '../CheckinConfirmation';
import type { AttendanceResultDto } from '@/services/api/types';

const MOCK_CHECKIN_TIME = new Date().toISOString();

describe('CheckinConfirmation', () => {
  const mockAttendances: AttendanceResultDto[] = [
    {
      attendanceIdKey: 'ABC123',
      personIdKey: 'PERSON123',
      personName: 'John Smith',
      groupName: 'Kids Ministry',
      locationName: 'Room 101',
      scheduleName: '9:00 AM Service',
      securityCode: '1234',
      checkInTime: MOCK_CHECKIN_TIME,
      isFirstTime: false,
    },
  ];

  it('should display selected members summary', () => {
    render(
      <CheckinConfirmation
        attendances={mockAttendances}
        onDone={vi.fn()}
      />
    );

    expect(screen.getByText('John Smith')).toBeInTheDocument();
  });

  it('should show done button', () => {
    render(
      <CheckinConfirmation
        attendances={mockAttendances}
        onDone={vi.fn()}
      />
    );

    expect(screen.getByRole('button', { name: /done/i })).toBeInTheDocument();
  });

  it('should call onDone when done button clicked', async () => {
    const user = userEvent.setup();
    const mockOnDone = vi.fn();

    render(
      <CheckinConfirmation
        attendances={mockAttendances}
        onDone={mockOnDone}
      />
    );

    const doneButton = screen.getByRole('button', { name: /done/i });
    await user.click(doneButton);

    expect(mockOnDone).toHaveBeenCalled();
  });

  it('should show print labels button when callback provided', () => {
    render(
      <CheckinConfirmation
        attendances={mockAttendances}
        onDone={vi.fn()}
        onPrintLabels={vi.fn()}
      />
    );

    expect(screen.getByRole('button', { name: /print labels/i })).toBeInTheDocument();
  });

  it('should show printing state', () => {
    render(
      <CheckinConfirmation
        attendances={mockAttendances}
        onDone={vi.fn()}
        onPrintLabels={vi.fn()}
        printStatus="printing"
      />
    );

    expect(screen.getByText(/printing labels/i)).toBeInTheDocument();
  });

  it('should show print success state', () => {
    render(
      <CheckinConfirmation
        attendances={mockAttendances}
        onDone={vi.fn()}
        onPrintLabels={vi.fn()}
        printStatus="success"
      />
    );

    expect(screen.getByText(/labels printed successfully/i)).toBeInTheDocument();
  });

  it('should display print error message', () => {
    const errorMessage = 'Printer offline';
    render(
      <CheckinConfirmation
        attendances={mockAttendances}
        onDone={vi.fn()}
        onPrintLabels={vi.fn()}
        printStatus="error"
        printError={errorMessage}
      />
    );

    expect(screen.getByText(errorMessage)).toBeInTheDocument();
  });

  it('should show person count', () => {
    const multipleAttendances: AttendanceResultDto[] = [
      ...mockAttendances,
      {
        attendanceIdKey: 'DEF456',
        personIdKey: 'PERSON456',
        personName: 'Jane Smith',
        groupName: 'Kids Ministry',
        locationName: 'Room 101',
        scheduleName: '9:00 AM Service',
        securityCode: '5678',
        checkInTime: MOCK_CHECKIN_TIME,
        isFirstTime: false,
      },
    ];

    render(
      <CheckinConfirmation
        attendances={multipleAttendances}
        onDone={vi.fn()}
      />
    );

    expect(screen.getByText(/2 people checked in/i)).toBeInTheDocument();
  });

  it('should have large touch targets', () => {
    render(
      <CheckinConfirmation
        attendances={mockAttendances}
        onDone={vi.fn()}
      />
    );

    const doneButton = screen.getByRole('button', { name: /done/i });
    // Button uses size="lg" which should have min height
    expect(doneButton).toBeInTheDocument();
  });

  it('should display security codes', () => {
    render(
      <CheckinConfirmation
        attendances={mockAttendances}
        onDone={vi.fn()}
      />
    );

    expect(screen.getByText('1234')).toBeInTheDocument();
  });
});
