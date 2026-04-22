/**
 * InvolvementSummary tests
 *
 * Catches:
 *  - Attendance counts render.
 *  - Empty groups state with Browse Groups link.
 *  - Group list renders name, type, role, and joined date.
 *  - Invalid/missing joinedDate falls back to "N/A".
 */
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { InvolvementSummary } from '../InvolvementSummary';
import type { MyInvolvementDto } from '@/types/profile';

function involvement(overrides: Partial<MyInvolvementDto> = {}): MyInvolvementDto {
  return {
    groups: [],
    recentAttendanceCount: 0,
    totalGroupsCount: 0,
    ...overrides,
  };
}

describe('InvolvementSummary', () => {
  it('renders attendance and total counts', () => {
    render(
      <InvolvementSummary
        involvement={involvement({ recentAttendanceCount: 12, totalGroupsCount: 3 })}
      />,
    );
    expect(screen.getByText('12')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
  });

  it('shows empty-state for no groups with Browse Groups link', () => {
    render(<InvolvementSummary involvement={involvement()} />);
    expect(screen.getByText(/not a member of any groups yet/i)).toBeInTheDocument();
    const link = screen.getByRole('link', { name: /browse groups/i });
    expect(link).toHaveAttribute('href', '/groups');
  });

  it('renders group name, type, role, and joined date', () => {
    render(
      <InvolvementSummary
        involvement={involvement({
          totalGroupsCount: 1,
          groups: [
            {
              idKey: 'G1',
              groupName: 'Bible Study',
              groupTypeName: 'Small Group',
              role: 'Member',
              isLeader: false,
              // Use noon UTC so the local-tz conversion does not flip the day.
              joinedDate: '2026-01-15T12:00:00Z',
            },
          ],
        })}
      />,
    );
    expect(screen.getByText('Bible Study')).toBeInTheDocument();
    expect(screen.getByText('Small Group')).toBeInTheDocument();
    expect(screen.getByText('Member')).toBeInTheDocument();
    // Date format like "Jan 15, 2026" — loose match tolerates tz differences.
    expect(screen.getByText(/jan \d+, 2026/i)).toBeInTheDocument();
  });

  it('falls back to N/A for invalid joinedDate', () => {
    render(
      <InvolvementSummary
        involvement={involvement({
          totalGroupsCount: 1,
          groups: [
            {
              idKey: 'G1',
              groupName: 'G',
              groupTypeName: 'T',
              role: 'R',
              isLeader: false,
              joinedDate: 'not-a-date',
            },
          ],
        })}
      />,
    );
    expect(screen.getByText('N/A')).toBeInTheDocument();
  });
});
