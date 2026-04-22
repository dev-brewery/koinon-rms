/**
 * GroupTypeCard tests
 *
 * Catches:
 *  - Name always renders; description only when provided.
 *  - Capability badges appear conditionally (Attendance, Self-Registration, Public).
 *  - System badge appears only for system groupTypes.
 *  - groupCount singular / plural term rendering (uses groupTerm/groupMemberTerm).
 *  - Edit button disabled when isArchived=true.
 *  - onEdit / onViewGroups fire correct callbacks.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { GroupTypeCard } from '../GroupTypeCard';
import type { GroupTypeAdminDto } from '@/services/api/types';

function base(overrides: Partial<GroupTypeAdminDto> = {}): GroupTypeAdminDto {
  return {
    idKey: 'GT1',
    guid: '00000000-0000-0000-0000-000000000001',
    name: 'Small Group',
    groupTerm: 'Group',
    groupMemberTerm: 'Member',
    takesAttendance: false,
    allowSelfRegistration: false,
    requiresMemberApproval: false,
    defaultIsPublic: false,
    isSystem: false,
    isArchived: false,
    order: 0,
    groupCount: 5,
    ...overrides,
  };
}

describe('GroupTypeCard', () => {
  it('renders the group type name', () => {
    render(
      <GroupTypeCard groupType={base()} onEdit={vi.fn()} onViewGroups={vi.fn()} />,
    );
    expect(screen.getByRole('heading', { name: 'Small Group' })).toBeInTheDocument();
  });

  it('shows the description when provided', () => {
    render(
      <GroupTypeCard
        groupType={base({ description: 'Community groups' })}
        onEdit={vi.fn()}
        onViewGroups={vi.fn()}
      />,
    );
    expect(screen.getByText('Community groups')).toBeInTheDocument();
  });

  it('shows capability badges for enabled capabilities', () => {
    render(
      <GroupTypeCard
        groupType={base({
          takesAttendance: true,
          allowSelfRegistration: true,
          defaultIsPublic: true,
        })}
        onEdit={vi.fn()}
        onViewGroups={vi.fn()}
      />,
    );
    expect(screen.getByText('Attendance')).toBeInTheDocument();
    expect(screen.getByText('Self-Registration')).toBeInTheDocument();
    expect(screen.getByText('Public')).toBeInTheDocument();
  });

  it('omits capability badges for disabled capabilities', () => {
    render(
      <GroupTypeCard groupType={base()} onEdit={vi.fn()} onViewGroups={vi.fn()} />,
    );
    expect(screen.queryByText('Attendance')).toBeNull();
    expect(screen.queryByText('Self-Registration')).toBeNull();
    expect(screen.queryByText('Public')).toBeNull();
  });

  it('shows the System badge when isSystem=true', () => {
    render(
      <GroupTypeCard
        groupType={base({ isSystem: true })}
        onEdit={vi.fn()}
        onViewGroups={vi.fn()}
      />,
    );
    expect(screen.getByText('System')).toBeInTheDocument();
  });

  it('uses singular groupTerm when groupCount=1', () => {
    render(
      <GroupTypeCard
        groupType={base({ groupCount: 1, groupTerm: 'Group' })}
        onEdit={vi.fn()}
        onViewGroups={vi.fn()}
      />,
    );
    // Button label "1 Group"
    expect(screen.getByRole('button', { name: '1 Group' })).toBeInTheDocument();
  });

  it('pluralizes groupTerm when groupCount !== 1', () => {
    render(
      <GroupTypeCard
        groupType={base({ groupCount: 5, groupTerm: 'Group' })}
        onEdit={vi.fn()}
        onViewGroups={vi.fn()}
      />,
    );
    expect(screen.getByRole('button', { name: '5 Groups' })).toBeInTheDocument();
  });

  it('disables Edit when isArchived=true', () => {
    render(
      <GroupTypeCard
        groupType={base({ isArchived: true })}
        onEdit={vi.fn()}
        onViewGroups={vi.fn()}
      />,
    );
    expect(screen.getByRole('button', { name: 'Edit' })).toBeDisabled();
  });

  it('fires callbacks on button click', async () => {
    const user = userEvent.setup();
    const onEdit = vi.fn();
    const onViewGroups = vi.fn();
    render(
      <GroupTypeCard
        groupType={base({ groupCount: 3 })}
        onEdit={onEdit}
        onViewGroups={onViewGroups}
      />,
    );
    await user.click(screen.getByRole('button', { name: /3 groups/i }));
    expect(onViewGroups).toHaveBeenCalledOnce();

    await user.click(screen.getByRole('button', { name: 'Edit' }));
    expect(onEdit).toHaveBeenCalledOnce();
  });
});
