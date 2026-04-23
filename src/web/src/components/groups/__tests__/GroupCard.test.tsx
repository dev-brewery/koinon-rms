/**
 * GroupCard (public groups) tests
 *
 * Catches regressions in:
 *   - Renders name + publicDescription when given.
 *   - Campus / groupType / openings badges show conditionally.
 *   - "Full" badge replaces "Open" when hasOpenings=false.
 *   - Meeting schedule line renders only when summary is present.
 *   - Member count shows "N members" with optional "/ capacity".
 *   - Renders as <button> when onClick supplied; onClick fires with the group.
 *   - Renders as <div> when no onClick.
 *   - Request to Join button only when showRequestButton && hasOpenings;
 *     clicking stops propagation and opens the modal.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';

vi.mock('../RequestToJoinModal', () => ({
  RequestToJoinModal: ({
    isOpen,
    groupName,
    onClose,
  }: {
    isOpen: boolean;
    groupName: string;
    onClose: () => void;
  }) =>
    isOpen ? (
      <div data-testid="rtj-modal">
        <span>modal:{groupName}</span>
        <button onClick={onClose}>modal-close</button>
      </div>
    ) : null,
}));

import { GroupCard } from '../GroupCard';

const baseGroup = {
  idKey: 'g1',
  name: 'Alpha',
  publicDescription: 'Fun group',
  campusName: 'Main',
  groupTypeName: 'Small Group',
  meetingScheduleSummary: 'Sundays 10am',
  memberCount: 5,
  capacity: 10,
  hasOpenings: true,
};

describe('GroupCard (public)', () => {
  afterEach(() => vi.clearAllMocks());

  it('renders name and description', () => {
    render(<GroupCard group={baseGroup as never} />);
    expect(screen.getByRole('heading', { name: 'Alpha' })).toBeInTheDocument();
    expect(screen.getByText('Fun group')).toBeInTheDocument();
  });

  it('hides description when missing', () => {
    render(
      <GroupCard group={{ ...baseGroup, publicDescription: '' } as never} />
    );
    expect(screen.queryByText('Fun group')).toBeNull();
  });

  it('renders campus + group type badges', () => {
    render(<GroupCard group={baseGroup as never} />);
    expect(screen.getByText('Main')).toBeInTheDocument();
    expect(screen.getByText('Small Group')).toBeInTheDocument();
  });

  it('renders Open badge when hasOpenings=true', () => {
    render(<GroupCard group={baseGroup as never} />);
    expect(screen.getByText('Open')).toBeInTheDocument();
    expect(screen.queryByText('Full')).toBeNull();
  });

  it('renders Full badge when hasOpenings=false', () => {
    render(
      <GroupCard group={{ ...baseGroup, hasOpenings: false } as never} />
    );
    expect(screen.getByText('Full')).toBeInTheDocument();
    expect(screen.queryByText('Open')).toBeNull();
  });

  it('shows meeting schedule summary when present', () => {
    render(<GroupCard group={baseGroup as never} />);
    expect(screen.getByText('Sundays 10am')).toBeInTheDocument();
  });

  it('omits meeting schedule when summary empty', () => {
    render(
      <GroupCard
        group={{ ...baseGroup, meetingScheduleSummary: '' } as never}
      />
    );
    expect(screen.queryByText('Sundays 10am')).toBeNull();
  });

  it('renders member count with capacity', () => {
    render(<GroupCard group={baseGroup as never} />);
    expect(screen.getByText(/5 \/ 10 members/i)).toBeInTheDocument();
  });

  it('renders member count without capacity when capacity is nullish', () => {
    const { container } = render(
      <GroupCard group={{ ...baseGroup, capacity: null } as never} />
    );
    // Should show "5" then " members" (no slash).
    expect(container.textContent).toMatch(/5\s*members/);
    expect(container.textContent).not.toMatch(/5 \/ /);
  });

  it('renders as <button> when onClick is provided and fires with the group', async () => {
    const onClick = vi.fn();
    const user = userEvent.setup();
    render(<GroupCard group={baseGroup as never} onClick={onClick} />);
    const button = screen.getByRole('button', { name: /alpha/i });
    await user.click(button);
    expect(onClick).toHaveBeenCalledWith(baseGroup);
  });

  it('renders as <div> when onClick is not provided (no cursor-pointer ring class)', () => {
    const { container } = render(<GroupCard group={baseGroup as never} />);
    // The element doesn't have role="button"
    const element = container.querySelector('h3')!.closest('div, button');
    expect(element?.tagName).toBe('DIV');
  });

  it('opens Request-to-Join modal when button clicked; propagation is stopped', async () => {
    const onClick = vi.fn();
    const user = userEvent.setup();
    render(<GroupCard group={baseGroup as never} onClick={onClick} />);
    // Button has exact text; distinguish from the card's accessible name which
    // includes the whole card text.
    const buttons = screen.getAllByRole('button');
    const joinBtn = buttons.find((b) => b.textContent === 'Request to Join');
    expect(joinBtn).toBeTruthy();
    await user.click(joinBtn!);
    expect(screen.getByTestId('rtj-modal')).toBeInTheDocument();
    expect(screen.getByText('modal:Alpha')).toBeInTheDocument();
    // onClick on the card must NOT have fired because the button stopped propagation.
    expect(onClick).not.toHaveBeenCalled();
  });

  it('hides Request-to-Join button when showRequestButton=false', () => {
    render(
      <GroupCard group={baseGroup as never} showRequestButton={false} />
    );
    expect(screen.queryByRole('button', { name: /request to join/i })).toBeNull();
  });

  it('hides Request-to-Join button when no openings even if showRequestButton=true', () => {
    render(
      <GroupCard
        group={{ ...baseGroup, hasOpenings: false } as never}
        showRequestButton
      />
    );
    expect(screen.queryByRole('button', { name: /request to join/i })).toBeNull();
  });

  it('closes the modal on close click', async () => {
    const user = userEvent.setup();
    render(<GroupCard group={baseGroup as never} />);
    // Without onClick, the outer element is a div; the only button in the
    // tree is "Request to Join".
    await user.click(screen.getByRole('button', { name: 'Request to Join' }));
    expect(screen.getByTestId('rtj-modal')).toBeInTheDocument();
    await user.click(screen.getByText('modal-close'));
    expect(screen.queryByTestId('rtj-modal')).toBeNull();
  });
});
