/**
 * Additional errorMessages tests covering ProblemDetails branches and logError.
 *
 * The original errorMessages.test.ts focuses on legacy API errors; this file
 * drives uncovered lines (ProblemDetails format branches, validation detail
 * extraction, default catch-all, and logError).
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { getErrorMessage, logError } from '../errorMessages';
import { ApiClientError } from '../../services/api/client';

describe('getErrorMessage - ProblemDetails format', () => {
  it('uses detail from ProblemDetails for non-specific status codes', () => {
    const err = new ApiClientError(
      418,
      {
        type: 'https://example.com/teapot',
        title: 'Teapot',
        status: 418,
        detail: 'I am a teapot',
      },
      'problemDetails'
    );

    const result = getErrorMessage(err);
    // Not in explicit list; falls through to default
    expect(result.message).toBe('I am a teapot');
    expect(result.variant).toBe('error');
  });

  it('pulls first field error from ProblemDetails extensions.errors for 400', () => {
    const err = new ApiClientError(
      400,
      {
        type: 'https://example.com/validation',
        title: 'Validation failed',
        status: 400,
        detail: 'One or more fields are invalid',
        extensions: {
          errors: {
            email: ['Email is required'],
            name: ['Name too short'],
          },
        },
      },
      'problemDetails'
    );

    const result = getErrorMessage(err);
    expect(result.title).toBe('Validation Error');
    expect(result.message).toBe('Email is required');
  });

  it('uses ProblemDetails detail for plain 400 with no extensions.errors', () => {
    const err = new ApiClientError(
      400,
      {
        type: 'https://example.com/bad-request',
        title: 'Bad Request',
        status: 400,
        detail: 'The request body was invalid',
      },
      'problemDetails'
    );

    const result = getErrorMessage(err);
    expect(result.title).toBe('Invalid Request');
    expect(result.message).toBe('The request body was invalid');
  });

  it('uses ProblemDetails detail for 409 conflict', () => {
    const err = new ApiClientError(
      409,
      {
        type: 'https://example.com/conflict',
        title: 'Conflict',
        status: 409,
        detail: 'Slug already exists',
      },
      'problemDetails'
    );

    const result = getErrorMessage(err);
    expect(result.title).toBe('Conflict');
    expect(result.message).toBe('Slug already exists');
  });

  it('falls back to generic conflict message when ProblemDetails detail is empty', () => {
    const err = new ApiClientError(
      409,
      {
        type: 'https://example.com/conflict',
        title: 'Conflict',
        status: 409,
      },
      'problemDetails',
      'conflict occurred'
    );

    const result = getErrorMessage(err);
    expect(result.title).toBe('Conflict');
    // message is the constructor message; still non-empty
    expect(result.message).toBeTruthy();
  });

  it('falls back to default legacy message for unknown legacy code', () => {
    const err = new ApiClientError(418, {
      code: 'TEAPOT',
      message: 'Still a teapot',
    });
    const result = getErrorMessage(err);
    expect(result.title).toBe('Error');
    expect(result.message).toBe('Still a teapot');
  });
});

describe('logError', () => {
  let devSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    // Make DEV true so logError actually runs
    vi.stubEnv('DEV', true);
    devSpy = vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  afterEach(() => {
    devSpy.mockRestore();
    vi.unstubAllEnvs();
  });

  it('is a no-op in production (DEV=false)', () => {
    vi.stubEnv('DEV', false);
    logError(new Error('prod error'), 'ctx');
    expect(devSpy).not.toHaveBeenCalled();
  });

  it('logs legacy ApiClientError with code/message', () => {
    const err = new ApiClientError(500, {
      code: 'SERVER_ERROR',
      message: 'boom',
      traceId: 'trace-123',
    });
    logError(err, 'requestX');
    expect(devSpy).toHaveBeenCalledTimes(1);
    const [prefix, , payload] = devSpy.mock.calls[0];
    expect(prefix).toBe('[requestX]');
    expect(payload).toMatchObject({
      statusCode: 500,
      format: 'legacy',
      code: 'SERVER_ERROR',
      message: 'boom',
      traceId: 'trace-123',
    });
  });

  it('logs ProblemDetails ApiClientError with detail fields', () => {
    const err = new ApiClientError(
      400,
      {
        type: 'https://example.com/probs',
        title: 'Validation',
        status: 400,
        detail: 'bad input',
        instance: '/api/things/1',
        traceId: 'tid',
      },
      'problemDetails'
    );

    logError(err);
    expect(devSpy).toHaveBeenCalledTimes(1);
    const [prefix, , payload] = devSpy.mock.calls[0];
    expect(prefix).toBe('[Error]');
    expect(payload).toMatchObject({
      statusCode: 400,
      format: 'problemDetails',
      title: 'Validation',
      status: 400,
      detail: 'bad input',
      instance: '/api/things/1',
      traceId: 'tid',
    });
  });

  it('logs plain Error with name, message, and stack', () => {
    const err = new Error('plain');
    logError(err);
    expect(devSpy).toHaveBeenCalledTimes(1);
    const [prefix, name, message] = devSpy.mock.calls[0];
    expect(prefix).toBe('[Error]');
    expect(name).toBe('Error');
    expect(message).toBe('plain');
  });

  it('logs unknown error shapes', () => {
    logError({ weird: 'thing' });
    expect(devSpy).toHaveBeenCalledTimes(1);
    const [prefix, label] = devSpy.mock.calls[0];
    expect(prefix).toBe('[Error]');
    expect(label).toBe('Unknown error:');
  });
});
