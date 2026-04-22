/**
 * PWAUpdatePrompt tests
 *
 * Catches:
 *  - Hidden until needRefresh=true.
 *  - Shows the "Update Now" and "Later" buttons when visible.
 *  - onUpdate fires when the user confirms the update.
 *  - Later button dismisses the banner.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { PWAUpdatePrompt } from '../PWAUpdatePrompt';

describe('PWAUpdatePrompt', () => {
  it('is hidden when needRefresh=false', () => {
    render(<PWAUpdatePrompt onUpdate={vi.fn()} needRefresh={false} />);
    expect(screen.queryByTestId('pwa-update-prompt')).toBeNull();
  });

  it('shows when needRefresh becomes true', () => {
    render(<PWAUpdatePrompt onUpdate={vi.fn()} needRefresh />);
    expect(screen.getByTestId('pwa-update-prompt')).toBeInTheDocument();
    expect(screen.getByText(/new version available/i)).toBeInTheDocument();
  });

  it('fires onUpdate when Update Now is clicked', async () => {
    const user = userEvent.setup();
    const onUpdate = vi.fn();
    render(<PWAUpdatePrompt onUpdate={onUpdate} needRefresh />);

    await user.click(screen.getByRole('button', { name: /update now/i }));
    expect(onUpdate).toHaveBeenCalledOnce();
  });

  it('dismisses the banner when Later is clicked', async () => {
    const user = userEvent.setup();
    render(<PWAUpdatePrompt onUpdate={vi.fn()} needRefresh />);

    expect(screen.getByTestId('pwa-update-prompt')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /later/i }));
    expect(screen.queryByTestId('pwa-update-prompt')).toBeNull();
  });
});
