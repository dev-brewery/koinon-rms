/**
 * LoginForm tests
 *
 * Catches:
 *  - Email + password form submits via the login() function.
 *  - 400/401 error from API shows "Invalid email or password" inline (no toast).
 *  - Other errors flow through handleError and surface its user-facing message.
 *  - Loading button text while request in flight.
 */
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

// Hoisted mocks. The factories cannot reference outer variables, so we define
// the error class inside the factory and pull it out later via dynamic import.
vi.mock('../../../services/api', () => {
  class ApiClientError extends Error {
    statusCode: number;
    constructor(statusCode: number, message: string) {
      super(message);
      this.statusCode = statusCode;
      this.name = 'ApiClientError';
    }
  }
  return { ApiClientError };
});

const loginMock = vi.fn();
const handleErrorMock = vi.fn();

vi.mock('../../../hooks/useAuth', () => ({
  useAuth: () => ({ login: loginMock }),
}));

vi.mock('../../../hooks/useErrorHandler', () => ({
  useErrorHandler: () => ({ handleError: handleErrorMock }),
}));

import { LoginForm } from '../LoginForm';
import { ApiClientError } from '../../../services/api';

describe('LoginForm', () => {
  beforeEach(() => {
    loginMock.mockReset();
    handleErrorMock.mockReset();
  });

  it('calls login with the entered credentials on submit', async () => {
    const user = userEvent.setup();
    loginMock.mockResolvedValue(undefined);

    render(<LoginForm />);
    await user.type(screen.getByLabelText(/email/i), 'alice@example.com');
    await user.type(screen.getByLabelText(/password/i), 'secret');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(loginMock).toHaveBeenCalledWith({
      email: 'alice@example.com',
      password: 'secret',
    });
  });

  it('shows "Invalid email or password" inline for a 401 error', async () => {
    const user = userEvent.setup();
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    loginMock.mockRejectedValue(new (ApiClientError as any)(401, 'Unauthorized'));

    render(<LoginForm />);
    await user.type(screen.getByLabelText(/email/i), 'a@b.c');
    await user.type(screen.getByLabelText(/password/i), 'bad');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() =>
      expect(screen.getByRole('alert')).toHaveTextContent(
        /invalid email or password/i,
      ),
    );
    expect(handleErrorMock).not.toHaveBeenCalled();
  });

  it('shows inline error for 400 errors as well', async () => {
    const user = userEvent.setup();
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    loginMock.mockRejectedValue(new (ApiClientError as any)(400, 'Bad Request'));

    render(<LoginForm />);
    await user.type(screen.getByLabelText(/email/i), 'a@b.c');
    await user.type(screen.getByLabelText(/password/i), 'bad');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() =>
      expect(screen.getByRole('alert')).toHaveTextContent(
        /invalid email or password/i,
      ),
    );
  });

  it('delegates other errors to handleError and surfaces its message', async () => {
    const user = userEvent.setup();
    loginMock.mockRejectedValue(new Error('network down'));
    handleErrorMock.mockReturnValue({ message: 'Please try again later.' });

    render(<LoginForm />);
    await user.type(screen.getByLabelText(/email/i), 'a@b.c');
    await user.type(screen.getByLabelText(/password/i), 'pw');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() =>
      expect(screen.getByRole('alert')).toHaveTextContent(/please try again later/i),
    );
    expect(handleErrorMock).toHaveBeenCalled();
  });

  it('shows loading label while request is in flight', async () => {
    const user = userEvent.setup();
    let resolveLogin: () => void = () => {};
    loginMock.mockImplementation(
      () => new Promise<void>((r) => (resolveLogin = r)),
    );

    render(<LoginForm />);
    await user.type(screen.getByLabelText(/email/i), 'a@b.c');
    await user.type(screen.getByLabelText(/password/i), 'pw');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(screen.getByRole('button', { name: /signing in/i })).toBeDisabled();

    resolveLogin();
    await waitFor(() =>
      expect(screen.getByRole('button', { name: /sign in/i })).not.toBeDisabled(),
    );
  });
});
