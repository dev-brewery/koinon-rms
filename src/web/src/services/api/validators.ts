/**
 * Zod validators for API responses
 * Provides runtime type validation for all API data
 */

import { z } from 'zod';

// ============================================================================
// Common Validators
// ============================================================================

export const ApiErrorSchema = z.object({
  error: z.object({
    code: z.string(),
    message: z.string(),
    details: z.record(z.string(), z.array(z.string())).optional(),
    traceId: z.string().optional(),
  }),
});

export const ProblemDetailsSchema = z.object({
  type: z.string().optional(),
  title: z.string().optional(),
  status: z.number().optional(),
  detail: z.string().optional(),
  instance: z.string().optional(),
  traceId: z.string().optional(),
  extensions: z.record(z.string(), z.unknown()).optional(),
});

export const PaginationMetaSchema = z.object({
  page: z.number(),
  pageSize: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
});

export const ApiResponseSchema = <T extends z.ZodType>(dataSchema: T) =>
  z.object({
    data: dataSchema,
    meta: PaginationMetaSchema.optional(),
  });

export const PagedResultSchema = <T extends z.ZodType>(itemSchema: T) =>
  z.object({
    data: z.array(itemSchema),
    meta: PaginationMetaSchema,
  });

// ============================================================================
// Auth Validators
// ============================================================================

export const UserSummarySchema = z.object({
  idKey: z.string(),
  firstName: z.string(),
  lastName: z.string(),
  email: z.string(),
  photoUrl: z.string().optional(),
});

export const TokenResponseSchema = z.object({
  accessToken: z.string(),
  refreshToken: z.string(),
  expiresAt: z.string(),
  user: UserSummarySchema,
});

export const RefreshResponseSchema = z.object({
  accessToken: z.string(),
  refreshToken: z.string(),
  expiresAt: z.string(),
});

// ============================================================================
// Helper Functions
// ============================================================================

/**
 * Safely parse JSON data with Zod schema validation
 * Throws a descriptive error if validation fails
 */
export function parseWithSchema<T>(
  schema: z.ZodType<T>,
  data: unknown,
  context?: string
): T {
  try {
    return schema.parse(data);
  } catch (error) {
    if (error instanceof z.ZodError) {
      const contextMsg = context ? ` (${context})` : '';
      const errorDetails = error.issues
        .map((err: z.ZodIssue) => `${err.path.join('.')}: ${err.message}`)
        .join(', ');

      console.error(`Validation error${contextMsg}:`, {
        errors: error.issues,
        data,
      });

      throw new Error(
        `Invalid API response format${contextMsg}: ${errorDetails}`
      );
    }
    throw error;
  }
}

/**
 * Type guard to check if an object is a ProblemDetails error
 */
export function isProblemDetails(data: unknown): data is z.infer<typeof ProblemDetailsSchema> {
  if (typeof data !== 'object' || data === null) {
    return false;
  }
  
  // Check for required ProblemDetails fields
  return (
    'type' in data &&
    'title' in data &&
    'status' in data &&
    'detail' in data
  );
}

/**
 * Discriminated union type for error response parsing
 */
export type ParsedErrorResponse =
  | { format: 'problemDetails'; error: z.infer<typeof ProblemDetailsSchema> }
  | { format: 'legacy'; error: z.infer<typeof ApiErrorSchema>['error'] };

/**
 * Parse error response supporting both ProblemDetails and legacy ApiError formats
 */
export function parseErrorResponse(data: unknown): ParsedErrorResponse {
  // Check if it's ProblemDetails format (has 'type' field)
  if (typeof data === 'object' && data !== null && 'type' in data) {
    try {
      const problemDetails = ProblemDetailsSchema.parse(data);
      return { format: 'problemDetails', error: problemDetails };
    } catch (error) {
      if (import.meta.env.DEV) {
        console.warn('Invalid ProblemDetails format:', error);
      }
    }
  }

  // Check if it's legacy ApiError format (has 'error' field)
  if (typeof data === 'object' && data !== null && 'error' in data) {
    try {
      const apiError = ApiErrorSchema.parse(data);
      return { format: 'legacy', error: apiError.error };
    } catch (error) {
      if (import.meta.env.DEV) {
        console.warn('Invalid ApiError format:', error);
      }
    }
  }

  // Unknown format - return as-is wrapped in legacy format
  return {
    format: 'legacy',
    error: {
      code: 'UNKNOWN_ERROR',
      message: 'An unknown error occurred',
    },
  };
}

/**
 * Safely parse potentially unknown JSON data
 * Returns null if parsing fails
 */
export function safeJsonParse(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch (error) {
    console.error('Failed to parse JSON:', {
      error,
      text: text.substring(0, 200), // Log first 200 chars
    });
    return null;
  }
}
