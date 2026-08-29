/**
 * PrintBridgeClient tests
 *
 * The client proxies to a local desktop app at http://localhost:9632.
 * Every public method has the same three failure modes:
 *   - network ok=false with JSON error body -> error.message
 *   - network ok=false with JSON missing message -> default string
 *   - fetch throws -> error message wrapped
 *   - AbortError (timeout on checkHealth) -> "connection timeout"
 *
 * The happy path returns `{ success: true, data }`. These tests exercise
 * both branches of each method so we catch regressions in error handling.
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { PrintBridgeClient } from '../PrintBridgeClient';

const okJson = (body: unknown): Response =>
  ({
    ok: true,
    status: 200,
    json: async () => body,
  }) as unknown as Response;

const errJson = (status: number, body: unknown): Response =>
  ({
    ok: false,
    status,
    json: async () => body,
  }) as unknown as Response;

describe('PrintBridgeClient', () => {
  let client: PrintBridgeClient;
  const fetchSpy = vi.fn();

  beforeEach(() => {
    fetchSpy.mockReset();
    vi.stubGlobal('fetch', fetchSpy);
    client = new PrintBridgeClient('http://localhost:9999');
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
  });

  // ---------------------------------------------------------------------------
  // checkHealth
  // ---------------------------------------------------------------------------
  describe('checkHealth', () => {
    it('uses VITE_PRINT_BRIDGE_URL when no constructor URL is provided', async () => {
      vi.stubEnv('VITE_PRINT_BRIDGE_URL', 'http://localhost:7777');
      client = new PrintBridgeClient();
      fetchSpy.mockResolvedValueOnce(
        okJson({ status: 'ok', version: '1.0.0', timestamp: 'now' })
      );

      await client.checkHealth();

      expect(fetchSpy).toHaveBeenCalledWith(
        'http://localhost:7777/health',
        expect.objectContaining({ method: 'GET' })
      );
    });

    it('returns success + data when health endpoint returns 200', async () => {
      fetchSpy.mockResolvedValueOnce(
        okJson({ status: 'ok', version: '1.0.0', timestamp: 'now' })
      );
      const res = await client.checkHealth();
      expect(res.success).toBe(true);
      if (res.success) expect(res.data.status).toBe('ok');
      expect(fetchSpy).toHaveBeenCalledWith(
        'http://localhost:9999/health',
        expect.objectContaining({ method: 'GET' })
      );
    });

    it('returns error when response is not ok', async () => {
      fetchSpy.mockResolvedValueOnce(errJson(500, {}));
      const res = await client.checkHealth();
      expect(res.success).toBe(false);
      if (!res.success) expect(res.error).toMatch(/error status/i);
    });

    it('returns timeout error when AbortError is thrown', async () => {
      const err = new Error('aborted');
      err.name = 'AbortError';
      fetchSpy.mockRejectedValueOnce(err);
      const res = await client.checkHealth();
      expect(res.success).toBe(false);
      if (!res.success) expect(res.error).toMatch(/timeout/i);
    });

    it('returns wrapped connection error on other Error', async () => {
      fetchSpy.mockRejectedValueOnce(new Error('connection refused'));
      const res = await client.checkHealth();
      expect(res.success).toBe(false);
      if (!res.success) expect(res.error).toMatch(/Cannot connect.*connection refused/i);
    });

    it('returns generic connect error on non-Error throw', async () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      fetchSpy.mockRejectedValueOnce('boom' as any);
      const res = await client.checkHealth();
      expect(res.success).toBe(false);
      if (!res.success) expect(res.error).toBe('Cannot connect to print bridge');
    });
  });

  // ---------------------------------------------------------------------------
  // getPrinters
  // ---------------------------------------------------------------------------
  describe('getPrinters', () => {
    it('returns printer list on success', async () => {
      const printers = { printers: [], count: 0, zebraCount: 0 };
      fetchSpy.mockResolvedValueOnce(okJson(printers));
      const res = await client.getPrinters();
      expect(res.success).toBe(true);
      if (res.success) expect(res.data).toEqual(printers);
    });

    it('uses error body message on failure', async () => {
      fetchSpy.mockResolvedValueOnce(errJson(500, { error: 'x', message: 'no printers' }));
      const res = await client.getPrinters();
      if (!res.success) expect(res.error).toBe('no printers');
    });

    it('falls back to default error when message is missing', async () => {
      fetchSpy.mockResolvedValueOnce(errJson(500, {}));
      const res = await client.getPrinters();
      if (!res.success) expect(res.error).toMatch(/Failed to get printers/);
    });

    it('wraps thrown errors', async () => {
      fetchSpy.mockRejectedValueOnce(new Error('offline'));
      const res = await client.getPrinters();
      if (!res.success) expect(res.error).toMatch(/offline/);
    });

    it('returns generic error on non-Error throw', async () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      fetchSpy.mockRejectedValueOnce('oops' as any);
      const res = await client.getPrinters();
      if (!res.success) expect(res.error).toBe('Failed to get printers');
    });
  });

  // ---------------------------------------------------------------------------
  // refreshPrinters
  // ---------------------------------------------------------------------------
  describe('refreshPrinters', () => {
    it('returns success + data on 200', async () => {
      fetchSpy.mockResolvedValueOnce(okJson({ message: 'refreshed', count: 3 }));
      const res = await client.refreshPrinters();
      expect(res.success).toBe(true);
      if (res.success) expect(res.data.count).toBe(3);
    });

    it('propagates error message from body', async () => {
      fetchSpy.mockResolvedValueOnce(errJson(500, { message: 'refresh failed' }));
      const res = await client.refreshPrinters();
      if (!res.success) expect(res.error).toBe('refresh failed');
    });

    it('falls back to default error text when body missing', async () => {
      fetchSpy.mockResolvedValueOnce(errJson(500, {}));
      const res = await client.refreshPrinters();
      if (!res.success) expect(res.error).toMatch(/refresh/);
    });

    it('wraps thrown Error', async () => {
      fetchSpy.mockRejectedValueOnce(new Error('network'));
      const res = await client.refreshPrinters();
      if (!res.success) expect(res.error).toMatch(/network/);
    });

    it('returns generic error on non-Error throw', async () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      fetchSpy.mockRejectedValueOnce(123 as any);
      const res = await client.refreshPrinters();
      if (!res.success) expect(res.error).toBe('Failed to refresh printers');
    });
  });

  // ---------------------------------------------------------------------------
  // printTestLabel
  // ---------------------------------------------------------------------------
  describe('printTestLabel', () => {
    it('sends printerName in body and returns data', async () => {
      fetchSpy.mockResolvedValueOnce(
        okJson({ success: true, message: 'ok', printerName: 'ZD420' })
      );
      const res = await client.printTestLabel('ZD420');
      expect(res.success).toBe(true);
      const [, init] = fetchSpy.mock.calls[0];
      expect(JSON.parse(init.body)).toEqual({ printerName: 'ZD420' });
    });

    it('uses body error message on failure', async () => {
      fetchSpy.mockResolvedValueOnce(errJson(400, { message: 'no such printer' }));
      const res = await client.printTestLabel('missing');
      if (!res.success) expect(res.error).toBe('no such printer');
    });

    it('falls back to default when body has no message', async () => {
      fetchSpy.mockResolvedValueOnce(errJson(400, {}));
      const res = await client.printTestLabel('x');
      if (!res.success) expect(res.error).toMatch(/test label/);
    });

    it('wraps Error throws', async () => {
      fetchSpy.mockRejectedValueOnce(new Error('oops'));
      const res = await client.printTestLabel('x');
      if (!res.success) expect(res.error).toMatch(/oops/);
    });

    it('generic message on non-Error throw', async () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      fetchSpy.mockRejectedValueOnce(null as any);
      const res = await client.printTestLabel('x');
      if (!res.success) expect(res.error).toBe('Failed to print test label');
    });
  });

  // ---------------------------------------------------------------------------
  // printLabel
  // ---------------------------------------------------------------------------
  describe('printLabel', () => {
    it('sends printerName and zplContent', async () => {
      fetchSpy.mockResolvedValueOnce(
        okJson({ success: true, message: 'ok', printerName: 'ZD420' })
      );
      await client.printLabel('ZD420', '^XA^XZ');
      const [, init] = fetchSpy.mock.calls[0];
      expect(JSON.parse(init.body)).toEqual({
        printerName: 'ZD420',
        zplContent: '^XA^XZ',
      });
    });

    it('returns body message on failure', async () => {
      fetchSpy.mockResolvedValueOnce(errJson(400, { message: 'nope' }));
      const res = await client.printLabel('x', 'zpl');
      if (!res.success) expect(res.error).toBe('nope');
    });

    it('default error when no body message', async () => {
      fetchSpy.mockResolvedValueOnce(errJson(500, {}));
      const res = await client.printLabel('x', 'zpl');
      if (!res.success) expect(res.error).toMatch(/print label/);
    });

    it('wraps Error throws', async () => {
      fetchSpy.mockRejectedValueOnce(new Error('no route'));
      const res = await client.printLabel('x', 'zpl');
      if (!res.success) expect(res.error).toMatch(/no route/);
    });

    it('generic message on non-Error throw', async () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      fetchSpy.mockRejectedValueOnce(undefined as any);
      const res = await client.printLabel('x', 'zpl');
      if (!res.success) expect(res.error).toBe('Failed to print label');
    });
  });

  // ---------------------------------------------------------------------------
  // printBatch
  // ---------------------------------------------------------------------------
  describe('printBatch', () => {
    it('sends array of zplContents', async () => {
      fetchSpy.mockResolvedValueOnce(
        okJson({ success: true, message: 'ok', printerName: 'x', labelCount: 2 })
      );
      await client.printBatch('x', ['a', 'b']);
      const [, init] = fetchSpy.mock.calls[0];
      expect(JSON.parse(init.body)).toEqual({
        printerName: 'x',
        zplContents: ['a', 'b'],
      });
    });

    it('body message used on failure', async () => {
      fetchSpy.mockResolvedValueOnce(errJson(400, { message: 'batch bad' }));
      const res = await client.printBatch('x', ['a']);
      if (!res.success) expect(res.error).toBe('batch bad');
    });

    it('default message when body missing message', async () => {
      fetchSpy.mockResolvedValueOnce(errJson(500, {}));
      const res = await client.printBatch('x', ['a']);
      if (!res.success) expect(res.error).toMatch(/print labels/);
    });

    it('wraps Error throws', async () => {
      fetchSpy.mockRejectedValueOnce(new Error('dead'));
      const res = await client.printBatch('x', ['a']);
      if (!res.success) expect(res.error).toMatch(/dead/);
    });

    it('generic message on non-Error throw', async () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      fetchSpy.mockRejectedValueOnce({} as any);
      const res = await client.printBatch('x', ['a']);
      if (!res.success) expect(res.error).toBe('Failed to print labels');
    });
  });

  // ---------------------------------------------------------------------------
  // getDefaultZebraPrinter
  // ---------------------------------------------------------------------------
  describe('getDefaultZebraPrinter', () => {
    const zebraDefault = {
      name: 'ZD420',
      status: 'idle',
      isDefault: true,
      isZebraPrinter: true,
      driverName: '',
      portName: '',
    };
    const zebraOther = { ...zebraDefault, name: 'ZD220', isDefault: false };
    const nonZebra = { ...zebraDefault, name: 'HPx', isZebraPrinter: false };

    it('returns default Zebra when present', async () => {
      fetchSpy.mockResolvedValueOnce(
        okJson({ printers: [zebraOther, zebraDefault, nonZebra], count: 3, zebraCount: 2 })
      );
      const res = await client.getDefaultZebraPrinter();
      expect(res.success).toBe(true);
      if (res.success) expect(res.data?.name).toBe('ZD420');
    });

    it('falls back to first Zebra when no default Zebra', async () => {
      fetchSpy.mockResolvedValueOnce(
        okJson({ printers: [nonZebra, zebraOther], count: 2, zebraCount: 1 })
      );
      const res = await client.getDefaultZebraPrinter();
      expect(res.success).toBe(true);
      if (res.success) expect(res.data?.name).toBe('ZD220');
    });

    it('returns data: null when no Zebra printers', async () => {
      fetchSpy.mockResolvedValueOnce(
        okJson({ printers: [nonZebra], count: 1, zebraCount: 0 })
      );
      const res = await client.getDefaultZebraPrinter();
      expect(res.success).toBe(true);
      if (res.success) expect(res.data).toBeNull();
    });

    it('propagates getPrinters failure', async () => {
      fetchSpy.mockRejectedValueOnce(new Error('offline'));
      const res = await client.getDefaultZebraPrinter();
      expect(res.success).toBe(false);
      if (!res.success) expect(res.error).toMatch(/offline/);
    });
  });

  it('default constructor uses localhost:9632', async () => {
    const defaultClient = new PrintBridgeClient();
    fetchSpy.mockResolvedValueOnce(okJson({ status: 'ok', version: '1', timestamp: 't' }));
    await defaultClient.checkHealth();
    expect(fetchSpy.mock.calls[0][0]).toBe('http://localhost:9632/health');
  });
});
