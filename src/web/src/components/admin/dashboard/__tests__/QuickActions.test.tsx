/**
 * QuickActions tests
 *
 * Catches:
 *  - Renders the expected admin quick-action links with correct hrefs
 *    (regressions would break primary dashboard navigation).
 *  - Each tile exposes both label and description text.
 */
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { QuickActions } from '../QuickActions';

describe('QuickActions', () => {
  function renderQA() {
    return render(
      <MemoryRouter>
        <QuickActions />
      </MemoryRouter>,
    );
  }

  it('renders the four primary quick actions', () => {
    renderQA();
    expect(screen.getByRole('link', { name: /add person/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /create family/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /group directory/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /upcoming events/i })).toBeInTheDocument();
  });

  it('wires Add Person to /admin/people/new', () => {
    renderQA();
    expect(screen.getByRole('link', { name: /add person/i })).toHaveAttribute(
      'href',
      '/admin/people/new',
    );
  });

  it('wires Create Family to /admin/families/new', () => {
    renderQA();
    expect(screen.getByRole('link', { name: /create family/i })).toHaveAttribute(
      'href',
      '/admin/families/new',
    );
  });

  it('wires Group Directory to /admin/groups', () => {
    renderQA();
    expect(screen.getByRole('link', { name: /group directory/i })).toHaveAttribute(
      'href',
      '/admin/groups',
    );
  });

  it('renders descriptions for each action', () => {
    renderQA();
    expect(screen.getByText(/create a new person record/i)).toBeInTheDocument();
    expect(screen.getByText(/add a new family household/i)).toBeInTheDocument();
  });
});
