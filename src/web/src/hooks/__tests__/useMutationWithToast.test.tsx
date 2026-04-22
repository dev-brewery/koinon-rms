/**
 * useMutationWithToast — wraps TanStack `useMutation` to surface toast notifications.
 *
 * Behaviors worth locking in:
 *   - successMessage may be a string OR a function of data.
 *   - errorMessage may be a string OR a function of the error.
 *   - No successMessage => no success toast; no errorMessage => default
 *     user-friendly error toast (never raw error.message).
 *   - custom onSuccess / onError callbacks are forwarded.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { act } from '@testing-library/react';

const successToast = vi.fn();
const errorToast = vi.fn();

vi.mock('@/contexts/ToastContext', () => ({
  useToast: () => ({
    success: successToast,
    error: errorToast,
    warning: vi.fn(),
    info: vi.fn(),
    addToast: vi.fn(),
    removeToast: vi.fn(),
  }),
}));

import { useMutationWithToast } from '../useMutationWithToast';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => {
  successToast.mockReset();
  errorToast.mockReset();
  // Silence console.error noise from error-path tests.
  vi.spyOn(console, 'error').mockImplementation(() => {});
});

describe('useMutationWithToast — success path', () => {
  it('static successMessage surfaces a success toast', async () => {
    const mutationFn = vi.fn().mockResolvedValue('done');
    const { result } = renderHookWithQuery(() =>
      useMutationWithToast({ mutationFn, successMessage: 'It worked' })
    );
    await act(async () => {
      await result.current.mutateAsync(undefined as never);
    });
    expect(successToast).toHaveBeenCalledWith('Success', 'It worked');
  });

  it('function successMessage receives the data and renders result', async () => {
    const mutationFn = vi.fn().mockResolvedValue({ name: 'Jane' });
    const { result } = renderHookWithQuery(() =>
      useMutationWithToast({
        mutationFn,
        successMessage: (d) => `Hi ${(d as { name: string }).name}`,
      })
    );
    await act(async () => {
      await result.current.mutateAsync(undefined as never);
    });
    expect(successToast).toHaveBeenCalledWith('Success', 'Hi Jane');
  });

  it('no successMessage => no success toast', async () => {
    const mutationFn = vi.fn().mockResolvedValue('done');
    const onSuccess = vi.fn();
    const { result } = renderHookWithQuery(() =>
      useMutationWithToast({ mutationFn, onSuccess })
    );
    await act(async () => {
      await result.current.mutateAsync(undefined as never);
    });
    expect(successToast).not.toHaveBeenCalled();
    // Custom callback still fires.
    expect(onSuccess).toHaveBeenCalled();
  });
});

describe('useMutationWithToast — error path', () => {
  it('static errorMessage surfaces an error toast', async () => {
    const mutationFn = vi.fn().mockRejectedValue(new Error('raw'));
    const { result } = renderHookWithQuery(() =>
      useMutationWithToast({
        mutationFn,
        errorMessage: 'User-friendly message',
      })
    );
    await act(async () => {
      try {
        await result.current.mutateAsync(undefined as never);
      } catch {
        /* expected */
      }
    });
    expect(errorToast).toHaveBeenCalledWith('Error', 'User-friendly message');
  });

  it('function errorMessage receives the error', async () => {
    const err = new Error('boom');
    const mutationFn = vi.fn().mockRejectedValue(err);
    const { result } = renderHookWithQuery(() =>
      useMutationWithToast({
        mutationFn,
        errorMessage: (e) => `Saw ${(e as Error).message}`,
      })
    );
    await act(async () => {
      try {
        await result.current.mutateAsync(undefined as never);
      } catch {
        /* expected */
      }
    });
    expect(errorToast).toHaveBeenCalledWith('Error', 'Saw boom');
  });

  it('no errorMessage => generic "please try again" toast (never raw error)', async () => {
    const mutationFn = vi.fn().mockRejectedValue(new Error('secret db error'));
    const onError = vi.fn();
    const { result } = renderHookWithQuery(() =>
      useMutationWithToast({ mutationFn, onError })
    );
    await act(async () => {
      try {
        await result.current.mutateAsync(undefined as never);
      } catch {
        /* expected */
      }
    });
    expect(errorToast).toHaveBeenCalledWith('Error', 'An error occurred. Please try again.');
    expect(onError).toHaveBeenCalled();
    // Guardrail: never leak the raw error message to the toast.
    const [, message] = errorToast.mock.calls[0];
    expect(String(message)).not.toContain('secret');
  });
});
