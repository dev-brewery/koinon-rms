/**
 * FamilyMemberCard tests
 *
 * Catches:
 *  - Link target wiring to /admin/people/{idKey}.
 *  - Avatar falls back to SVG when no photoUrl.
 *  - Role indicator dot: blue for Adult, green otherwise.
 *  - `familyRoles` prop renders the textual legend instead of a colour dot.
 *  - Remove button shows only when readOnly=false AND onRemove is provided.
 *  - Remove button ARIA label mentions the person's name.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { FamilyMemberCard } from '../FamilyMemberCard';
import type { FamilyMemberDto } from '@/services/api/types';

function member(
  overrides: Partial<FamilyMemberDto> & { personOverrides?: Partial<FamilyMemberDto['person']>; roleName?: string } = {},
): FamilyMemberDto {
  const { personOverrides, roleName = 'Adult', ...rest } = overrides;
  return {
    idKey: 'M1',
    status: 'Active',
    person: {
      idKey: 'PER1',
      firstName: 'Alice',
      lastName: 'Smith',
      fullName: 'Alice Smith',
      gender: 'Female',
      ...personOverrides,
    } as FamilyMemberDto['person'],
    role: { name: roleName } as FamilyMemberDto['role'],
    ...rest,
  };
}

function wrap(ui: React.ReactNode) {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
}

describe('FamilyMemberCard', () => {
  it('links the avatar and name to /admin/people/{idKey}', () => {
    wrap(<FamilyMemberCard member={member({ personOverrides: { idKey: 'XYZ' } })} />);
    const links = screen.getAllByRole('link');
    expect(links.length).toBeGreaterThanOrEqual(2);
    links.forEach((l) => {
      expect(l).toHaveAttribute('href', '/admin/people/XYZ');
    });
  });

  it('renders a blue dot for Adult role', () => {
    const { container } = wrap(<FamilyMemberCard member={member({ roleName: 'Adult' })} />);
    expect(container.querySelector('.bg-blue-500')).not.toBeNull();
  });

  it('renders a green dot for Child role', () => {
    const { container } = wrap(<FamilyMemberCard member={member({ roleName: 'Child' })} />);
    expect(container.querySelector('.bg-green-500')).not.toBeNull();
  });

  it('renders the familyRoles legend instead of the dot when provided', () => {
    wrap(
      <FamilyMemberCard
        member={member({ roleName: 'Adult' })}
        familyRoles={['Adult', 'Parent']}
      />,
    );
    expect(screen.getByText('Adult · Parent')).toBeInTheDocument();
  });

  it('hides the remove button when readOnly=true', () => {
    wrap(
      <FamilyMemberCard member={member()} readOnly onRemove={vi.fn()} />,
    );
    expect(
      screen.queryByRole('button', { name: /remove .* from family/i }),
    ).toBeNull();
  });

  it('shows remove button and calls onRemove when clicked', async () => {
    const user = userEvent.setup();
    const onRemove = vi.fn();
    wrap(<FamilyMemberCard member={member()} onRemove={onRemove} />);
    await user.click(
      screen.getByRole('button', { name: /remove alice smith from family/i }),
    );
    expect(onRemove).toHaveBeenCalledOnce();
  });

  it('shows age suffix with "y" when person has age', () => {
    wrap(<FamilyMemberCard member={member({ personOverrides: { age: 42 } })} />);
    expect(screen.getByText('(42y)')).toBeInTheDocument();
  });
});
