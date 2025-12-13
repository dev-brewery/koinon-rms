/**
 * Error Message Utilities Tests
 */

import { describe, it, expect } from 'vitest';
import { getErrorMessage } from '../errorMessages';
import { ApiClientError } from '../../services/api/client';

describe('getErrorMessage', () => {
  it('should map 400 Bad Request to validation error', () => {
    const error = new ApiClientError(400, {
      code: 'VALIDATION_ERROR',
      message: 'Validation failed',
    });

    const result = getErrorMessage(error);

    expect(result.title).toBe('Invalid Request');
    expect(result.message).toBe('Validation failed');
    expect(result.variant).toBe('error');
  });

  it('should map 400 with validation details to specific field error', () => {
    const error = new ApiClientError(400, {
      code: 'VALIDATION_ERROR',
      message: 'Validation failed',
      details: {
        email: ['Email is required'],
        password: ['Password must be at least 8 characters'],
      },
    });

    const result = getErrorMessage(error);

    expect(result.title).toBe('Validation Error');
    expect(result.message).toBe('Email is required');
    expect(result.variant).toBe('error');
  });

  it('should map 401 Unauthorized to authentication error', () => {
    const error = new ApiClientError(401, {
      code: 'UNAUTHORIZED',
      message: 'Invalid credentials',
    });

    const result = getErrorMessage(error);

    expect(result.title).toBe('Authentication Required');
    expect(result.message).toBe('Your session has expired. Please sign in again.');
    expect(result.variant).toBe('warning');
  });

  it('should map 403 Forbidden to access denied error', () => {
    const error = new ApiClientError(403, {
      code: 'FORBIDDEN',
      message: 'Access denied',
    });

    const result = getErrorMessage(error);

    expect(result.title).toBe('Access Denied');
    expect(result.message).toBe('You do not have permission to perform this action.');
    expect(result.variant).toBe('error');
  });

  it('should map 404 Not Found to resource not found error', () => {
    const error = new ApiClientError(404, {
      code: 'NOT_FOUND',
      message: 'Resource not found',
    });

    const result = getErrorMessage(error);

    expect(result.title).toBe('Not Found');
    expect(result.message).toBe('The requested resource was not found.');
    expect(result.variant).toBe('error');
  });

  it('should map 408 Request Timeout to timeout error', () => {
    const error = new ApiClientError(408, {
      code: 'REQUEST_TIMEOUT',
      message: 'Request timed out',
    });

    const result = getErrorMessage(error);

    expect(result.title).toBe('Request Timeout');
    expect(result.message).toBe('The request took too long. Please try again.');
    expect(result.variant).toBe('error');
  });

  it('should map 409 Conflict to conflict error', () => {
    const error = new ApiClientError(409, {
      code: 'CONFLICT',
      message: 'Email already exists',
    });

    const result = getErrorMessage(error);

    expect(result.title).toBe('Conflict');
    expect(result.message).toBe('Email already exists');
    expect(result.variant).toBe('error');
  });

  it('should map 429 Too Many Requests to rate limit error', () => {
    const error = new ApiClientError(429, {
      code: 'TOO_MANY_REQUESTS',
      message: 'Rate limit exceeded',
    });

    const result = getErrorMessage(error);

    expect(result.title).toBe('Too Many Requests');
    expect(result.message).toBe('You are making requests too quickly. Please wait a moment and try again.');
    expect(result.variant).toBe('warning');
  });

  it('should map 500+ Server Errors to server error', () => {
    const error = new ApiClientError(500, {
      code: 'INTERNAL_SERVER_ERROR',
      message: 'Internal server error',
    });

    const result = getErrorMessage(error);

    expect(result.title).toBe('Server Error');
    expect(result.message).toBe('A server error occurred. Please try again later.');
    expect(result.variant).toBe('error');
  });

  it('should handle network errors (TypeError)', () => {
    const error = new TypeError('Failed to fetch');

    const result = getErrorMessage(error);

    expect(result.title).toBe('Network Error');
    expect(result.message).toBe('Network connection lost. Please check your internet connection and try again.');
    expect(result.variant).toBe('error');
  });

  it('should handle generic Error instances', () => {
    const error = new Error('Something went wrong');

    const result = getErrorMessage(error);

    expect(result.title).toBe('Error');
    expect(result.message).toBe('Something went wrong. Please try again.');
    expect(result.variant).toBe('error');
  });

  it('should handle unknown error types', () => {
    const error = { unknown: 'error' };

    const result = getErrorMessage(error);

    expect(result.title).toBe('Error');
    expect(result.message).toBe('An unexpected error occurred. Please try again.');
    expect(result.variant).toBe('error');
  });

  it('should handle network error with "network" in message', () => {
    const error = new TypeError('network error occurred');

    const result = getErrorMessage(error);

    expect(result.title).toBe('Network Error');
    expect(result.message).toContain('Network connection lost');
    expect(result.variant).toBe('error');
  });

  it('should fallback for TypeError without network keywords', () => {
    const error = new TypeError('Some other type error');

    const result = getErrorMessage(error);

    expect(result.title).toBe('Error');
    expect(result.message).toBe('Something went wrong. Please try again.');
    expect(result.variant).toBe('error');
  });
});
