import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import { FamilyMemberList } from '../FamilyMemberList';

describe('FamilyMemberList', () => {
  const mockMembers = [
    {
      id: 1,
      idKey: 'ABC123',
      firstName: 'John',
      lastName: 'Smith',
      age: 42,
      photoUrl: 'https://example.com/john.jpg',
      isEligible: true,
    },
    {
      id: 2,
      idKey: 'DEF456',
      firstName: 'Jane',
      lastName: 'Smith',
      age: 40,
      photoUrl: null,
      isEligible: true,
    },
  ];

  it('should render family member cards', () => {
    render(<FamilyMemberList members={mockMembers} onSelect={vi.fn()} />);

    expect(screen.getByText('John Smith')).toBeInTheDocument();
    expect(screen.getByText('Jane Smith')).toBeInTheDocument();
  });

  it('should call onSelect when member is clicked', async () => {
    const user = userEvent.setup();
    const mockOnSelect = vi.fn();

    render(<FamilyMemberList members={mockMembers} onSelect={mockOnSelect} />);

    const johnCard = screen.getByText('John Smith').closest('[data-testid="family-member-card"]');
    await user.click(johnCard!);

    expect(mockOnSelect).toHaveBeenCalledWith(mockMembers[0]);
  });

  it('should show selected state visually', async () => {
    const user = userEvent.setup();
    render(<FamilyMemberList members={mockMembers} onSelect={vi.fn()} />);

    const johnCard = screen.getByText('John Smith').closest('[data-testid="family-member-card"]');
    await user.click(johnCard!);

    expect(johnCard).toHaveAttribute('data-selected', 'true');
  });

  it('should disable ineligible members', () => {
    const ineligibleMembers = [
      { ...mockMembers[0], isEligible: false },
      mockMembers[1],
    ];

    render(<FamilyMemberList members={ineligibleMembers} onSelect={vi.fn()} />);

    const cards = screen.getAllByTestId('family-member-card');
    expect(cards[0]).toHaveAttribute('data-eligible', 'false');
  });

  it('should have touch-optimized card size', () => {
    render(<FamilyMemberList members={mockMembers} onSelect={vi.fn()} />);

    const cards = screen.getAllByTestId('family-member-card');

    cards.forEach((card) => {
      expect(card).toHaveClass('min-h-[80px]');
    });
  });

  it('should be keyboard navigable', async () => {
    const user = userEvent.setup();
    const mockOnSelect = vi.fn();

    render(<FamilyMemberList members={mockMembers} onSelect={mockOnSelect} />);

    const cards = screen.getAllByTestId('family-member-card');

    await user.tab();
    expect(cards[0]).toHaveFocus();

    await user.keyboard('{Enter}');
    expect(mockOnSelect).toHaveBeenCalled();
  });
});
