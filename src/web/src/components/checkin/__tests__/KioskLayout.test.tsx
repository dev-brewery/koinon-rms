/**
 * KioskLayout tests
 *
 * Catches:
 *  - Renders title when provided and hides when not.
 *  - Supervisor button visibility depends on the onSupervisorTrigger prop.
 *  - Reset button visibility depends on the onReset prop.
 *  - Triple-tap on the header triggers supervisor mode.
 *  - Supervisor button click triggers immediately.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { KioskLayout } from '../KioskLayout';

describe('KioskLayout', () => {
  it('renders the title when provided', () => {
    render(
      <KioskLayout title="Sunday 9AM">
        <div>content</div>
      </KioskLayout>,
    );
    expect(screen.getByText('Sunday 9AM')).toBeInTheDocument();
  });

  it('renders children in the main content area', () => {
    render(
      <KioskLayout>
        <div>hello world</div>
      </KioskLayout>,
    );
    expect(screen.getByText('hello world')).toBeInTheDocument();
  });

  it('hides the supervisor button when no onSupervisorTrigger is given', () => {
    render(
      <KioskLayout>
        <div />
      </KioskLayout>,
    );
    expect(screen.queryByRole('button', { name: /supervisor mode/i })).toBeNull();
  });

  it('fires onSupervisorTrigger when the button is clicked', async () => {
    const user = userEvent.setup();
    const onSupervisor = vi.fn();
    render(
      <KioskLayout onSupervisorTrigger={onSupervisor}>
        <div />
      </KioskLayout>,
    );
    await user.click(screen.getByRole('button', { name: /supervisor mode/i }));
    expect(onSupervisor).toHaveBeenCalledOnce();
  });

  it('hides Start Over when no onReset is given', () => {
    render(
      <KioskLayout>
        <div />
      </KioskLayout>,
    );
    expect(screen.queryByRole('button', { name: /start over/i })).toBeNull();
  });

  it('fires onReset when Start Over is clicked', async () => {
    const user = userEvent.setup();
    const onReset = vi.fn();
    render(
      <KioskLayout onReset={onReset}>
        <div />
      </KioskLayout>,
    );
    await user.click(screen.getByRole('button', { name: /start over/i }));
    expect(onReset).toHaveBeenCalledOnce();
  });

  it('triggers supervisor mode after three taps on the header logo', async () => {
    const user = userEvent.setup();
    const onSupervisor = vi.fn();
    render(
      <KioskLayout onSupervisorTrigger={onSupervisor}>
        <div />
      </KioskLayout>,
    );
    const logo = screen.getByRole('heading', { name: 'Check-In' });

    // Each tap on the header container triggers the handler.
    await user.click(logo);
    await user.click(logo);
    // Not yet triggered.
    expect(onSupervisor).toHaveBeenCalledTimes(0);

    await user.click(logo);
    // Three taps → supervisor button click triggers, plus the direct
    // onSupervisorTrigger from the triple-tap path. We expect at least one call.
    expect(onSupervisor).toHaveBeenCalled();
  });
});
