/**
 * React Query mutation hook with automatic toast notifications
 */

import { useMutation, type UseMutationOptions } from '@tanstack/react-query';
import { useToast } from '@/contexts/ToastContext';
import { ApiClientError } from '@/services/api/client';

// ============================================================================
// Types
// ============================================================================

export interface MutationWithToastOptions<TData, TError, TVariables, TContext = unknown>
  extends Omit<
    UseMutationOptions<TData, TError, TVariables, TContext>,
    'onSuccess' | 'onError'
  > {
  successMessage?: string | ((data: TData) => string);
  errorMessage?: string | ((error: TError) => string);
  onSuccess?: (data: TData, variables: TVariables, context: TContext | undefined) => void;
  onError?: (error: TError, variables: TVariables, context: TContext | undefined) => void;
}

// ============================================================================
// Hook
// ============================================================================

/**
 * Wraps TanStack Query's useMutation to automatically show toast notifications
 *
 * @example
 * const createPersonMutation = useMutationWithToast({
 *   mutationFn: (data) => peopleApi.createPerson(data),
 *   successMessage: (person) => `Created ${person.firstName} ${person.lastName}`,
 *   errorMessage: 'Failed to create person',
 * });
 */
export function useMutationWithToast<TData, TError = Error, TVariables = void, TContext = unknown>(
  options: MutationWithToastOptions<TData, TError, TVariables, TContext>
) {
  const { success, error } = useToast();

  const {
    successMessage,
    errorMessage,
    onSuccess: customOnSuccess,
    onError: customOnError,
    ...mutationOptions
  } = options;

  return useMutation({
    ...mutationOptions,
    onSuccess: (data, variables, context) => {
      // Show success toast
      if (successMessage) {
        const message = typeof successMessage === 'function'
          ? successMessage(data)
          : successMessage;
        success('Success', message);
      }

      // Call custom onSuccess callback
      customOnSuccess?.(data, variables, context);
    },
    onError: (err, variables, context) => {
      // Show error toast - never expose raw error messages to users
      if (errorMessage) {
        const message = typeof errorMessage === 'function'
          ? errorMessage(err)
          : errorMessage;
        error('Error', message);
      } else {
        // Default user-friendly error message
        error('Error', 'An error occurred. Please try again.');
      }

      // Log full error details to console for debugging
      // Include traceId if available from ProblemDetails
      if (err instanceof ApiClientError && err.traceId) {
        console.error('Mutation error:', {
          error: err,
          traceId: err.traceId,
          format: err.format,
        });
      } else {
        console.error('Mutation error:', err);
      }

      // Call custom onError callback
      customOnError?.(err, variables, context);
    },
  });
}
