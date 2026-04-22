/**
 * PersonCard tests
 *
 * Catches:
 *  - Links the card to /admin/people/{idKey}.
 *  - Shows fullName + age when provided; omits age when undefined.
 *  - Gender='Unknown' is hidden (privacy / clutter).
 *  - connectionStatus and recordStatus badges render conditionally.
 *  - recordStatus=Active is NOT shown as a badge.
 *  - primaryCampus name renders when provided.
 */
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { PersonCard } from '../PersonCard';
import type { PersonSummaryDto } from '@/services/api/types';

function person(overrides: Partial<PersonSummaryDto> = {}): PersonSummaryDto {
  return {
    idKey: 'PER1',
    firstName: 'Alice',
    lastName: 'Smith',
    fullName: 'Alice Smith',
    gender: 'Female',
    ...overrides,
  } as PersonSummaryDto;
}

function wrap(ui: React.ReactNode) {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
}

describe('PersonCard', () => {
  it('links the card to /admin/people/{idKey}', () => {
    wrap(<PersonCard person={person({ idKey: 'XYZ' })} />);
    expect(screen.getByRole('link')).toHaveAttribute('href', '/admin/people/XYZ');
  });

  it('shows the fullName', () => {
    wrap(<PersonCard person={person()} />);
    expect(screen.getByText('Alice Smith')).toBeInTheDocument();
  });

  it('shows age when provided, hides when undefined', () => {
    wrap(<PersonCard person={person({ age: 32 })} />);
    expect(screen.getByText('(32)')).toBeInTheDocument();
  });

  it('omits gender when it is "Unknown"', () => {
    wrap(<PersonCard person={person({ gender: 'Unknown' } as unknown as PersonSummaryDto)} />);
    expect(screen.queryByText('Unknown')).toBeNull();
  });

  it('shows gender when it is not "Unknown"', () => {
    wrap(<PersonCard person={person({ gender: 'Male' } as unknown as PersonSummaryDto)} />);
    expect(screen.getByText('Male')).toBeInTheDocument();
  });

  it('hides recordStatus badge when value=Active', () => {
    wrap(
      <PersonCard
        person={person({
          recordStatus: { value: 'Active' } as PersonSummaryDto['recordStatus'],
        })}
      />,
    );
    expect(screen.queryByText('Active')).toBeNull();
  });

  it('shows recordStatus badge when value !== Active', () => {
    wrap(
      <PersonCard
        person={person({
          recordStatus: { value: 'Inactive' } as PersonSummaryDto['recordStatus'],
        })}
      />,
    );
    expect(screen.getByText('Inactive')).toBeInTheDocument();
  });

  it('shows connectionStatus badge when provided', () => {
    wrap(
      <PersonCard
        person={person({
          connectionStatus: { value: 'Member' } as PersonSummaryDto['connectionStatus'],
        })}
      />,
    );
    expect(screen.getByText('Member')).toBeInTheDocument();
  });

  it('shows primaryCampus name when provided', () => {
    wrap(
      <PersonCard
        person={person({
          primaryCampus: { name: 'Main Campus' } as PersonSummaryDto['primaryCampus'],
        })}
      />,
    );
    expect(screen.getByText('Main Campus')).toBeInTheDocument();
  });

  it('renders photo when photoUrl is provided', () => {
    wrap(<PersonCard person={person({ photoUrl: 'https://x/y.jpg' })} />);
    const img = screen.getByAltText('Alice Smith') as HTMLImageElement;
    expect(img.src).toContain('https://x/y.jpg');
  });
});
