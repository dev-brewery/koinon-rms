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
  const { statusCode } = error;

  // Extract error details based on format
  let message: string;
  let details: Record<string, string[]> | undefined;

  if (error.format === 'problemDetails' && error.problemDetails) {
    message = error.problemDetails.detail || error.message;
    // Extract validation errors from extensions if present
    if (error.problemDetails.extensions?.errors) {
      details = error.problemDetails.extensions.errors as Record<string, string[]>;
    }
  } else {
    // Legacy format
    const apiError = error.error;
    message = apiError.message;
    details = apiError.details;
  }

  // 400 - Bad Request (validation errors)
  if (statusCode === 400) {
    // If we have validation details, show them
    if (details && Object.keys(details).length > 0) {
      const firstField = Object.keys(details)[0];
      const firstError = details[firstField]?.[0];
      return {
        title: 'Validation Error',
        message: firstError || message || 'Please check your input and try again.',
        variant: 'error',
      };
    }

    return {
      title: 'Invalid Request',
      message: message || 'Please check your input and try again.',
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
      message: message || 'This operation conflicts with existing data.',
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
    message: message || 'Something went wrong. Please try again.',
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
    const logData: Record<string, unknown> = {
      statusCode: error.statusCode,
      format: error.format,
    };

    if (error.format === 'problemDetails' && error.problemDetails) {
      if (error.problemDetails.type) logData.type = error.problemDetails.type;
      if (error.problemDetails.title) logData.title = error.problemDetails.title;
      if (error.problemDetails.status) logData.status = error.problemDetails.status;
      if (error.problemDetails.detail) logData.detail = error.problemDetails.detail;
      if (error.problemDetails.instance) logData.instance = error.problemDetails.instance;
      if (error.traceId) logData.traceId = error.traceId;
    } else {
      logData.code = error.error.code;
      logData.message = error.error.message;
      if (error.traceId) {
        logData.traceId = error.traceId;
      }
    }

    console.error(prefix, 'API Error:', logData);
  } else if (error instanceof Error) {
    console.error(prefix, error.name, error.message, {
      stack: error.stack,
    });
  } else {
    console.error(prefix, 'Unknown error:', error);
  }
}
