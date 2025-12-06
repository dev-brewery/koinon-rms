/**
 * Tests for API client
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import {
  get,
  post,
  setTokens,
  clearTokens,
  getAccessToken,
  getRefreshToken,
  ApiClientError,
} from '../client';

// Mock fetch globally
const mockFetch = vi.fn();
global.fetch = mockFetch;

describe('apiClient', () => {
  beforeEach(() => {
    mockFetch.mockReset();
    clearTokens();
    vi.clearAllTimers();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Token Management', () => {
    it('should store tokens in memory', () => {
      setTokens('access-123', 'refresh-456');
      expect(getAccessToken()).toBe('access-123');
      expect(getRefreshToken()).toBe('refresh-456');
    });

    it('should clear tokens', () => {
      setTokens('access-123', 'refresh-456');
      clearTokens();
      expect(getAccessToken()).toBeNull();
      expect(getRefreshToken()).toBeNull();
    });
  });

  describe('Basic HTTP Operations', () => {
    it('should make GET request successfully', async () => {
      const mockData = { data: { id: 1, name: 'Test' } };
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        headers: new Headers({ 'content-type': 'application/json' }),
        text: async () => JSON.stringify(mockData),
      });

      const result = await get<typeof mockData>('/test');

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/v1/test'),
        expect.objectContaining({
          method: 'GET',
        })
      );
      expect(result).toEqual(mockData);
    });

    it('should make POST request with body', async () => {
      const mockData = { data: { id: 1 } };
      const requestBody = { name: 'New Item' };

      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        headers: new Headers({ 'content-type': 'application/json' }),
        text: async () => JSON.stringify(mockData),
      });

      const result = await post<typeof mockData>('/test', requestBody);

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/v1/test'),
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify(requestBody),
        })
      );
      expect(result).toEqual(mockData);
    });

    it('should handle 204 No Content responses', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 204,
        headers: new Headers(),
      });

      const result = await get<void>('/test');

      expect(result).toBeUndefined();
    });
  });

  describe('Authentication', () => {
    it('should include Authorization header when token is set', async () => {
      setTokens('my-access-token', 'my-refresh-token');

      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        headers: new Headers({ 'content-type': 'application/json' }),
        text: async () => JSON.stringify({ data: {} }),
      });

      await get('/test');

      const callArgs = mockFetch.mock.calls[0];
      const headers = callArgs[1].headers as Headers;
      expect(headers.get('Authorization')).toBe('Bearer my-access-token');
    });

    it('should skip auth header when skipAuth is true', async () => {
      setTokens('my-access-token', 'my-refresh-token');

      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        headers: new Headers({ 'content-type': 'application/json' }),
        text: async () => JSON.stringify({ data: {} }),
      });

      await get('/test', { skipAuth: true });

      const callArgs = mockFetch.mock.calls[0];
      const headers = callArgs[1].headers as Headers;
      expect(headers.get('Authorization')).toBeNull();
    });
  });

  describe('Token Refresh', () => {
    it('should refresh token on 401 and retry request', async () => {
      setTokens('old-access', 'refresh-token');

      // First call returns 401
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 401,
        headers: new Headers({ 'content-type': 'application/json' }),
        text: async () => JSON.stringify({
          error: { code: 'UNAUTHORIZED', message: 'Token expired' },
        }),
      });

      // Refresh token call succeeds
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        headers: new Headers({ 'content-type': 'application/json' }),
        text: async () => JSON.stringify({
          data: {
            accessToken: 'new-access',
            refreshToken: 'new-refresh',
            expiresAt: '2024-01-01T00:00:00Z',
          },
        }),
      });

      // Retry with new token succeeds
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        headers: new Headers({ 'content-type': 'application/json' }),
        text: async () => JSON.stringify({ data: { success: true } }),
      });

      const result = await get<{ data: { success: boolean } }>('/test');

      expect(mockFetch).toHaveBeenCalledTimes(3);
      expect(result).toEqual({ data: { success: true } });
      expect(getAccessToken()).toBe('new-access');
      expect(getRefreshToken()).toBe('new-refresh');
    });

    it('should clear tokens if refresh fails', async () => {
      setTokens('old-access', 'refresh-token');

      // First call returns 401
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 401,
        headers: new Headers({ 'content-type': 'application/json' }),
        text: async () => JSON.stringify({
          error: { code: 'UNAUTHORIZED', message: 'Token expired' },
        }),
      });

      // Refresh token call fails
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 401,
        headers: new Headers({ 'content-type': 'application/json' }),
        text: async () => JSON.stringify({
          error: { code: 'INVALID_TOKEN', message: 'Refresh token invalid' },
        }),
      });

      await expect(get('/test')).rejects.toThrow(ApiClientError);

      expect(getAccessToken()).toBeNull();
      expect(getRefreshToken()).toBeNull();
    });
  });

  describe('Error Handling', () => {
    it('should throw ApiClientError for API errors', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 404,
        statusText: 'Not Found',
        headers: new Headers({ 'content-type': 'application/json' }),
        text: async () => JSON.stringify({
          error: {
            code: 'NOT_FOUND',
            message: 'Resource not found',
          },
        }),
      });

      try {
        await get('/test');
        expect.fail('Should have thrown');
      } catch (error) {
        expect(error).toBeInstanceOf(ApiClientError);
        expect((error as ApiClientError).statusCode).toBe(404);
        expect((error as ApiClientError).error.code).toBe('NOT_FOUND');
      }
    });

    it('should handle non-JSON error responses', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        statusText: 'Internal Server Error',
        headers: new Headers({ 'content-type': 'text/plain' }),
        text: async () => 'Something went wrong',
      });

      try {
        await get('/test');
        expect.fail('Should have thrown');
      } catch (error) {
        expect(error).toBeInstanceOf(ApiClientError);
        expect((error as ApiClientError).statusCode).toBe(500);
        expect((error as ApiClientError).error.message).toBe('Something went wrong');
      }
    });

    it('should handle network errors', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Network error'));

      try {
        await get('/test');
        expect.fail('Should have thrown');
      } catch (error) {
        expect(error).toBeInstanceOf(Error);
        expect((error as Error).message).toBe('Network error');
      }
    });
  });

  describe('Timeout Handling', () => {
    it('should include timeout in request options', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        headers: new Headers({ 'content-type': 'application/json' }),
        text: async () => JSON.stringify({ data: {} }),
      });

      await get('/test', { timeout: 5000 });

      // Verify that the fetch was called with an AbortSignal
      const callArgs = mockFetch.mock.calls[0];
      expect(callArgs[1].signal).toBeInstanceOf(AbortSignal);
    });

    it('should handle AbortError as timeout', async () => {
      const abortError = new Error('The operation was aborted');
      abortError.name = 'AbortError';
      mockFetch.mockRejectedValueOnce(abortError);

      try {
        await get('/test');
        expect.fail('Should have thrown');
      } catch (error) {
        expect(error).toBeInstanceOf(ApiClientError);
        expect((error as ApiClientError).statusCode).toBe(408);
        expect((error as ApiClientError).error.code).toBe('REQUEST_TIMEOUT');
      }
    });
  });

  describe('Response Validation', () => {
    it('should reject invalid JSON responses', async () => {
      const mockResponse = {
        ok: true,
        status: 200,
        headers: new Headers({ 'content-type': 'application/json' }),
        text: vi.fn().mockResolvedValue('not valid json'),
      };
      mockFetch.mockResolvedValueOnce(mockResponse);

      try {
        await get('/test');
        expect.fail('Should have thrown');
      } catch (error) {
        expect(error).toBeInstanceOf(Error);
        expect((error as Error).message).toContain('Invalid JSON response');
      }
    });

    it('should handle empty JSON responses', async () => {
      const mockResponse = {
        ok: true,
        status: 200,
        headers: new Headers({ 'content-type': 'application/json' }),
        text: vi.fn().mockResolvedValue(''),
      };
      mockFetch.mockResolvedValueOnce(mockResponse);

      try {
        await get('/test');
        expect.fail('Should have thrown');
      } catch (error) {
        expect(error).toBeInstanceOf(Error);
        expect((error as Error).message).toContain('Invalid JSON response');
      }
    });
  });
});
