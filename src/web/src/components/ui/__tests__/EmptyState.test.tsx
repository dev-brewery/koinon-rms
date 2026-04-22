/**
 * EmptyState tests
 *
 * Catches:
 *  - Title always renders at h3 level.
 *  - Description is optional (absent when not provided).
 *  - Action button only renders when provided, clicking fires the callback.
 *  - Custom icon overrides the default inbox icon.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { EmptyState } from '../EmptyState';

describe('EmptyState', () => {
  it('renders the title as a heading', () => {
    render(<EmptyState title="No items" />);
    expect(screen.getByRole('heading', { level: 3, name: 'No items' })).toBeInTheDocument();
  });

  it('omits description when not provided', () => {
    const { container } = render(<EmptyState title="No items" />);
    expect(container.querySelectorAll('p')).toHaveLength(0);
  });

  it('renders the description when provided', () => {
    render(<EmptyState title="No items" description="Try again" />);
    expect(screen.getByText('Try again')).toBeInTheDocument();
  });

  it('does not render an action button when no action is provided', () => {
    render(<EmptyState title="No items" />);
    expect(screen.queryByRole('button')).toBeNull();
  });

  it('renders the action button and fires onClick when clicked', async () => {
    const user = userEvent.setup();
    const onClick = vi.fn();
    render(
      <EmptyState
        title="No items"
        action={{ label: 'Create one', onClick }}
      />,
    );
    const btn = screen.getByRole('button', { name: 'Create one' });
    await user.click(btn);
    expect(onClick).toHaveBeenCalledOnce();
  });

  it('uses a custom icon when provided (default inbox icon is replaced)', () => {
    const CustomIcon = () => <svg data-testid="custom-icon" />;
    render(<EmptyState title="t" icon={<CustomIcon />} />);
    expect(screen.getByTestId('custom-icon')).toBeInTheDocument();
  });
});
