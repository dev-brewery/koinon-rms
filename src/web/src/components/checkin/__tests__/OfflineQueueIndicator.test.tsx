/**
 * OfflineQueueIndicator tests
 *
 * Catches:
 *  - Returns null when online with an empty queue (no stray banner).
 *  - Shows offline banner when mode=offline.
 *  - Shows queue count with correct pluralization.
 *  - Shows "Send Now" button only when online with queued items AND onSync provided.
 *  - Syncing, success, and error statuses each render their distinct banners.
 *  - Retry button fires onSync in error state.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { OfflineQueueIndicator } from '../OfflineQueueIndicator';
import type { OfflineCheckinState } from '@/hooks/useOfflineCheckin';

function baseState(overrides: Partial<OfflineCheckinState> = {}): OfflineCheckinState {
  return {
    mode: 'online',
    queuedCount: 0,
    syncStatus: 'idle',
    lastSyncResults: null,
    ...overrides,
  } as OfflineCheckinState;
}

describe('OfflineQueueIndicator', () => {
  it('renders nothing when online with an empty queue and idle sync', () => {
    const { container } = render(
      <OfflineQueueIndicator state={baseState()} />,
    );
    expect(container.firstChild).toBeNull();
  });

  it('shows the offline banner when mode=offline', () => {
    render(<OfflineQueueIndicator state={baseState({ mode: 'offline' })} />);
    expect(screen.getByText(/offline mode/i)).toBeInTheDocument();
  });

  it('shows "1 check-in queued" (singular) when queuedCount=1', () => {
    render(
      <OfflineQueueIndicator
        state={baseState({ mode: 'offline', queuedCount: 1 })}
      />,
    );
    expect(screen.getByText(/1 check-in queued/)).toBeInTheDocument();
  });

  it('pluralizes when queuedCount > 1', () => {
    render(
      <OfflineQueueIndicator
        state={baseState({ mode: 'offline', queuedCount: 3 })}
      />,
    );
    expect(screen.getByText(/3 check-ins queued/)).toBeInTheDocument();
  });

  it('shows Send Now only when online with queued items AND onSync is provided', async () => {
    const user = userEvent.setup();
    const onSync = vi.fn();
    render(
      <OfflineQueueIndicator
        state={baseState({ queuedCount: 2 })}
        onSync={onSync}
      />,
    );
    const button = screen.getByRole('button', { name: /send now/i });
    await user.click(button);
    expect(onSync).toHaveBeenCalledOnce();
  });

  it('does NOT show Send Now while offline', () => {
    render(
      <OfflineQueueIndicator
        state={baseState({ mode: 'offline', queuedCount: 2 })}
        onSync={vi.fn()}
      />,
    );
    expect(screen.queryByRole('button', { name: /send now/i })).toBeNull();
  });

  it('renders the syncing state banner', () => {
    render(
      <OfflineQueueIndicator
        state={baseState({ queuedCount: 1, syncStatus: 'syncing' })}
      />,
    );
    expect(screen.getByText(/syncing check-ins/i)).toBeInTheDocument();
  });

  it('renders the success state banner with completed count', () => {
    render(
      <OfflineQueueIndicator
        state={baseState({
          syncStatus: 'success',
          lastSyncResults: [
            { success: true, isDuplicate: false },
            { success: true, isDuplicate: true },
          ] as OfflineCheckinState['lastSyncResults'],
        })}
      />,
    );
    expect(screen.getByText(/all done/i)).toBeInTheDocument();
    expect(screen.getByText(/2 check-ins synced/i)).toBeInTheDocument();
    // Note about duplicates is appended.
    expect(screen.getByText(/already checked in/i)).toBeInTheDocument();
  });

  it('renders the error state banner and fires onSync from the Retry button', async () => {
    const user = userEvent.setup();
    const onSync = vi.fn();
    render(
      <OfflineQueueIndicator
        state={baseState({ syncStatus: 'error' })}
        onSync={onSync}
      />,
    );
    expect(screen.getByText(/sync failed/i)).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /retry/i }));
    expect(onSync).toHaveBeenCalledOnce();
  });
});
