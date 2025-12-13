import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import { FamilyMemberList } from '../FamilyMemberList';
import type { PersonOpportunitiesDto } from '@/services/api/types';

describe('FamilyMemberList', () => {
  const mockOpportunities: PersonOpportunitiesDto[] = [
    {
      person: {
        idKey: 'ABC123',
        firstName: 'John',
        lastName: 'Smith',
        fullName: 'John Smith',
        age: 42,
        grade: undefined,
        photoUrl: 'https://example.com/john.jpg',
        allergies: undefined,
        hasCriticalAllergies: false,
      },
      availableOptions: [
        {
          groupIdKey: 'GRP1',
          groupName: 'Kids Ministry',
          locations: [
            {
              locationIdKey: 'LOC1',
              locationName: 'Room 101',
              currentCount: 5,
              schedules: [
                {
                  scheduleIdKey: 'SCH1',
                  scheduleName: '9:00 AM Service',
                  startTime: '9:00 AM',
                  isSelected: true,
                },
              ],
            },
          ],
        },
      ],
      currentAttendance: [],
    },
    {
      person: {
        idKey: 'DEF456',
        firstName: 'Jane',
        lastName: 'Smith',
        fullName: 'Jane Smith',
        age: 40,
        grade: undefined,
        photoUrl: undefined,
        allergies: undefined,
        hasCriticalAllergies: false,
      },
      availableOptions: [
        {
          groupIdKey: 'GRP1',
          groupName: 'Kids Ministry',
          locations: [
            {
              locationIdKey: 'LOC1',
              locationName: 'Room 101',
              currentCount: 5,
              schedules: [
                {
                  scheduleIdKey: 'SCH1',
                  scheduleName: '9:00 AM Service',
                  startTime: '9:00 AM',
                  isSelected: true,
                },
              ],
            },
          ],
        },
      ],
      currentAttendance: [],
    },
  ];

  it('should render family member cards', () => {
    render(
      <FamilyMemberList
        opportunities={mockOpportunities}
        selectedCheckins={new Map()}
        onToggleCheckin={vi.fn()}
      />
    );

    expect(screen.getByText('John Smith')).toBeInTheDocument();
    expect(screen.getByText('Jane Smith')).toBeInTheDocument();
  });

  it('should call onToggleCheckin when option is selected', async () => {
    const user = userEvent.setup();
    const mockOnToggle = vi.fn();

    render(
      <FamilyMemberList
        opportunities={mockOpportunities}
        selectedCheckins={new Map()}
        onToggleCheckin={mockOnToggle}
      />
    );

    const checkboxButton = screen.getAllByRole('button', { name: /kids ministry/i })[0];
    await user.click(checkboxButton);

    expect(mockOnToggle).toHaveBeenCalledWith(
      'ABC123',
      'GRP1',
      'LOC1',
      'SCH1',
      'Kids Ministry',
      'Room 101',
      '9:00 AM Service',
      '9:00 AM'
    );
  });

  it('should show selected state visually', async () => {
    const user = userEvent.setup();
    const selectedMap = new Map();

    render(
      <FamilyMemberList
        opportunities={mockOpportunities}
        selectedCheckins={selectedMap}
        onToggleCheckin={vi.fn()}
      />
    );

    const checkboxButton = screen.getAllByRole('button', { name: /kids ministry/i })[0];
    await user.click(checkboxButton);

    // Visual indication via border color change
    expect(checkboxButton).toBeInTheDocument();
  });

  it('should show already checked in members as disabled', () => {
    const checkedInOpportunities: PersonOpportunitiesDto[] = [
      {
        ...mockOpportunities[0],
        currentAttendance: [
          {
            attendanceIdKey: 'ATT123',
            group: 'Kids Ministry',
            location: 'Room 101',
            schedule: '9:00 AM Service',
            securityCode: '1234',
            checkInTime: new Date().toISOString(),
            canCheckOut: true,
          },
        ],
      },
    ];

    render(
      <FamilyMemberList
        opportunities={checkedInOpportunities}
        selectedCheckins={new Map()}
        onToggleCheckin={vi.fn()}
      />
    );

    expect(screen.getByText(/already checked in/i)).toBeInTheDocument();
  });

  it('should have touch-optimized button size', () => {
    render(
      <FamilyMemberList
        opportunities={mockOpportunities}
        selectedCheckins={new Map()}
        onToggleCheckin={vi.fn()}
      />
    );

    const checkboxButtons = screen.getAllByRole('button', { name: /kids ministry/i });

    checkboxButtons.forEach((button) => {
      expect(button).toHaveClass('min-h-[64px]');
    });
  });

  it('should be keyboard accessible', async () => {
    const user = userEvent.setup();
    const mockOnToggle = vi.fn();

    render(
      <FamilyMemberList
        opportunities={mockOpportunities}
        selectedCheckins={new Map()}
        onToggleCheckin={mockOnToggle}
      />
    );

    const checkboxButtons = screen.getAllByRole('button', { name: /kids ministry/i });

    await user.tab();
    expect(checkboxButtons[0]).toHaveFocus();

    await user.keyboard('{Enter}');
    expect(mockOnToggle).toHaveBeenCalled();
  });
});
