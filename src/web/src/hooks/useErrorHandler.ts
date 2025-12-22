/**
 * Error Handler Hook
 * Provides centralized error handling with toast notifications
 */

import { useCallback } from 'react';
import { useToast } from '../contexts/ToastContext';
import { getErrorMessage, logError } from '../lib/errorMessages';

export function useErrorHandler() {
  const toast = useToast();

  /**
   * Handle an error by logging it and showing a toast notification
   * @param error - The error to handle
   * @param context - Optional context string for logging (e.g., 'Login', 'Create Person')
   * @returns The user-friendly error message
   */
  const handleError = useCallback(
    (error: unknown, context?: string) => {
      // Log error for debugging (includes traceId in dev mode)
      logError(error, context);

      // Get user-friendly message (extracts detail field from ProblemDetails)
      const userError = getErrorMessage(error);

      // Show toast notification
      if (userError.variant === 'error') {
        toast.error(userError.title, userError.message);
      } else if (userError.variant === 'warning') {
        toast.warning(userError.title, userError.message);
      } else if (userError.variant === 'info') {
        toast.info(userError.title, userError.message);
      } else {
        // Exhaustive check - should never reach here
        void (userError.variant satisfies never);
        toast.error('Error', 'An unexpected error occurred');
      }

      return userError;
    },
    [toast]
  );

  return { handleError };
}
