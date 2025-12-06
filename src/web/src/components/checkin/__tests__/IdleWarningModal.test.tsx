import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import { IdleWarningModal } from '../IdleWarningModal';

describe('IdleWarningModal', () => {
  it('should not render when isOpen is false', () => {
    render(
      <IdleWarningModal
        isOpen={false}
        secondsRemaining={10}
        onStayActive={vi.fn()}
      />
    );

    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument();
  });

  it('should render when isOpen is true', () => {
    render(
      <IdleWarningModal
        isOpen={true}
        secondsRemaining={10}
        onStayActive={vi.fn()}
      />
    );

    expect(screen.getByRole('alertdialog')).toBeInTheDocument();
  });

  it('should display the correct title', () => {
    render(
      <IdleWarningModal
        isOpen={true}
        secondsRemaining={10}
        onStayActive={vi.fn()}
      />
    );

    expect(screen.getByText('Still There?')).toBeInTheDocument();
  });

  it('should display the seconds remaining', () => {
    render(
      <IdleWarningModal
        isOpen={true}
        secondsRemaining={10}
        onStayActive={vi.fn()}
      />
    );

    expect(screen.getByText('10')).toBeInTheDocument();
    expect(screen.getByText('seconds')).toBeInTheDocument();
  });

  it('should use singular "second" when secondsRemaining is 1', () => {
    render(
      <IdleWarningModal
        isOpen={true}
        secondsRemaining={1}
        onStayActive={vi.fn()}
      />
    );

    expect(screen.getByText('1')).toBeInTheDocument();
    expect(screen.getByText('second')).toBeInTheDocument();
  });

  it('should use plural "seconds" when secondsRemaining is not 1', () => {
    render(
      <IdleWarningModal
        isOpen={true}
        secondsRemaining={5}
        onStayActive={vi.fn()}
      />
    );

    expect(screen.getByText('5')).toBeInTheDocument();
    expect(screen.getByText('seconds')).toBeInTheDocument();
  });

  it('should call onStayActive when Continue button is clicked', async () => {
    const user = userEvent.setup();
    const onStayActive = vi.fn();

    render(
      <IdleWarningModal
        isOpen={true}
        secondsRemaining={10}
        onStayActive={onStayActive}
      />
    );

    const button = screen.getByRole('button', { name: /continue check-in/i });
    await user.click(button);

    expect(onStayActive).toHaveBeenCalledTimes(1);
  });

  it('should call onStayActive when backdrop is clicked', async () => {
    const user = userEvent.setup();
    const onStayActive = vi.fn();

    render(
      <IdleWarningModal
        isOpen={true}
        secondsRemaining={10}
        onStayActive={onStayActive}
      />
    );

    const backdrop = screen.getByRole('alertdialog');
    await user.click(backdrop);

    expect(onStayActive).toHaveBeenCalledTimes(1);
  });

  it('should call onStayActive when backdrop is touched', () => {
    const onStayActive = vi.fn();

    render(
      <IdleWarningModal
        isOpen={true}
        secondsRemaining={10}
        onStayActive={onStayActive}
      />
    );

    const backdrop = screen.getByRole('alertdialog');

    // Simulate touch event using native event
    const touchStartEvent = new TouchEvent('touchstart', {
      bubbles: true,
      cancelable: true,
      touches: [{ clientX: 100, clientY: 100 } as Touch],
    });
    backdrop.dispatchEvent(touchStartEvent);

    expect(onStayActive).toHaveBeenCalledTimes(1);
  });

  it('should have proper ARIA attributes', () => {
    render(
      <IdleWarningModal
        isOpen={true}
        secondsRemaining={10}
        onStayActive={vi.fn()}
      />
    );

    const dialog = screen.getByRole('alertdialog');
    expect(dialog).toHaveAttribute('aria-labelledby', 'idle-warning-title');
    expect(dialog).toHaveAttribute('aria-describedby', 'idle-warning-description');

    expect(screen.getByText('Still There?')).toHaveAttribute('id', 'idle-warning-title');
    expect(screen.getByText(/your session will reset in/i)).toHaveAttribute(
      'id',
      'idle-warning-description'
    );
  });

  it('should have touch-optimized button size (min 48px)', () => {
    render(
      <IdleWarningModal
        isOpen={true}
        secondsRemaining={10}
        onStayActive={vi.fn()}
      />
    );

    const button = screen.getByRole('button', { name: /continue check-in/i });

    // Check if button has minimum touch target size classes
    expect(button).toHaveClass('min-h-[80px]');
    expect(button).toHaveClass('min-w-[300px]');
  });

  it('should display helper text about tapping anywhere', () => {
    render(
      <IdleWarningModal
        isOpen={true}
        secondsRemaining={10}
        onStayActive={vi.fn()}
      />
    );

    expect(screen.getByText('Or tap anywhere to continue')).toBeInTheDocument();
  });

  it('should update countdown when secondsRemaining changes', () => {
    const { rerender } = render(
      <IdleWarningModal
        isOpen={true}
        secondsRemaining={10}
        onStayActive={vi.fn()}
      />
    );

    expect(screen.getByText('10')).toBeInTheDocument();

    rerender(
      <IdleWarningModal
        isOpen={true}
        secondsRemaining={5}
        onStayActive={vi.fn()}
      />
    );

    expect(screen.getByText('5')).toBeInTheDocument();
    expect(screen.queryByText('10')).not.toBeInTheDocument();
  });

  it('should render with correct z-index for overlay', () => {
    render(
      <IdleWarningModal
        isOpen={true}
        secondsRemaining={10}
        onStayActive={vi.fn()}
      />
    );

    const dialog = screen.getByRole('alertdialog');
    expect(dialog).toHaveClass('z-50');
  });

  it('should focus the Continue button when modal opens', () => {
    const { rerender } = render(
      <IdleWarningModal
        isOpen={false}
        secondsRemaining={10}
        onStayActive={vi.fn()}
      />
    );

    rerender(
      <IdleWarningModal
        isOpen={true}
        secondsRemaining={10}
        onStayActive={vi.fn()}
      />
    );

    const button = screen.getByRole('button', { name: /continue check-in/i });
    expect(button).toHaveFocus();
  });
});
