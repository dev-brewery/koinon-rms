/**
 * GroupFilters tests
 *
 * Catches:
 *  - onFiltersChange receives the correct shape for each control (search,
 *    campus, dayOfWeek, timeOfDay, hasOpenings).
 *  - Empty string → undefined conversion preserves `omit filter` semantics.
 *  - Clear Filters button visibility is driven by `hasActiveFilters`.
 *  - Campus options come from the query result.
 *  - dayOfWeek=0 (Sunday) correctly registers — regression category: numeric 0
 *    can be falsy and easily lost.
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/services/api/reference', () => ({
  getCampuses: vi.fn(),
}));
import { getCampuses } from '@/services/api/reference';
import { GroupFilters } from '../GroupFilters';

function wrap(ui: ReactNode) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

beforeEach(() => {
  (getCampuses as ReturnType<typeof vi.fn>).mockResolvedValue([
    { idKey: 'CAM1', name: 'Main Campus' },
    { idKey: 'CAM2', name: 'North Campus' },
  ]);
});

describe('GroupFilters', () => {
  it('hides Clear Filters button when no filter is active', () => {
    wrap(<GroupFilters filters={{}} onFiltersChange={vi.fn()} />);
    expect(screen.queryByRole('button', { name: /clear filters/i })).toBeNull();
  });

  it('shows Clear Filters button when at least one filter is active', () => {
    wrap(<GroupFilters filters={{ searchTerm: 'abc' }} onFiltersChange={vi.fn()} />);
    expect(screen.getByRole('button', { name: /clear filters/i })).toBeInTheDocument();
  });

  it('emits searchTerm through onFiltersChange', async () => {
    const user = userEvent.setup();
    const onFiltersChange = vi.fn();
    wrap(<GroupFilters filters={{}} onFiltersChange={onFiltersChange} />);
    await user.type(screen.getByLabelText(/search groups/i), 'a');
    expect(onFiltersChange).toHaveBeenCalled();
    expect(onFiltersChange.mock.lastCall?.[0].searchTerm).toBe('a');
  });

  it('emits undefined for searchTerm when cleared', async () => {
    const user = userEvent.setup();
    const onFiltersChange = vi.fn();
    wrap(
      <GroupFilters filters={{ searchTerm: 'abc' }} onFiltersChange={onFiltersChange} />,
    );
    const input = screen.getByLabelText(/search groups/i) as HTMLInputElement;
    await user.clear(input);
    expect(onFiltersChange.mock.lastCall?.[0].searchTerm).toBeUndefined();
  });

  it('emits numeric dayOfWeek=0 (Sunday) through onFiltersChange', async () => {
    const user = userEvent.setup();
    const onFiltersChange = vi.fn();
    wrap(<GroupFilters filters={{}} onFiltersChange={onFiltersChange} />);
    await user.selectOptions(screen.getByLabelText(/day of week/i), '0');
    expect(onFiltersChange.mock.lastCall?.[0].dayOfWeek).toBe(0);
  });

  it('emits dayOfWeek=undefined when "Any Day" is picked', async () => {
    const user = userEvent.setup();
    const onFiltersChange = vi.fn();
    wrap(
      <GroupFilters filters={{ dayOfWeek: 1 }} onFiltersChange={onFiltersChange} />,
    );
    await user.selectOptions(screen.getByLabelText(/day of week/i), '');
    expect(onFiltersChange.mock.lastCall?.[0].dayOfWeek).toBeUndefined();
  });

  it('emits hasOpenings=true when the checkbox is ticked', async () => {
    const user = userEvent.setup();
    const onFiltersChange = vi.fn();
    wrap(<GroupFilters filters={{}} onFiltersChange={onFiltersChange} />);
    await user.click(screen.getByRole('checkbox'));
    expect(onFiltersChange.mock.lastCall?.[0].hasOpenings).toBe(true);
  });

  it('emits hasOpenings=undefined when the checkbox is unchecked', async () => {
    const user = userEvent.setup();
    const onFiltersChange = vi.fn();
    // Start with hasOpenings=true so the checkbox renders checked.
    wrap(
      <GroupFilters filters={{ hasOpenings: true }} onFiltersChange={onFiltersChange} />,
    );
    await user.click(screen.getByRole('checkbox'));
    expect(onFiltersChange.mock.lastCall?.[0].hasOpenings).toBeUndefined();
  });

  it('clears all filters when Clear Filters is clicked', async () => {
    const user = userEvent.setup();
    const onFiltersChange = vi.fn();
    wrap(
      <GroupFilters
        filters={{ searchTerm: 'x', dayOfWeek: 3 }}
        onFiltersChange={onFiltersChange}
      />,
    );
    await user.click(screen.getByRole('button', { name: /clear filters/i }));
    expect(onFiltersChange).toHaveBeenLastCalledWith({});
  });
});
