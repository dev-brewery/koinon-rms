/**
 * StatCard tests
 *
 * Catches:
 *  - Title, value, subtitle rendering.
 *  - Colour variant maps to bg/text classes.
 *  - Trend (up/down/neutral) renders the correct icon + colour.
 *  - trendValue only renders when both trend and trendValue are provided.
 */
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { StatCard } from '../StatCard';

function icon() {
  return <svg data-testid="stat-icon" />;
}

describe('StatCard', () => {
  it('renders title, value, and subtitle', () => {
    render(
      <StatCard
        title="Active Members"
        value={42}
        subtitle="up from last week"
        icon={icon()}
        color="blue"
      />,
    );
    expect(screen.getByText('Active Members')).toBeInTheDocument();
    expect(screen.getByText('42')).toBeInTheDocument();
    expect(screen.getByText('up from last week')).toBeInTheDocument();
  });

  it('applies the blue color classes to the icon container', () => {
    render(<StatCard title="t" value={1} icon={icon()} color="blue" />);
    const card = screen.getByTestId('stat-card');
    // The icon wrapper is a div with bg-blue-50 text-blue-600.
    expect(card.querySelector('.bg-blue-50')).not.toBeNull();
  });

  it('applies the green color classes when color="green"', () => {
    render(<StatCard title="t" value={1} icon={icon()} color="green" />);
    expect(screen.getByTestId('stat-card').querySelector('.bg-green-50')).not.toBeNull();
  });

  it('renders the up trend with green colour when trend=up and trendValue is set', () => {
    render(
      <StatCard
        title="t"
        value={1}
        icon={icon()}
        color="blue"
        trend="up"
        trendValue="+5%"
      />,
    );
    const trendWrapper = screen.getByText('+5%').parentElement;
    expect(trendWrapper?.className).toMatch(/text-green-600/);
  });

  it('renders the down trend with red colour when trend=down', () => {
    render(
      <StatCard
        title="t"
        value={1}
        icon={icon()}
        color="blue"
        trend="down"
        trendValue="-3%"
      />,
    );
    const trendWrapper = screen.getByText('-3%').parentElement;
    expect(trendWrapper?.className).toMatch(/text-red-600/);
  });

  it('does not render trend info when only trend is set (without trendValue)', () => {
    const { container } = render(
      <StatCard title="t" value={1} icon={icon()} color="blue" trend="up" />,
    );
    // No element with the trend colour classes should exist.
    expect(container.querySelectorAll('.text-green-600')).toHaveLength(0);
  });
});
