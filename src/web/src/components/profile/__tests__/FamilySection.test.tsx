/**
 * FamilySection tests
 *
 * Catches:
 *  - Empty-state rendering with friendly message.
 *  - One FamilyMemberCard is rendered per member.
 *  - Grid heading renders for non-empty list.
 */
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, expect, it } from 'vitest';
import { FamilySection } from '../FamilySection';
import type { FamilyMemberDto } from '@/types/profile';

function renderWithClient(ui: React.ReactNode) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

function member(overrides: Partial<FamilyMemberDto> = {}): FamilyMemberDto {
  return {
    idKey: 'M1',
    firstName: 'Alice',
    lastName: 'Smith',
    fullName: 'Alice Smith',
    gender: 'Female',
    phoneNumbers: [],
    familyRole: 'Adult',
    canEdit: true,
    hasCriticalAllergies: false,
    ...overrides,
  };
}

describe('FamilySection', () => {
  it('renders empty-state when no members', () => {
    renderWithClient(<FamilySection members={[]} />);
    expect(screen.getByText(/no family members found/i)).toBeInTheDocument();
  });

  it('renders the heading when members are present', () => {
    renderWithClient(<FamilySection members={[member()]} />);
    expect(screen.getByRole('heading', { name: /your family/i })).toBeInTheDocument();
  });

  it('renders one card per member', () => {
    renderWithClient(
      <FamilySection
        members={[
          member({ idKey: 'M1', fullName: 'Alice Smith' }),
          member({ idKey: 'M2', fullName: 'Bob Smith' }),
        ]}
      />,
    );
    expect(screen.getByText('Alice Smith')).toBeInTheDocument();
    expect(screen.getByText('Bob Smith')).toBeInTheDocument();
  });
});
