/**
 * AuthorizedPickupList tests
 *
 * Covers branches of the list:
 *   - Error state renders the red error banner.
 *   - Loading state renders the spinner.
 *   - Empty state ("No authorized pickups") when list is [].
 *   - Populated state: photo vs placeholder avatar, phone conditional, badges.
 *   - Auto-populate button uses confirm() and only mutates when user confirms.
 *   - Remove button uses confirm() and only mutates when user confirms.
 *   - Add and Edit open the dialog with the appropriate pickup.
 *   - Delete button disabled state propagates via isPending.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mockUseAuthorizedPickups = vi.fn();
const mockDeleteMutate = vi.fn();
const mockAutoPopulateMutate = vi.fn();
const mockUseDeleteAuthorizedPickup = vi.fn();
const mockUseAutoPopulateFamilyMembers = vi.fn();

vi.mock('../hooks', () => ({
  useAuthorizedPickups: (childIdKey: string) => mockUseAuthorizedPickups(childIdKey),
  useDeleteAuthorizedPickup: () => mockUseDeleteAuthorizedPickup(),
  useAutoPopulateFamilyMembers: () => mockUseAutoPopulateFamilyMembers(),
}));

vi.mock('../AddEditAuthorizedPickupDialog', () => ({
  AddEditAuthorizedPickupDialog: ({
    pickup,
    onClose,
  }: {
    pickup: { idKey?: string } | null;
    onClose: () => void;
  }) => (
    <div data-testid="add-edit-dialog">
      <span>editing:{pickup?.idKey ?? 'new'}</span>
      <button onClick={onClose}>dialog-close</button>
    </div>
  ),
}));

import { AuthorizedPickupList } from '../AuthorizedPickupList';
import { AuthorizationLevel } from '../api';

const basePickup = {
  idKey: 'pk1',
  childIdKey: 'c1',
  authorizedPersonIdKey: 'ap1',
  authorizedPersonName: 'Alice Pickup',
  name: 'Alice Pickup',
  authorizationLevel: AuthorizationLevel.Always,
  phoneNumber: '5551234567',
  photoUrl: null,
  relationship: 0,
};

describe('AuthorizedPickupList', () => {
  beforeEach(() => {
    mockDeleteMutate.mockReset();
    mockAutoPopulateMutate.mockReset();
    mockUseDeleteAuthorizedPickup.mockReturnValue({
      mutateAsync: mockDeleteMutate,
      isPending: false,
    });
    mockUseAutoPopulateFamilyMembers.mockReturnValue({
      mutateAsync: mockAutoPopulateMutate,
      isPending: false,
    });
  });

  afterEach(() => vi.clearAllMocks());

  it('renders the error banner when the list query errors', () => {
    mockUseAuthorizedPickups.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error('bad'),
    });
    render(<AuthorizedPickupList childIdKey="c1" childName="Kid" />);
    expect(screen.getByText(/error loading authorized pickups/i)).toBeInTheDocument();
    expect(screen.getByText('bad')).toBeInTheDocument();
  });

  it('falls back to a generic message when error has no Error shape', () => {
    mockUseAuthorizedPickups.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: 'raw string',
    });
    render(<AuthorizedPickupList childIdKey="c1" childName="Kid" />);
    expect(screen.getByText(/unknown error occurred/i)).toBeInTheDocument();
  });

  it('renders loading spinner while loading', () => {
    mockUseAuthorizedPickups.mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
    });
    render(<AuthorizedPickupList childIdKey="c1" childName="Kid" />);
    expect(screen.getByText(/loading authorized pickups/i)).toBeInTheDocument();
  });

  it('renders empty state when list is empty', () => {
    mockUseAuthorizedPickups.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });
    render(<AuthorizedPickupList childIdKey="c1" childName="Kid" />);
    expect(screen.getByText(/no authorized pickups/i)).toBeInTheDocument();
  });

  it('renders the list with badges and phone numbers', () => {
    mockUseAuthorizedPickups.mockReturnValue({
      data: [
        { ...basePickup, idKey: 'pk1', photoUrl: 'https://img/x' },
        {
          ...basePickup,
          idKey: 'pk2',
          authorizedPersonName: 'Bob',
          name: 'Bob',
          authorizationLevel: AuthorizationLevel.EmergencyOnly,
          phoneNumber: undefined,
          photoUrl: null,
        },
      ],
      isLoading: false,
      error: null,
    });
    render(<AuthorizedPickupList childIdKey="c1" childName="Kid" />);
    expect(screen.getByText('Alice Pickup')).toBeInTheDocument();
    expect(screen.getByText('Bob')).toBeInTheDocument();
    expect(screen.getByText('Always Authorized')).toBeInTheDocument();
    expect(screen.getByText('Emergency Only')).toBeInTheDocument();
    // Photo branch: img with alt
    expect(screen.getByRole('img', { name: 'Alice Pickup' })).toBeInTheDocument();
    // Phone present for Alice
    expect(screen.getByText('5551234567')).toBeInTheDocument();
  });

  it('Add button opens the dialog for a new pickup', async () => {
    const user = userEvent.setup();
    mockUseAuthorizedPickups.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });
    render(<AuthorizedPickupList childIdKey="c1" childName="Kid" />);
    await user.click(screen.getByRole('button', { name: /add authorized pickup/i }));
    expect(screen.getByTestId('add-edit-dialog')).toBeInTheDocument();
    expect(screen.getByText('editing:new')).toBeInTheDocument();
  });

  it('Edit button opens the dialog with the clicked pickup', async () => {
    const user = userEvent.setup();
    mockUseAuthorizedPickups.mockReturnValue({
      data: [basePickup],
      isLoading: false,
      error: null,
    });
    render(<AuthorizedPickupList childIdKey="c1" childName="Kid" />);
    await user.click(screen.getByRole('button', { name: /edit/i }));
    expect(screen.getByText('editing:pk1')).toBeInTheDocument();
  });

  it('dialog close handler removes the dialog', async () => {
    const user = userEvent.setup();
    mockUseAuthorizedPickups.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });
    render(<AuthorizedPickupList childIdKey="c1" childName="Kid" />);
    await user.click(screen.getByRole('button', { name: /add authorized pickup/i }));
    await user.click(screen.getByText('dialog-close'));
    expect(screen.queryByTestId('add-edit-dialog')).toBeNull();
  });

  it('Remove cancels when user rejects the confirm()', async () => {
    const user = userEvent.setup();
    const confirmFn = vi.fn().mockReturnValue(false);
    vi.stubGlobal('confirm', confirmFn);
    mockUseAuthorizedPickups.mockReturnValue({
      data: [basePickup],
      isLoading: false,
      error: null,
    });
    render(<AuthorizedPickupList childIdKey="c1" childName="Kid" />);
    await user.click(screen.getByRole('button', { name: /remove/i }));
    expect(confirmFn).toHaveBeenCalled();
    expect(mockDeleteMutate).not.toHaveBeenCalled();
    vi.unstubAllGlobals();
  });

  it('Remove mutates when user accepts the confirm()', async () => {
    const user = userEvent.setup();
    vi.stubGlobal('confirm', vi.fn().mockReturnValue(true));
    mockDeleteMutate.mockResolvedValueOnce(undefined);
    mockUseAuthorizedPickups.mockReturnValue({
      data: [basePickup],
      isLoading: false,
      error: null,
    });
    render(<AuthorizedPickupList childIdKey="c1" childName="Kid" />);
    await user.click(screen.getByRole('button', { name: /remove/i }));
    expect(mockDeleteMutate).toHaveBeenCalledWith({
      pickupIdKey: 'pk1',
      childIdKey: 'c1',
    });
    vi.unstubAllGlobals();
  });

  it('Remove swallows the rejected mutation without crashing', async () => {
    const user = userEvent.setup();
    vi.stubGlobal('confirm', vi.fn().mockReturnValue(true));
    mockDeleteMutate.mockRejectedValueOnce(new Error('server error'));
    mockUseAuthorizedPickups.mockReturnValue({
      data: [basePickup],
      isLoading: false,
      error: null,
    });
    render(<AuthorizedPickupList childIdKey="c1" childName="Kid" />);
    await user.click(screen.getByRole('button', { name: /remove/i }));
    // Did not bubble; list still renders.
    expect(screen.getByText('Alice Pickup')).toBeInTheDocument();
    vi.unstubAllGlobals();
  });

  it('Auto-populate cancels when user rejects the confirm()', async () => {
    const user = userEvent.setup();
    vi.stubGlobal('confirm', vi.fn().mockReturnValue(false));
    mockUseAuthorizedPickups.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });
    render(<AuthorizedPickupList childIdKey="c1" childName="Kid" />);
    await user.click(screen.getByRole('button', { name: /auto-populate/i }));
    expect(mockAutoPopulateMutate).not.toHaveBeenCalled();
    vi.unstubAllGlobals();
  });

  it('Auto-populate mutates with childIdKey on accept', async () => {
    const user = userEvent.setup();
    vi.stubGlobal('confirm', vi.fn().mockReturnValue(true));
    mockAutoPopulateMutate.mockResolvedValueOnce(undefined);
    mockUseAuthorizedPickups.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });
    render(<AuthorizedPickupList childIdKey="c1" childName="Kid" />);
    await user.click(screen.getByRole('button', { name: /auto-populate/i }));
    expect(mockAutoPopulateMutate).toHaveBeenCalledWith('c1');
    vi.unstubAllGlobals();
  });

  it('Auto-populate swallows a mutation error', async () => {
    const user = userEvent.setup();
    vi.stubGlobal('confirm', vi.fn().mockReturnValue(true));
    mockAutoPopulateMutate.mockRejectedValueOnce(new Error('no family'));
    mockUseAuthorizedPickups.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });
    render(<AuthorizedPickupList childIdKey="c1" childName="Kid" />);
    await user.click(screen.getByRole('button', { name: /auto-populate/i }));
    // Component still rendered.
    expect(screen.getByText(/authorized pickups for/i)).toBeInTheDocument();
    vi.unstubAllGlobals();
  });

  it('Remove is disabled while delete mutation is pending', () => {
    mockUseDeleteAuthorizedPickup.mockReturnValue({
      mutateAsync: mockDeleteMutate,
      isPending: true,
    });
    mockUseAuthorizedPickups.mockReturnValue({
      data: [basePickup],
      isLoading: false,
      error: null,
    });
    render(<AuthorizedPickupList childIdKey="c1" childName="Kid" />);
    expect(screen.getByRole('button', { name: /removing/i })).toBeDisabled();
  });

  it('Auto-populate button shows pending label when isPending', () => {
    mockUseAutoPopulateFamilyMembers.mockReturnValue({
      mutateAsync: mockAutoPopulateMutate,
      isPending: true,
    });
    mockUseAuthorizedPickups.mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    });
    render(<AuthorizedPickupList childIdKey="c1" childName="Kid" />);
    expect(screen.getByRole('button', { name: /adding/i })).toBeDisabled();
  });
});
