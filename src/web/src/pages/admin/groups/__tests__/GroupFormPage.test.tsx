/**
 * GroupFormPage tests (bug #690 — parent group + campus dropdowns).
 *
 * Before the fix, the Parent Group select rendered only the
 * "None (Top-level group)" placeholder, and the Campus select rendered only the
 * "All Campuses" placeholder. These tests lock in the wired-up behavior:
 *
 *  - Parent Group options come from `useGroups({ pageSize: 200 })`.
 *  - Campus options come from `useCampuses()`.
 *  - Loading states render a disabled select with a "Loading…" placeholder.
 *  - Error states show inline helper text but keep the select usable.
 *  - The create-mode Group Type select still renders (no regression).
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';

// ---------------------------------------------------------------------------
// Hook mocks
// ---------------------------------------------------------------------------

vi.mock('@/hooks/useGroups', () => ({
  useGroup: vi.fn(),
  useGroups: vi.fn(),
  useCreateGroup: vi.fn(() => ({
    mutateAsync: vi.fn(),
    isPending: false,
  })),
  useUpdateGroup: vi.fn(() => ({
    mutateAsync: vi.fn(),
    isPending: false,
  })),
}));

vi.mock('@/hooks/useGroupTypes', () => ({
  useGroupTypes: vi.fn(),
}));

vi.mock('@/hooks/useCampuses', () => ({
  useCampuses: vi.fn(),
}));

import { useGroup, useGroups } from '@/hooks/useGroups';
import { useGroupTypes } from '@/hooks/useGroupTypes';
import { useCampuses } from '@/hooks/useCampuses';
import { GroupFormPage } from '../GroupFormPage';

const useGroupMock = vi.mocked(useGroup);
const useGroupsMock = vi.mocked(useGroups);
const useGroupTypesMock = vi.mocked(useGroupTypes);
const useCampusesMock = vi.mocked(useCampuses);

// ---------------------------------------------------------------------------
// Harness
// ---------------------------------------------------------------------------

function renderPage(initialPath = '/admin/groups/new'): void {
  const client = new QueryClient({
    defaultOptions: {
      queries: { retry: false, staleTime: 0, gcTime: 0 },
      mutations: { retry: false },
    },
  });
  const Wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={client}>{children}</QueryClientProvider>
  );
  render(
    <Wrapper>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route path="/admin/groups/new" element={<GroupFormPage />} />
          <Route path="/admin/groups/:idKey/edit" element={<GroupFormPage />} />
        </Routes>
      </MemoryRouter>
    </Wrapper>,
  );
}

// A minimal shape that satisfies the bits of the TanStack Query result the
// page actually reads (`data`, `isLoading`, `error`). Cast to `never` at the
// call site to dodge deep type mismatches — this test only cares about DOM.
function queryStub(overrides: Record<string, unknown> = {}) {
  return {
    data: undefined,
    isLoading: false,
    error: null,
    isError: false,
    isSuccess: true,
    refetch: vi.fn(),
    ...overrides,
  };
}

beforeEach(() => {
  vi.clearAllMocks();
  useGroupMock.mockReturnValue(queryStub({ data: undefined }) as never);
  useGroupTypesMock.mockReturnValue(
    queryStub({
      data: [
        { idKey: 'gt1', name: 'Small Group' },
        { idKey: 'gt2', name: 'Serving Team' },
      ],
    }) as never,
  );
  useCampusesMock.mockReturnValue(
    queryStub({
      data: [
        { idKey: 'c1', name: 'Main Campus' },
        { idKey: 'c2', name: 'South Campus' },
      ],
    }) as never,
  );
  useGroupsMock.mockReturnValue(
    queryStub({
      data: {
        data: [
          { idKey: 'g1', name: 'Nursery' },
          { idKey: 'g2', name: 'Preschool' },
        ],
        meta: { page: 1, pageSize: 200, totalCount: 2, totalPages: 1 },
      },
    }) as never,
  );
});

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('GroupFormPage — #690 dropdowns wired', () => {
  it('renders campus options from useCampuses plus the "All Campuses" default', () => {
    renderPage();
    const campusSelect = screen.getByLabelText(/campus/i, {
      selector: '#campusId',
    }) as HTMLSelectElement;
    const option = (text: RegExp) =>
      Array.from(campusSelect.options).find((o) => text.test(o.textContent ?? ''));
    expect(option(/all campuses/i)).toBeTruthy();
    expect(option(/^Main Campus$/)).toBeTruthy();
    expect(option(/^South Campus$/)).toBeTruthy();
    expect(campusSelect.options.length).toBe(3);
  });

  it('renders parent group options from useGroups plus the "None" default', () => {
    renderPage();
    const parentSelect = screen.getByLabelText(/parent group/i) as HTMLSelectElement;
    const option = (text: RegExp) =>
      Array.from(parentSelect.options).find((o) => text.test(o.textContent ?? ''));
    expect(option(/none \(top-level group\)/i)).toBeTruthy();
    expect(option(/^Nursery$/)).toBeTruthy();
    expect(option(/^Preschool$/)).toBeTruthy();
    expect(parentSelect.options.length).toBe(3);
  });

  it('requests a larger page of groups for the parent picker', () => {
    renderPage();
    expect(useGroupsMock).toHaveBeenCalledWith({ pageSize: 200 });
  });

  it('preserves the existing Group Type options in create mode', () => {
    renderPage();
    const typeSelect = screen.getByLabelText(/group type/i) as HTMLSelectElement;
    expect(
      Array.from(typeSelect.options).map((o) => o.textContent),
    ).toEqual(
      expect.arrayContaining(['Small Group', 'Serving Team']),
    );
  });

  it('disables the campus select and shows a loading placeholder while fetching', () => {
    useCampusesMock.mockReturnValue(
      queryStub({ data: undefined, isLoading: true, isSuccess: false }) as never,
    );
    renderPage();
    const campusSelect = screen.getByLabelText(/campus/i, {
      selector: '#campusId',
    }) as HTMLSelectElement;
    expect(campusSelect).toBeDisabled();
    expect(campusSelect.options[0].textContent).toMatch(/loading campuses/i);
  });

  it('disables the parent group select and shows a loading placeholder while fetching', () => {
    useGroupsMock.mockReturnValue(
      queryStub({ data: undefined, isLoading: true, isSuccess: false }) as never,
    );
    renderPage();
    const parentSelect = screen.getByLabelText(/parent group/i) as HTMLSelectElement;
    expect(parentSelect).toBeDisabled();
    expect(parentSelect.options[0].textContent).toMatch(/loading groups/i);
  });

  it('shows inline error copy if campuses fail to load', () => {
    useCampusesMock.mockReturnValue(
      queryStub({
        data: undefined,
        isLoading: false,
        error: new Error('boom'),
        isError: true,
        isSuccess: false,
      }) as never,
    );
    renderPage();
    expect(
      screen.getByText(/could not load campuses\. you can still save without a campus\./i),
    ).toBeInTheDocument();
  });

  it('shows inline error copy if parent groups fail to load', () => {
    useGroupsMock.mockReturnValue(
      queryStub({
        data: undefined,
        isLoading: false,
        error: new Error('boom'),
        isError: true,
        isSuccess: false,
      }) as never,
    );
    renderPage();
    expect(
      screen.getByText(/could not load groups\. you can still save without a parent\./i),
    ).toBeInTheDocument();
  });

  it('pre-selects a parentId from ?parentId=... and disables the parent select', () => {
    renderPage('/admin/groups/new?parentId=g1');
    const parentSelect = screen.getByLabelText(/parent group/i) as HTMLSelectElement;
    expect(parentSelect).toBeDisabled();
    expect(parentSelect.value).toBe('g1');
    expect(
      screen.getByText(/parent group is pre-selected/i),
    ).toBeInTheDocument();
  });
});
