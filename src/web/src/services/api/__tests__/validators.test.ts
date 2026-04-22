/**
 * Tests for services/api/validators
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { z } from 'zod';
import {
  parseWithSchema,
  isProblemDetails,
  parseErrorResponse,
  safeJsonParse,
  TokenResponseSchema,
  RefreshResponseSchema,
  PaginationMetaSchema,
  ApiErrorSchema,
  ProblemDetailsSchema,
  ApiResponseSchema,
  PagedResultSchema,
} from '../validators';

describe('parseWithSchema', () => {
  const schema = z.object({ name: z.string(), age: z.number() });

  it('returns parsed data on success', () => {
    expect(parseWithSchema(schema, { name: 'a', age: 2 })).toEqual({
      name: 'a',
      age: 2,
    });
  });

  it('throws a descriptive Error with field paths on ZodError', () => {
    const errSpy = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    try {
      expect(() =>
        parseWithSchema(schema, { name: 'x', age: 'no' }, 'user')
      ).toThrow(/Invalid API response format \(user\).*age/);
    } finally {
      errSpy.mockRestore();
    }
  });

  it('rethrows non-Zod errors unchanged', () => {
    const weirdSchema = {
      parse() {
        throw new RangeError('weird');
      },
    } as unknown as z.ZodType<unknown>;
    expect(() => parseWithSchema(weirdSchema, {})).toThrow(RangeError);
  });
});

describe('isProblemDetails', () => {
  it('returns true when all core fields exist', () => {
    expect(
      isProblemDetails({ type: 't', title: 'T', status: 400, detail: 'd' })
    ).toBe(true);
  });

  it('returns false when any core field is missing', () => {
    expect(isProblemDetails({ type: 't', title: 'T' })).toBe(false);
    expect(isProblemDetails({})).toBe(false);
  });

  it('returns false for non-objects', () => {
    expect(isProblemDetails(null)).toBe(false);
    expect(isProblemDetails('not a problem')).toBe(false);
    expect(isProblemDetails(undefined)).toBe(false);
    expect(isProblemDetails(42)).toBe(false);
  });
});

describe('parseErrorResponse', () => {
  it('parses ProblemDetails when "type" is present', () => {
    const parsed = parseErrorResponse({
      type: 'https://example.com/err',
      title: 'Err',
      status: 400,
      detail: 'd',
    });
    expect(parsed.format).toBe('problemDetails');
    if (parsed.format === 'problemDetails') {
      expect(parsed.error.title).toBe('Err');
    }
  });

  it('parses ProblemDetails when "title" + "detail" present', () => {
    const parsed = parseErrorResponse({
      title: 'Whoops',
      detail: 'something',
    });
    expect(parsed.format).toBe('problemDetails');
  });

  it('parses legacy ApiError shape with error.code/message', () => {
    const parsed = parseErrorResponse({
      error: { code: 'NOT_FOUND', message: 'not found' },
    });
    expect(parsed.format).toBe('legacy');
    if (parsed.format === 'legacy') {
      expect(parsed.error.code).toBe('NOT_FOUND');
      expect(parsed.error.message).toBe('not found');
    }
  });

  it('handles loose format with top-level error string', () => {
    const parsed = parseErrorResponse({
      error: 'Email is taken',
      details: { email: ['Email is taken'] },
    });
    expect(parsed.format).toBe('legacy');
    if (parsed.format === 'legacy') {
      expect(parsed.error.code).toBe('VALIDATION_ERROR');
      expect(parsed.error.message).toBe('Email is taken');
      expect(parsed.error.details?.email).toEqual(['Email is taken']);
    }
  });

  it('handles loose format with top-level message string', () => {
    const parsed = parseErrorResponse({ message: 'oops' });
    expect(parsed.format).toBe('legacy');
    if (parsed.format === 'legacy') {
      expect(parsed.error.code).toBe('ERROR');
      expect(parsed.error.message).toBe('oops');
    }
  });

  it('returns an UNKNOWN_ERROR for unrecognized shapes', () => {
    const parsed = parseErrorResponse(42);
    expect(parsed.format).toBe('legacy');
    if (parsed.format === 'legacy') {
      expect(parsed.error.code).toBe('UNKNOWN_ERROR');
    }

    const parsed2 = parseErrorResponse(null);
    expect(parsed2.format).toBe('legacy');
  });
});

describe('safeJsonParse', () => {
  let errSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    errSpy = vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  afterEach(() => {
    errSpy.mockRestore();
  });

  it('parses valid JSON', () => {
    expect(safeJsonParse('{"a":1}')).toEqual({ a: 1 });
  });

  it('returns null and logs on invalid JSON', () => {
    const out = safeJsonParse('not valid');
    expect(out).toBeNull();
    expect(errSpy).toHaveBeenCalled();
  });

  it('returns null on empty string', () => {
    expect(safeJsonParse('')).toBeNull();
  });
});

describe('exported schemas', () => {
  it('TokenResponseSchema validates user shape', () => {
    const ok = TokenResponseSchema.safeParse({
      accessToken: 'a',
      refreshToken: 'r',
      expiresAt: '2024-01-01T00:00:00Z',
      user: {
        idKey: 'k',
        firstName: 'A',
        lastName: 'B',
        email: null,
        photoUrl: null,
        roles: ['admin'],
      },
    });
    expect(ok.success).toBe(true);
  });

  it('RefreshResponseSchema requires access/refresh/expiresAt', () => {
    const bad = RefreshResponseSchema.safeParse({
      accessToken: 'a',
      refreshToken: 'r',
    });
    expect(bad.success).toBe(false);
  });

  it('PaginationMetaSchema requires numeric fields', () => {
    const ok = PaginationMetaSchema.safeParse({
      page: 1,
      pageSize: 10,
      totalCount: 100,
      totalPages: 10,
    });
    expect(ok.success).toBe(true);
  });

  it('ApiErrorSchema accepts optional details/traceId', () => {
    const ok = ApiErrorSchema.safeParse({
      error: {
        code: 'C',
        message: 'M',
        details: { field: ['err'] },
        traceId: 't',
      },
    });
    expect(ok.success).toBe(true);
  });

  it('ProblemDetailsSchema allows all fields to be optional', () => {
    expect(ProblemDetailsSchema.safeParse({}).success).toBe(true);
  });

  it('ApiResponseSchema wraps a data schema', () => {
    const schema = ApiResponseSchema(z.object({ id: z.number() }));
    expect(schema.safeParse({ data: { id: 1 } }).success).toBe(true);
    expect(schema.safeParse({ data: { id: 'x' } }).success).toBe(false);
  });

  it('PagedResultSchema wraps an item schema and requires meta', () => {
    const schema = PagedResultSchema(z.string());
    const ok = schema.safeParse({
      data: ['a', 'b'],
      meta: { page: 1, pageSize: 10, totalCount: 2, totalPages: 1 },
    });
    expect(ok.success).toBe(true);
    const missingMeta = schema.safeParse({ data: ['a'] });
    expect(missingMeta.success).toBe(false);
  });
});
