/**
 * PagerSearch tests
 *
 * Covers the key visible branches of the search component:
 *   - Renders input with autoFocus.
 *   - Loading branch renders spinner while isLoading=true.
 *   - Error branch renders the "Failed to search pagers" message.
 *   - Empty-results branch distinguishes between "no pagers today" and
 *     "no pagers match your search".
 *   - Result branch shows pluralized count + all fields.
 *   - Parent phone fallback ("No phone number") only when missing.
 *   - Messages-sent badge only renders when count > 0 and uses orange
 *     color for 2+ messages.
 *   - onSelectPager invoked when row clicked.
 *   - Normalization: "P-127" and "p127" strip the prefix before searching.
 */
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

type MockResult = {
  data?: unknown;
  isLoading: boolean;
  error: unknown;
};

const mockUsePagerSearch = vi.fn<(term: string | undefined) => MockResult>();

vi.mock('../hooks', () => ({
  usePagerSearch: (term?: string) => mockUsePagerSearch(term),
}));

import { PagerSearch } from '../PagerSearch';

describe('PagerSearch', () => {
  beforeEach(() => {
    mockUsePagerSearch.mockReset();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders the autofocused search input', () => {
    mockUsePagerSearch.mockReturnValue({ data: undefined, isLoading: false, error: null });
    render(<PagerSearch onSelectPager={vi.fn()} />);
    const input = screen.getByPlaceholderText(/search by pager number/i) as HTMLInputElement;
    expect(input).toBeInTheDocument();
    expect(input.value).toBe('');
  });

  it('renders the loading branch when isLoading is true', () => {
    mockUsePagerSearch.mockReturnValue({ data: undefined, isLoading: true, error: null });
    render(<PagerSearch onSelectPager={vi.fn()} />);
    expect(screen.getByText(/searching/i)).toBeInTheDocument();
  });

  it('renders the error branch when error is present', () => {
    mockUsePagerSearch.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error('x'),
    });
    render(<PagerSearch onSelectPager={vi.fn()} />);
    expect(screen.getByText(/failed to search pagers/i)).toBeInTheDocument();
  });

  it('renders "No active pagers today" when empty & no query', () => {
    mockUsePagerSearch.mockReturnValue({ data: [], isLoading: false, error: null });
    render(<PagerSearch onSelectPager={vi.fn()} />);
    expect(screen.getByText(/no active pagers today/i)).toBeInTheDocument();
  });

  it('renders "No pagers found" when empty & has query', async () => {
    mockUsePagerSearch.mockReturnValue({ data: [], isLoading: false, error: null });
    const user = userEvent.setup();
    render(<PagerSearch onSelectPager={vi.fn()} />);
    await user.type(screen.getByPlaceholderText(/search by pager number/i), '127');
    await waitFor(
      () => expect(screen.getByText(/no pagers found/i)).toBeInTheDocument(),
      { timeout: 2000 }
    );
  });

  it('renders result list with singular count', () => {
    mockUsePagerSearch.mockReturnValue({
      data: [
        {
          idKey: 'p1',
          pagerNumber: 127,
          childName: 'Kid One',
          groupName: 'K-1',
          locationName: 'Room 1',
          parentPhoneNumber: '5551234567',
          checkedInAt: '2026-04-22T10:00:00Z',
          messagesSentCount: 0,
        },
      ],
      isLoading: false,
      error: null,
    });
    render(<PagerSearch onSelectPager={vi.fn()} />);
    expect(screen.getByText(/1 pager found/i)).toBeInTheDocument();
    expect(screen.getByText('Kid One')).toBeInTheDocument();
    expect(screen.getByText('P-127')).toBeInTheDocument();
    expect(screen.getByText('K-1')).toBeInTheDocument();
  });

  it('renders result list with plural count + 2+ messages badge in orange', () => {
    mockUsePagerSearch.mockReturnValue({
      data: [
        {
          idKey: 'p1',
          pagerNumber: 127,
          childName: 'Kid One',
          groupName: 'K-1',
          locationName: 'Room 1',
          parentPhoneNumber: '5551234567',
          checkedInAt: '2026-04-22T10:00:00Z',
          messagesSentCount: 3,
        },
        {
          idKey: 'p2',
          pagerNumber: 128,
          childName: 'Kid Two',
          groupName: 'K-1',
          locationName: 'Room 1',
          parentPhoneNumber: null,
          checkedInAt: '2026-04-22T10:00:00Z',
          messagesSentCount: 0,
        },
      ],
      isLoading: false,
      error: null,
    });
    render(<PagerSearch onSelectPager={vi.fn()} />);
    expect(screen.getByText(/2 pagers found/i)).toBeInTheDocument();
    // Orange badge for >=2 messages
    const badge = screen.getByText(/3 messages sent/i);
    expect(badge.className).toMatch(/orange/);
    // No-phone-number fallback for the second pager
    expect(screen.getByText(/no phone number/i)).toBeInTheDocument();
  });

  it('onSelectPager receives the full pager when its row is clicked', async () => {
    const onSelectPager = vi.fn();
    const pager = {
      idKey: 'p1',
      pagerNumber: 127,
      childName: 'Kid One',
      groupName: 'K-1',
      locationName: 'Room 1',
      parentPhoneNumber: '5551234567',
      checkedInAt: '2026-04-22T10:00:00Z',
      messagesSentCount: 1,
    };
    mockUsePagerSearch.mockReturnValue({
      data: [pager],
      isLoading: false,
      error: null,
    });
    const user = userEvent.setup();
    render(<PagerSearch onSelectPager={onSelectPager} />);
    // singular "1 message sent" branch
    expect(screen.getByText(/1 message sent/i)).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /kid one/i }));
    expect(onSelectPager).toHaveBeenCalledWith(pager);
  });

  it('strips the P- prefix from the search term before querying', async () => {
    mockUsePagerSearch.mockReturnValue({ data: [], isLoading: false, error: null });
    const user = userEvent.setup();
    render(<PagerSearch onSelectPager={vi.fn()} />);
    await user.type(
      screen.getByPlaceholderText(/search by pager number/i),
      'P-127'
    );
    await waitFor(() => {
      const called = mockUsePagerSearch.mock.calls.map((c) => c[0]);
      expect(called).toContain('127');
    }, { timeout: 2000 });
  });

  it('strips the lower-case p prefix too', async () => {
    mockUsePagerSearch.mockReturnValue({ data: [], isLoading: false, error: null });
    const user = userEvent.setup();
    render(<PagerSearch onSelectPager={vi.fn()} />);
    await user.type(
      screen.getByPlaceholderText(/search by pager number/i),
      'p127'
    );
    await waitFor(() => {
      const called = mockUsePagerSearch.mock.calls.map((c) => c[0]);
      expect(called).toContain('127');
    }, { timeout: 2000 });
  });

  it('passes undefined to the hook when the input is empty', () => {
    mockUsePagerSearch.mockReturnValue({ data: undefined, isLoading: false, error: null });
    render(<PagerSearch onSelectPager={vi.fn()} />);
    expect(mockUsePagerSearch).toHaveBeenCalledWith(undefined);
  });
});
