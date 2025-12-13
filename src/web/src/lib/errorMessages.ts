/**
 * Error Message Utilities
 * Maps HTTP status codes and error types to user-friendly messages
 */

import { ApiClientError } from '../services/api/client';
import { isNetworkError } from './networkUtils';

export interface UserFriendlyError {
  title: string;
  message: string;
  variant: 'error' | 'warning' | 'info';
}

/**
 * Map HTTP status code to user-friendly error message
 */
export function getErrorMessage(error: unknown): UserFriendlyError {
  // Handle ApiClientError (from our API)
  if (error instanceof ApiClientError) {
    return getApiErrorMessage(error);
  }

  // Handle network errors (TypeError from fetch)
  if (isNetworkError(error)) {
    return {
      title: 'Network Error',
      message: 'Network connection lost. Please check your internet connection and try again.',
      variant: 'error',
    };
  }

  // Handle generic Error instances
  if (error instanceof Error) {
    return {
      title: 'Error',
      message: 'Something went wrong. Please try again.',
      variant: 'error',
    };
  }

  // Unknown error type
  return {
    title: 'Error',
    message: 'An unexpected error occurred. Please try again.',
    variant: 'error',
  };
}

/**
 * Map ApiClientError to user-friendly message
 */
function getApiErrorMessage(error: ApiClientError): UserFriendlyError {
  const { statusCode, error: apiError } = error;

  // 400 - Bad Request (validation errors)
  if (statusCode === 400) {
    // If we have validation details, show them
    if (apiError.details && Object.keys(apiError.details).length > 0) {
      const firstField = Object.keys(apiError.details)[0];
      const firstError = apiError.details[firstField]?.[0];
      return {
        title: 'Validation Error',
        message: firstError || apiError.message || 'Please check your input and try again.',
        variant: 'error',
      };
    }

    return {
      title: 'Invalid Request',
      message: apiError.message || 'Please check your input and try again.',
      variant: 'error',
    };
  }

  // 401 - Unauthorized
  if (statusCode === 401) {
    return {
      title: 'Authentication Required',
      message: 'Your session has expired. Please sign in again.',
      variant: 'warning',
    };
  }

  // 403 - Forbidden
  if (statusCode === 403) {
    return {
      title: 'Access Denied',
      message: 'You do not have permission to perform this action.',
      variant: 'error',
    };
  }

  // 404 - Not Found
  if (statusCode === 404) {
    return {
      title: 'Not Found',
      message: 'The requested resource was not found.',
      variant: 'error',
    };
  }

  // 408 - Request Timeout
  if (statusCode === 408) {
    return {
      title: 'Request Timeout',
      message: 'The request took too long. Please try again.',
      variant: 'error',
    };
  }

  // 409 - Conflict
  if (statusCode === 409) {
    return {
      title: 'Conflict',
      message: apiError.message || 'This operation conflicts with existing data.',
      variant: 'error',
    };
  }

  // 429 - Too Many Requests
  if (statusCode === 429) {
    return {
      title: 'Too Many Requests',
      message: 'You are making requests too quickly. Please wait a moment and try again.',
      variant: 'warning',
    };
  }

  // 500+ - Server Errors
  if (statusCode >= 500) {
    return {
      title: 'Server Error',
      message: 'A server error occurred. Please try again later.',
      variant: 'error',
    };
  }

  // Default error message
  return {
    title: 'Error',
    message: apiError.message || 'Something went wrong. Please try again.',
    variant: 'error',
  };
}

/**
 * Log error for debugging (only in development, with sanitization)
 */
export function logError(error: unknown, context?: string): void {
  // Only log in development mode
  if (!import.meta.env.DEV) {
    return;
  }

  const prefix = context ? `[${context}]` : '[Error]';

  if (error instanceof ApiClientError) {
    // Don't log traceId or details (may contain sensitive data)
    console.error(prefix, 'API Error:', {
      statusCode: error.statusCode,
      code: error.error.code,
      message: error.error.message,
    });
  } else if (error instanceof Error) {
    console.error(prefix, error.name, error.message, {
      stack: error.stack,
    });
  } else {
    console.error(prefix, 'Unknown error:', error);
  }
}
