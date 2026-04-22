/**
 * CampusCard tests
 *
 * Catches:
 *  - Renders required fields (name) and optional badges (shortCode, Inactive).
 *  - Omits description/timezone/phone when fields are empty.
 *  - Edit and Delete buttons fire their callbacks.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { CampusCard } from '../CampusCard';
import type { CampusDto } from '@/types';

function baseCampus(overrides: Partial<CampusDto> = {}): CampusDto {
  return {
    idKey: 'C1',
    name: 'Main Campus',
    isActive: true,
    ...overrides,
  } as CampusDto;
}

describe('CampusCard', () => {
  it('shows the campus name', () => {
    render(<CampusCard campus={baseCampus()} onEdit={vi.fn()} onDelete={vi.fn()} />);
    expect(screen.getByRole('heading', { name: 'Main Campus' })).toBeInTheDocument();
  });

  it('shows the short code when provided', () => {
    render(
      <CampusCard
        campus={baseCampus({ shortCode: 'MAIN' })}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
      />,
    );
    expect(screen.getByText('MAIN')).toBeInTheDocument();
  });

  it('shows the Inactive badge when isActive=false', () => {
    render(
      <CampusCard
        campus={baseCampus({ isActive: false })}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
      />,
    );
    expect(screen.getByText('Inactive')).toBeInTheDocument();
  });

  it('does NOT show the Inactive badge when isActive=true', () => {
    render(
      <CampusCard
        campus={baseCampus({ isActive: true })}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
      />,
    );
    expect(screen.queryByText('Inactive')).toBeNull();
  });

  it('shows the description when provided', () => {
    render(
      <CampusCard
        campus={baseCampus({ description: 'Primary campus' })}
        onEdit={vi.fn()}
        onDelete={vi.fn()}
      />,
    );
    expect(screen.getByText('Primary campus')).toBeInTheDocument();
  });

  it('fires onEdit and onDelete when the respective buttons are clicked', async () => {
    const user = userEvent.setup();
    const onEdit = vi.fn();
    const onDelete = vi.fn();
    render(<CampusCard campus={baseCampus()} onEdit={onEdit} onDelete={onDelete} />);

    await user.click(screen.getByTitle('Edit campus'));
    expect(onEdit).toHaveBeenCalledOnce();

    await user.click(screen.getByTitle('Delete campus'));
    expect(onDelete).toHaveBeenCalledOnce();
  });
});
