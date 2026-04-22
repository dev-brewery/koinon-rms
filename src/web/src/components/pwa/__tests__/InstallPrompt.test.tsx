/**
 * InstallPrompt tests
 *
 * Catches:
 *  - Hidden until the beforeinstallprompt event fires.
 *  - Shows install copy after the event is received.
 *  - Install button calls the captured prompt() and then hides on "accepted".
 *  - Dismiss button hides without calling prompt.
 */
import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { InstallPrompt } from '../InstallPrompt';

function fireBeforeInstall(
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>,
): ReturnType<typeof vi.fn> {
  const promptMock = vi.fn().mockResolvedValue(undefined);
  const event: Event & { prompt?: unknown; userChoice?: unknown } = new Event(
    'beforeinstallprompt',
  );
  (event as unknown as { prompt: typeof promptMock }).prompt = promptMock;
  (event as unknown as { userChoice: typeof userChoice }).userChoice = userChoice;

  act(() => {
    window.dispatchEvent(event);
  });

  return promptMock;
}

describe('InstallPrompt', () => {
  it('is hidden until beforeinstallprompt fires', () => {
    render(<InstallPrompt />);
    expect(screen.queryByText(/install check-in app/i)).toBeNull();
  });

  it('shows the install banner after beforeinstallprompt fires', () => {
    render(<InstallPrompt />);
    fireBeforeInstall(Promise.resolve({ outcome: 'accepted' }));
    expect(screen.getByText(/install check-in app/i)).toBeInTheDocument();
  });

  it('calls prompt() and hides the banner when install is accepted', async () => {
    const user = userEvent.setup();
    render(<InstallPrompt />);
    const promptMock = fireBeforeInstall(
      Promise.resolve({ outcome: 'accepted' }),
    );

    await user.click(screen.getByRole('button', { name: /^install$/i }));
    expect(promptMock).toHaveBeenCalled();

    // Banner should be hidden after acceptance.
    await screen.findByText(/install check-in app/i, {}, { timeout: 0 }).catch(() => {});
    expect(screen.queryByText(/install check-in app/i)).toBeNull();
  });

  it('hides the banner when Not Now is clicked (no prompt call)', async () => {
    const user = userEvent.setup();
    render(<InstallPrompt />);
    const promptMock = fireBeforeInstall(
      Promise.resolve({ outcome: 'accepted' }),
    );

    await user.click(screen.getByRole('button', { name: /not now/i }));
    expect(screen.queryByText(/install check-in app/i)).toBeNull();
    expect(promptMock).not.toHaveBeenCalled();
  });

  it('dismiss X button hides the banner', async () => {
    const user = userEvent.setup();
    render(<InstallPrompt />);
    fireBeforeInstall(Promise.resolve({ outcome: 'accepted' }));

    await user.click(screen.getByRole('button', { name: /dismiss install prompt/i }));
    expect(screen.queryByText(/install check-in app/i)).toBeNull();
  });
});
