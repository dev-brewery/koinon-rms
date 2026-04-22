/**
 * Breadcrumb tests
 *
 * Catches:
 *  - Renders nothing when explicit items has length <= 1.
 *  - Auto-generates breadcrumbs from the current pathname.
 *  - Path-label map produces human-friendly labels (catches regression if the
 *    mapping entries are dropped).
 *  - Last item is NOT a link; intermediate items ARE links.
 *  - Home icon link always points to /.
 */
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import { Breadcrumb } from '../Breadcrumb';

describe('Breadcrumb', () => {
  it('renders nothing when only one breadcrumb item exists', () => {
    const { container } = render(
      <MemoryRouter initialEntries={['/admin']}>
        <Breadcrumb items={[{ label: 'Admin' }]} />
      </MemoryRouter>,
    );
    expect(container.querySelector('nav')).toBeNull();
  });

  it('renders nothing when auto-generated pathname has no segments', () => {
    const { container } = render(
      <MemoryRouter initialEntries={['/']}>
        <Breadcrumb />
      </MemoryRouter>,
    );
    expect(container.querySelector('nav')).toBeNull();
  });

  it('renders explicit items with the last not linked', () => {
    render(
      <MemoryRouter initialEntries={['/admin/people']}>
        <Breadcrumb
          items={[
            { label: 'Admin', path: '/admin' },
            { label: 'People' },
          ]}
        />
      </MemoryRouter>,
    );
    const linked = screen.getByRole('link', { name: 'Admin' });
    expect(linked).toHaveAttribute('href', '/admin');
    // The current page "People" renders as a span, not a link.
    expect(screen.queryByRole('link', { name: 'People' })).toBeNull();
    expect(screen.getByText('People')).toBeInTheDocument();
  });

  it('auto-generates breadcrumbs from the current pathname', () => {
    render(
      <MemoryRouter initialEntries={['/admin/people']}>
        <Breadcrumb />
      </MemoryRouter>,
    );
    expect(screen.getByRole('link', { name: 'Admin' })).toHaveAttribute('href', '/admin');
    // Last segment is not linked.
    expect(screen.getByText('People')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'People' })).toBeNull();
  });

  it('falls back to the raw segment when no label mapping exists', () => {
    render(
      <MemoryRouter initialEntries={['/admin/frobnicator']}>
        <Breadcrumb />
      </MemoryRouter>,
    );
    // "frobnicator" has no mapping → renders as-is.
    expect(screen.getByText('frobnicator')).toBeInTheDocument();
  });

  it('always renders the Home link', () => {
    render(
      <MemoryRouter initialEntries={['/admin/people']}>
        <Breadcrumb />
      </MemoryRouter>,
    );
    expect(screen.getByRole('link', { name: /home/i })).toHaveAttribute('href', '/');
  });
});
