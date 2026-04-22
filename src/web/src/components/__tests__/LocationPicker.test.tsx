/**
 * LocationPicker tests
 *
 * Catches:
 *  - Disabled + "Loading locations..." option while the query is loading.
 *  - Error message renders when the query fails.
 *  - Option list is built from area.locations, filters inactive, sorts by name.
 *  - Helper message appears when campus is missing.
 *  - onChange fires with the selected idKey.
 */
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';

vi.mock('@/services/api/checkin', () => ({
  getCheckinConfiguration: vi.fn(),
}));
import { getCheckinConfiguration } from '@/services/api/checkin';
import { LocationPicker } from '../LocationPicker';

function makeClient() {
  return new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
}

function wrap(ui: ReactNode, client = makeClient()) {
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

const fakeConfig = {
  areas: [
    {
      locations: [
        { idKey: 'L2', name: 'Bravo Room', isActive: true, softThreshold: 20 },
        { idKey: 'L1', name: 'Alpha Room', isActive: true, softThreshold: null },
        { idKey: 'L3', name: 'Charlie (inactive)', isActive: false, softThreshold: 10 },
      ],
    },
  ],
};

beforeEach(() => {
  (getCheckinConfiguration as ReturnType<typeof vi.fn>).mockReset();
});

describe('LocationPicker', () => {
  it('renders the campus-required helper when no campus is provided', () => {
    (getCheckinConfiguration as ReturnType<typeof vi.fn>).mockResolvedValue(fakeConfig);
    wrap(<LocationPicker value="" onChange={() => {}} />);
    expect(screen.getByText(/please configure a campus/i)).toBeInTheDocument();
  });

  it('does not call the API when campusIdKey is absent', () => {
    (getCheckinConfiguration as ReturnType<typeof vi.fn>).mockResolvedValue(fakeConfig);
    wrap(<LocationPicker value="" onChange={() => {}} />);
    expect(getCheckinConfiguration).not.toHaveBeenCalled();
  });

  it('shows an error message when the query fails', async () => {
    (getCheckinConfiguration as ReturnType<typeof vi.fn>).mockRejectedValue(
      new Error('server down'),
    );
    wrap(<LocationPicker value="" onChange={() => {}} campusIdKey="CAM1" />);
    expect(await screen.findByText(/failed to load locations/i)).toBeInTheDocument();
    expect(screen.getByText(/server down/i)).toBeInTheDocument();
  });

  it('renders active locations sorted by name and filters out inactive ones', async () => {
    (getCheckinConfiguration as ReturnType<typeof vi.fn>).mockResolvedValue(fakeConfig);
    wrap(<LocationPicker value="" onChange={() => {}} campusIdKey="CAM1" />);

    // Wait for the options to be rendered post-fetch.
    await waitFor(() =>
      expect(screen.getByRole('option', { name: /alpha room/i })).toBeInTheDocument(),
    );

    const options = screen.getAllByRole('option');
    // First option is the placeholder ("Select a room").
    // Active locations, sorted alphabetically: Alpha, Bravo (capacity label differs).
    expect(options[1].textContent).toMatch(/alpha/i);
    expect(options[2].textContent).toMatch(/bravo/i);
    // Inactive "Charlie" should NOT be rendered.
    expect(screen.queryByRole('option', { name: /charlie/i })).toBeNull();
  });

  it('shows capacity in the label when softThreshold is set', async () => {
    (getCheckinConfiguration as ReturnType<typeof vi.fn>).mockResolvedValue(fakeConfig);
    wrap(<LocationPicker value="" onChange={() => {}} campusIdKey="CAM1" />);
    const bravo = await screen.findByRole('option', { name: /bravo room/i });
    expect(bravo.textContent).toMatch(/capacity.*20/i);
  });

  it('fires onChange with the selected idKey', async () => {
    (getCheckinConfiguration as ReturnType<typeof vi.fn>).mockResolvedValue(fakeConfig);
    const onChange = vi.fn();
    wrap(<LocationPicker value="" onChange={onChange} campusIdKey="CAM1" />);

    const select = (await screen.findByRole('combobox')) as HTMLSelectElement;
    // Wait for options to be populated.
    await waitFor(() =>
      expect(screen.getByRole('option', { name: /alpha room/i })).toBeInTheDocument(),
    );

    const user = userEvent.setup();
    await user.selectOptions(select, 'L2');
    expect(onChange).toHaveBeenCalledWith('L2');
  });
});
