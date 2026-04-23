/**
 * api/client.ts tests
 *
 * Covers the low-level fetch wrapper:
 *   - Attaches Authorization bearer when getAccessToken returns a token.
 *   - Skips Authorization header when no token.
 *   - Sets Content-Type: application/json by default on bodies.
 *   - Preserves existing Content-Type if caller already set one.
 *   - Throws ApiException on 401 and fires onUnauthorized callback.
 *   - Throws ApiException on non-401 errors (e.g. 500).
 *   - Happy path returns the fetched Response untouched.
 *   - getApiClient / configureApiClient singleton semantics.
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  ApiClient,
  ApiException,
  configureApiClient,
  getApiBaseUrl,
  getApiClient,
} from '../client';

const makeResponse = (
  status: number,
  body: string = '',
  headers: Record<string, string> = {}
): Response => {
  const headerMap = new Headers(headers);
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 401 ? 'Unauthorized' : `status ${status}`,
    text: async () => body,
    json: async () => (body ? JSON.parse(body) : null),
    headers: headerMap,
  } as unknown as Response;
};

describe('ApiClient', () => {
  const fetchSpy = vi.fn();

  beforeEach(() => {
    fetchSpy.mockReset();
    vi.stubGlobal('fetch', fetchSpy);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('attaches Authorization header when token returned', async () => {
    fetchSpy.mockResolvedValueOnce(makeResponse(200, '{}'));
    const client = new ApiClient({
      baseUrl: 'https://api.test',
      getAccessToken: async () => 'tkn',
    });
    await client.fetch('/ping');
    const [, init] = fetchSpy.mock.calls[0];
    expect((init.headers as Headers).get('Authorization')).toBe('Bearer tkn');
  });

  it('omits Authorization when token is null', async () => {
    fetchSpy.mockResolvedValueOnce(makeResponse(200, '{}'));
    const client = new ApiClient({
      baseUrl: 'https://api.test',
      getAccessToken: async () => null,
    });
    await client.fetch('/ping');
    const [, init] = fetchSpy.mock.calls[0];
    expect((init.headers as Headers).has('Authorization')).toBe(false);
  });

  it('works when no getAccessToken is configured', async () => {
    fetchSpy.mockResolvedValueOnce(makeResponse(200, '{}'));
    const client = new ApiClient({ baseUrl: 'https://api.test' });
    const res = await client.fetch('/ping');
    expect(res.status).toBe(200);
  });

  it('sets Content-Type to application/json when a body is provided', async () => {
    fetchSpy.mockResolvedValueOnce(makeResponse(200, '{}'));
    const client = new ApiClient({ baseUrl: 'https://api.test' });
    await client.fetch('/create', { method: 'POST', body: '{"x":1}' });
    const [, init] = fetchSpy.mock.calls[0];
    expect((init.headers as Headers).get('Content-Type')).toBe('application/json');
  });

  it('preserves caller-supplied Content-Type', async () => {
    fetchSpy.mockResolvedValueOnce(makeResponse(200, ''));
    const client = new ApiClient({ baseUrl: 'https://api.test' });
    const headers = new Headers({ 'Content-Type': 'multipart/form-data' });
    await client.fetch('/upload', { method: 'POST', body: 'raw', headers });
    const [, init] = fetchSpy.mock.calls[0];
    expect((init.headers as Headers).get('Content-Type')).toBe('multipart/form-data');
  });

  it('does not set Content-Type when no body is supplied', async () => {
    fetchSpy.mockResolvedValueOnce(makeResponse(200, ''));
    const client = new ApiClient({ baseUrl: 'https://api.test' });
    await client.fetch('/ping');
    const [, init] = fetchSpy.mock.calls[0];
    expect((init.headers as Headers).get('Content-Type')).toBeNull();
  });

  it('fires onUnauthorized and throws ApiException on 401', async () => {
    fetchSpy.mockResolvedValueOnce(makeResponse(401, 'no token'));
    const onUnauthorized = vi.fn();
    const client = new ApiClient({
      baseUrl: 'https://api.test',
      onUnauthorized,
    });
    await expect(client.fetch('/secure')).rejects.toBeInstanceOf(ApiException);
    expect(onUnauthorized).toHaveBeenCalled();
  });

  it('does not require onUnauthorized to be set on 401', async () => {
    fetchSpy.mockResolvedValueOnce(makeResponse(401, ''));
    const client = new ApiClient({ baseUrl: 'https://api.test' });
    await expect(client.fetch('/secure')).rejects.toBeInstanceOf(ApiException);
  });

  it('throws ApiException on non-401 error with status/text', async () => {
    fetchSpy.mockResolvedValueOnce(
      makeResponse(500, 'boom', { 'x-trace': 'abc' })
    );
    const client = new ApiClient({ baseUrl: 'https://api.test' });
    try {
      await client.fetch('/boom');
      expect.unreachable();
    } catch (e) {
      expect(e).toBeInstanceOf(ApiException);
      const err = e as ApiException;
      expect(err.status).toBe(500);
      expect(err.response).toBe('boom');
      expect(err.headers['x-trace']).toBe('abc');
    }
  });

  it('returns the Response on success', async () => {
    const r = makeResponse(200, 'ok');
    fetchSpy.mockResolvedValueOnce(r);
    const client = new ApiClient({ baseUrl: 'https://api.test' });
    const result = await client.fetch('/ping');
    expect(result).toBe(r);
  });

  it('ApiException carries status, statusText, response, headers, result', () => {
    const e = new ApiException(422, 'invalid', 'bad', { h: '1' }, { msg: 'x' });
    expect(e.name).toBe('ApiException');
    expect(e.status).toBe(422);
    expect(e.message).toMatch(/422.*invalid/);
    expect(e.headers.h).toBe('1');
    expect(e.result).toEqual({ msg: 'x' });
  });
});

describe('getApiBaseUrl', () => {
  it('returns a non-empty URL (env default or provided)', () => {
    const url = getApiBaseUrl();
    expect(url.length).toBeGreaterThan(0);
    expect(url).toMatch(/^http/);
  });
});

describe('configureApiClient / getApiClient', () => {
  const fetchSpy = vi.fn();

  beforeEach(() => {
    fetchSpy.mockReset();
    vi.stubGlobal('fetch', fetchSpy);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('returns the configured client when configureApiClient is called', async () => {
    configureApiClient({
      baseUrl: 'https://configured.test',
      getAccessToken: async () => 'ctok',
    });
    fetchSpy.mockResolvedValueOnce(makeResponse(200, ''));
    const client = getApiClient();
    await client.fetch('/x');
    expect(fetchSpy.mock.calls[0][0]).toBe('https://configured.test/x');
  });

  it('falls back to env-based client when never configured', async () => {
    // Force a reset via fresh module import
    vi.resetModules();
    const mod = await import('../client');
    fetchSpy.mockResolvedValueOnce(makeResponse(200, ''));
    const client = mod.getApiClient();
    await client.fetch('/fallback');
    // URL should resolve from env default to http://localhost:5000 or env var.
    const url = fetchSpy.mock.calls[0][0];
    expect(url).toMatch(/^http.*\/fallback$/);
  });
});
