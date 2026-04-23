/**
 * RouteErrorBoundary tests
 *
 * Exercises every status-code branch plus the non-HTTP JS error path:
 *   - 404 renders "Page Not Found" + Dashboard / Home links.
 *   - 403 renders "Access Denied" + single Dashboard link.
 *   - 500 renders "Server Error" + Reload button.
 *   - Other HTTP statuses fall through to the generic HTTP error block.
 *   - Non-HTTP errors (thrown Error) render the JS-error branch.
 */
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';
import { RouteErrorBoundary } from '../RouteErrorBoundary';

// Build a router whose loader throws the supplied value, forcing the
// RouteErrorBoundary to render with that error.
function renderWithError(error: unknown) {
  const router = createMemoryRouter(
    [
      {
        path: '/',
        loader: () => {
          throw error;
        },
        element: <div>should not render</div>,
        errorElement: <RouteErrorBoundary />,
      },
    ],
    { initialEntries: ['/'] }
  );
  return render(<RouterProvider router={router} />);
}

describe('RouteErrorBoundary', () => {
  it('renders 404 branch for 404 route errors', async () => {
    renderWithError(new Response(null, { status: 404 }));
    expect(await screen.findByText('404')).toBeInTheDocument();
    expect(screen.getByText(/page not found/i)).toBeInTheDocument();
    // Both CTA links present.
    expect(screen.getByRole('link', { name: /dashboard/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /go home/i })).toBeInTheDocument();
  });

  it('renders 403 branch for 403 route errors', async () => {
    renderWithError(new Response(null, { status: 403 }));
    expect(await screen.findByText('403')).toBeInTheDocument();
    expect(screen.getByText(/access denied/i)).toBeInTheDocument();
  });

  it('renders 500 branch with reload button', async () => {
    renderWithError(new Response(null, { status: 500 }));
    expect(await screen.findByText('500')).toBeInTheDocument();
    expect(screen.getByText(/server error/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /reload/i })).toBeInTheDocument();
  });

  it('renders generic HTTP branch for other statuses', async () => {
    renderWithError(new Response(null, { status: 418, statusText: "I'm a teapot" }));
    expect(await screen.findByText('418')).toBeInTheDocument();
    expect(screen.getByText(/teapot/i)).toBeInTheDocument();
  });

  it('renders JS-error branch when thrown value is a plain Error', async () => {
    renderWithError(new Error('boom'));
    expect(await screen.findByText(/something went wrong/i)).toBeInTheDocument();
    // Two CTAs (reload button + dashboard link).
    expect(screen.getByRole('button', { name: /reload/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /dashboard/i })).toBeInTheDocument();
  });

  it('wraps non-Error throws in a new Error object for JS branch', async () => {
    renderWithError('string error');
    expect(await screen.findByText(/something went wrong/i)).toBeInTheDocument();
  });
});
