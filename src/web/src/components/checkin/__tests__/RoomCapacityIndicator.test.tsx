/**
 * RoomCapacityIndicator tests
 *
 * Catches:
 *  - Status → colour/icon mapping regressions (FULL=red, WARNING=yellow, AVAILABLE=green).
 *  - compact variant renders inline vs. full card.
 *  - capacityText: "X / Y" when capacity known, "X checked in" when not.
 *  - FULL shows overflow helper message; WARNING shows near-capacity message.
 *  - Progress bar width is clamped to 100% when currentCount > softCapacity.
 *  - RoomCapacityBadge emits correct colour mapping.
 */
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { CapacityStatus } from '@/services/api/types';
import {
  RoomCapacityBadge,
  RoomCapacityIndicator,
} from '../RoomCapacityIndicator';

describe('RoomCapacityIndicator — status mapping', () => {
  it('renders AVAILABLE status with green styling', () => {
    const { container } = render(
      <RoomCapacityIndicator
        currentCount={3}
        softCapacity={20}
        hardCapacity={25}
        status={CapacityStatus.Available}
        locationName="Room 1"
      />,
    );
    expect(screen.getByText('AVAILABLE')).toBeInTheDocument();
    expect(container.querySelector('.bg-green-100')).not.toBeNull();
  });

  it('renders WARNING status with yellow styling and near-capacity note', () => {
    const { container } = render(
      <RoomCapacityIndicator
        currentCount={18}
        softCapacity={20}
        hardCapacity={25}
        status={CapacityStatus.Warning}
        locationName="Room 1"
      />,
    );
    expect(screen.getByText('WARNING')).toBeInTheDocument();
    expect(container.querySelector('.bg-yellow-100')).not.toBeNull();
    expect(screen.getByText(/near capacity/i)).toBeInTheDocument();
  });

  it('renders FULL status with red styling and overflow note', () => {
    const { container } = render(
      <RoomCapacityIndicator
        currentCount={25}
        softCapacity={20}
        hardCapacity={25}
        status={CapacityStatus.Full}
        locationName="Room 1"
      />,
    );
    expect(screen.getByText('FULL')).toBeInTheDocument();
    expect(container.querySelector('.bg-red-100')).not.toBeNull();
    expect(screen.getByText(/room full/i)).toBeInTheDocument();
  });
});

describe('RoomCapacityIndicator — capacity text', () => {
  it('shows "N / capacity" when a capacity is defined', () => {
    render(
      <RoomCapacityIndicator
        currentCount={5}
        softCapacity={10}
        hardCapacity={12}
        status={CapacityStatus.Available}
        locationName="Room 2"
      />,
    );
    expect(screen.getByText('5 / 12')).toBeInTheDocument();
  });

  it('falls back to softCapacity when hardCapacity is absent', () => {
    render(
      <RoomCapacityIndicator
        currentCount={5}
        softCapacity={10}
        status={CapacityStatus.Available}
        locationName="Room 2"
      />,
    );
    expect(screen.getByText('5 / 10')).toBeInTheDocument();
  });

  it('shows "N checked in" when no capacity is known', () => {
    render(
      <RoomCapacityIndicator
        currentCount={5}
        status={CapacityStatus.Available}
        locationName="Open Room"
      />,
    );
    expect(screen.getByText('5 checked in')).toBeInTheDocument();
  });

  it('renders the percentageFull value in parentheses when provided', () => {
    render(
      <RoomCapacityIndicator
        currentCount={15}
        softCapacity={20}
        status={CapacityStatus.Warning}
        percentageFull={75}
        locationName="x"
      />,
    );
    expect(screen.getByText('(75%)')).toBeInTheDocument();
  });
});

describe('RoomCapacityIndicator — compact', () => {
  it('renders an inline pill with status icon', () => {
    const { container } = render(
      <RoomCapacityIndicator
        currentCount={5}
        softCapacity={10}
        status={CapacityStatus.Available}
        locationName="Room"
        compact
      />,
    );
    const pill = container.firstChild as HTMLElement;
    expect(pill.className).toMatch(/inline-flex/);
    expect(pill.className).toMatch(/bg-green-100/);
  });

  it('hides the capacity text when showDetails=false (compact)', () => {
    render(
      <RoomCapacityIndicator
        currentCount={5}
        softCapacity={10}
        status={CapacityStatus.Available}
        locationName="Room"
        compact
        showDetails={false}
      />,
    );
    expect(screen.queryByText('5 / 10')).toBeNull();
  });
});

describe('RoomCapacityIndicator — progress bar', () => {
  it('clamps the progress bar width at 100% when over soft capacity', () => {
    const { container } = render(
      <RoomCapacityIndicator
        currentCount={50}
        softCapacity={20}
        status={CapacityStatus.Full}
        locationName="Room"
      />,
    );
    const bar = container.querySelector(
      '[style*="width:"]',
    ) as HTMLElement | null;
    expect(bar).not.toBeNull();
    // Should clamp to 100%, not 250%.
    expect(bar!.style.width).toBe('100%');
  });

  it('uses red fill colour when FULL', () => {
    const { container } = render(
      <RoomCapacityIndicator
        currentCount={20}
        softCapacity={20}
        status={CapacityStatus.Full}
        locationName="Room"
      />,
    );
    expect(container.querySelector('.bg-red-600')).not.toBeNull();
  });

  it('uses yellow fill colour when WARNING', () => {
    const { container } = render(
      <RoomCapacityIndicator
        currentCount={15}
        softCapacity={20}
        status={CapacityStatus.Warning}
        locationName="Room"
      />,
    );
    expect(container.querySelector('.bg-yellow-500')).not.toBeNull();
  });
});

describe('RoomCapacityBadge', () => {
  it('emits green styling for Available', () => {
    const { container } = render(
      <RoomCapacityBadge
        status={CapacityStatus.Available}
        currentCount={3}
        capacity={10}
      />,
    );
    expect(container.firstChild).toHaveClass('bg-green-100');
    expect(screen.getByText('3/10')).toBeInTheDocument();
  });

  it('emits yellow styling for Warning', () => {
    const { container } = render(
      <RoomCapacityBadge status={CapacityStatus.Warning} currentCount={8} capacity={10} />,
    );
    expect(container.firstChild).toHaveClass('bg-yellow-100');
  });

  it('emits red styling for Full', () => {
    const { container } = render(
      <RoomCapacityBadge status={CapacityStatus.Full} currentCount={10} capacity={10} />,
    );
    expect(container.firstChild).toHaveClass('bg-red-100');
  });

  it('omits the capacity portion when no capacity is provided', () => {
    render(<RoomCapacityBadge status={CapacityStatus.Available} currentCount={4} />);
    expect(screen.getByText('4')).toBeInTheDocument();
  });
});
