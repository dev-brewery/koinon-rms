/**
 * Skeleton tests
 *
 * Catches:
 *  - role="status" + aria-label exposed for assistive tech (catches a11y regression).
 *  - Variant controls the rounding class (rounded / rounded-full / rounded-lg).
 *  - width/height props render as px when numeric and pass-through when string.
 *  - Skeleton.Text multi-line mode renders `lines` placeholders.
 *  - Skeleton.Avatar uses circular variant.
 *  - Skeleton.Card composes rectangle + text blocks.
 */
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Skeleton } from '../Skeleton';

describe('Skeleton', () => {
  it('renders with role="status" and an aria-label', () => {
    render(<Skeleton />);
    const el = screen.getByRole('status');
    expect(el.getAttribute('aria-label')).toBe('Loading content');
  });

  it('applies the rectangular rounding by default', () => {
    const { container } = render(<Skeleton />);
    expect(container.firstChild).toHaveClass('rounded-lg');
  });

  it('applies the text rounding for variant="text"', () => {
    const { container } = render(<Skeleton variant="text" />);
    expect(container.firstChild).toHaveClass('rounded');
  });

  it('applies the circular rounding for variant="circular"', () => {
    const { container } = render(<Skeleton variant="circular" />);
    expect(container.firstChild).toHaveClass('rounded-full');
  });

  it('converts numeric width/height to pixel values', () => {
    const { container } = render(<Skeleton width={100} height={40} />);
    const el = container.firstChild as HTMLElement;
    expect(el.style.width).toBe('100px');
    expect(el.style.height).toBe('40px');
  });

  it('passes string width through unchanged', () => {
    const { container } = render(<Skeleton width="75%" />);
    const el = container.firstChild as HTMLElement;
    expect(el.style.width).toBe('75%');
  });
});

describe('Skeleton.Text', () => {
  it('renders a single placeholder when lines=1', () => {
    const { container } = render(<Skeleton.Text />);
    // One status element for a single line.
    expect(container.querySelectorAll('[role="status"]')).toHaveLength(1);
  });

  it('renders one placeholder per line when lines>1', () => {
    const { container } = render(<Skeleton.Text lines={3} />);
    expect(container.querySelectorAll('[role="status"]')).toHaveLength(3);
  });
});

describe('Skeleton.Avatar', () => {
  it('uses the circular variant', () => {
    const { container } = render(<Skeleton.Avatar />);
    expect(container.firstChild).toHaveClass('rounded-full');
  });

  it('applies the size prop as both width and height in pixels', () => {
    const { container } = render(<Skeleton.Avatar size={32} />);
    const el = container.firstChild as HTMLElement;
    expect(el.style.width).toBe('32px');
    expect(el.style.height).toBe('32px');
  });
});

describe('Skeleton.Card', () => {
  it('renders multiple placeholders (rectangle plus text lines)', () => {
    const { container } = render(<Skeleton.Card />);
    expect(container.querySelectorAll('[role="status"]').length).toBeGreaterThanOrEqual(3);
  });
});
