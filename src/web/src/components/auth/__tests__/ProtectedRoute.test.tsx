/**
 * ProtectedRoute tests
 *
 * Catches:
 *  - Loading spinner renders while auth state is loading.
 *  - Unauthenticated users are redirected to /login (or custom redirect).
 *  - Authenticated users see the protected children.
 */
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { ProtectedRoute } from '../ProtectedRoute';

vi.mock('../../../hooks/useAuth', () => ({
  useAuth: vi.fn(),
}));
import { useAuth } from '../../../hooks/useAuth';

describe('ProtectedRoute', () => {
  it('shows the loading state while auth is loading', () => {
    (useAuth as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      isAuthenticated: false,
      isLoading: true,
    });

    render(
      <MemoryRouter>
        <ProtectedRoute>
          <div>secret</div>
        </ProtectedRoute>
      </MemoryRouter>,
    );

    expect(screen.getByText(/loading/i)).toBeInTheDocument();
    expect(screen.queryByText('secret')).toBeNull();
  });

  it('redirects to /login when not authenticated', () => {
    (useAuth as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      isAuthenticated: false,
      isLoading: false,
    });

    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Routes>
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <div>secret</div>
              </ProtectedRoute>
            }
          />
          <Route path="/login" element={<div>login page</div>} />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText('login page')).toBeInTheDocument();
    expect(screen.queryByText('secret')).toBeNull();
  });

  it('redirects to a custom redirectTo when provided', () => {
    (useAuth as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      isAuthenticated: false,
      isLoading: false,
    });

    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Routes>
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute redirectTo="/signin">
                <div>secret</div>
              </ProtectedRoute>
            }
          />
          <Route path="/signin" element={<div>signin page</div>} />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText('signin page')).toBeInTheDocument();
  });

  it('renders children when authenticated', () => {
    (useAuth as unknown as ReturnType<typeof vi.fn>).mockReturnValue({
      isAuthenticated: true,
      isLoading: false,
    });

    render(
      <MemoryRouter>
        <ProtectedRoute>
          <div>secret</div>
        </ProtectedRoute>
      </MemoryRouter>,
    );

    expect(screen.getByText('secret')).toBeInTheDocument();
  });
});
