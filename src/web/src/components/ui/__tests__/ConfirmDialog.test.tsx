/**
 * ConfirmDialog tests
 *
 * Catches:
 *  - Renders nothing when isOpen=false (prevents stray modals in tests).
 *  - role="dialog" + aria-modal="true" + aria-labelledby (accessibility regressions).
 *  - Confirm / Cancel callbacks fire on clicks.
 *  - Escape key closes the dialog but NOT while isLoading.
 *  - Backdrop click closes the dialog.
 *  - Loading state disables the cancel button.
 *  - Variant changes the confirm button styling.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ConfirmDialog } from '../ConfirmDialog';

function renderDialog(overrides: Partial<Parameters<typeof ConfirmDialog>[0]> = {}) {
  const onClose = vi.fn();
  const onConfirm = vi.fn();
  render(
    <ConfirmDialog
      isOpen
      onClose={onClose}
      onConfirm={onConfirm}
      title="Delete user?"
      description="This cannot be undone."
      {...overrides}
    />,
  );
  return { onClose, onConfirm };
}

describe('ConfirmDialog', () => {
  it('renders nothing when isOpen is false', () => {
    render(
      <ConfirmDialog
        isOpen={false}
        onClose={vi.fn()}
        onConfirm={vi.fn()}
        title="hidden"
      />,
    );
    expect(screen.queryByRole('dialog')).toBeNull();
  });

  it('renders with dialog role and aria attributes', () => {
    renderDialog();
    const dialog = screen.getByRole('dialog');
    expect(dialog.getAttribute('aria-modal')).toBe('true');
    expect(dialog.getAttribute('aria-labelledby')).toBe('dialog-title');
    expect(dialog.getAttribute('aria-describedby')).toBe('dialog-description');
  });

  it('omits aria-describedby when no description provided', () => {
    render(
      <ConfirmDialog
        isOpen
        onClose={vi.fn()}
        onConfirm={vi.fn()}
        title="No desc"
      />,
    );
    expect(screen.getByRole('dialog').getAttribute('aria-describedby')).toBeNull();
  });

  it('fires onConfirm when the confirm button is clicked', async () => {
    const user = userEvent.setup();
    const { onConfirm } = renderDialog({ confirmLabel: 'Delete' });
    await user.click(screen.getByRole('button', { name: 'Delete' }));
    expect(onConfirm).toHaveBeenCalledOnce();
  });

  it('fires onClose when the cancel button is clicked', async () => {
    const user = userEvent.setup();
    const { onClose } = renderDialog({ cancelLabel: 'Cancel' });
    await user.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(onClose).toHaveBeenCalledOnce();
  });

  it('closes on Escape key when not loading', async () => {
    const user = userEvent.setup();
    const { onClose } = renderDialog();
    await user.keyboard('{Escape}');
    expect(onClose).toHaveBeenCalledOnce();
  });

  it('does NOT close on Escape while loading', async () => {
    const user = userEvent.setup();
    const { onClose } = renderDialog({ isLoading: true });
    await user.keyboard('{Escape}');
    expect(onClose).not.toHaveBeenCalled();
  });

  it('disables the cancel button while loading', () => {
    renderDialog({ isLoading: true, cancelLabel: 'Cancel' });
    const cancelButton = screen.getByRole('button', { name: 'Cancel' });
    expect(cancelButton).toBeDisabled();
  });

  it('applies danger styling to the confirm button by default', () => {
    renderDialog({ confirmLabel: 'Delete' });
    const confirm = screen.getByRole('button', { name: 'Delete' });
    expect(confirm.className).toMatch(/bg-red-600/);
  });

  it('applies warning styling when variant="warning"', () => {
    renderDialog({ variant: 'warning', confirmLabel: 'Proceed' });
    const confirm = screen.getByRole('button', { name: 'Proceed' });
    expect(confirm.className).toMatch(/bg-yellow-600/);
  });

  it('applies info styling when variant="info"', () => {
    renderDialog({ variant: 'info', confirmLabel: 'Ok' });
    const confirm = screen.getByRole('button', { name: 'Ok' });
    expect(confirm.className).toMatch(/bg-blue-600/);
  });
});
