/**
 * useErrorHandler tests - verifies dispatch to the correct toast variant
 * based on getErrorMessage's classification.
 */

import { describe, it, expect, vi } from 'vitest';
import { renderHook } from '@testing-library/react';
import React from 'react';

const toastMocks = {
  success: vi.fn(),
  error: vi.fn(),
  warning: vi.fn(),
  info: vi.fn(),
};

vi.mock('../../contexts/ToastContext', () => ({
  useToast: () => toastMocks,
  ToastProvider: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}));

import { useErrorHandler } from '../useErrorHandler';
import { ApiClientError } from '../../services/api/client';

describe('useErrorHandler', () => {
  it('dispatches an error toast for 500 server errors', () => {
    toastMocks.error.mockReset();
    const { result } = renderHook(() => useErrorHandler());
    result.current.handleError(
      new ApiClientError(500, { code: 'E', message: 'boom' })
    );
    expect(toastMocks.error).toHaveBeenCalledWith(
      'Server Error',
      expect.stringContaining('server error')
    );
  });

  it('dispatches a warning toast for 401 auth errors', () => {
    toastMocks.warning.mockReset();
    const { result } = renderHook(() => useErrorHandler());
    result.current.handleError(
      new ApiClientError(401, { code: 'E', message: 'nope' })
    );
    expect(toastMocks.warning).toHaveBeenCalled();
  });

  it('returns the user-friendly error so callers can inline-display it', () => {
    toastMocks.error.mockReset();
    const { result } = renderHook(() => useErrorHandler());
    const out = result.current.handleError(new Error('oops'), 'CreatePerson');
    expect(out.title).toBe('Error');
    expect(out.message).toBeDefined();
  });
});
