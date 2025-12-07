/**
 * Tests for PrintBridgeClient
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import {
  PrintBridgeClient,
  PrintBridgeError,
  getPrintBridgeClient,
  resetPrintBridgeClient,
} from './PrintBridgeClient';
import type { HealthResponse, PrintersResponse, PrintResult } from './types';

// Mock fetch globally
const mockFetch = vi.fn();
vi.stubGlobal('fetch', mockFetch);

describe('PrintBridgeClient', () => {
  beforeEach(() => {
    resetPrintBridgeClient();
    mockFetch.mockClear();
  });

  afterEach(() => {
    resetPrintBridgeClient();
  });

  describe('constructor', () => {
    it('should create client with default config', () => {
      const client = new PrintBridgeClient();
      expect(client).toBeDefined();
    });

    it('should create client with custom config', () => {
      const client = new PrintBridgeClient({
        baseUrl: 'http://192.168.1.100:9632',
        timeout: 10000,
      });
      expect(client).toBeDefined();
    });
  });

  describe('singleton pattern', () => {
    it('should return same instance on subsequent calls', () => {
      const client1 = getPrintBridgeClient();
      const client2 = getPrintBridgeClient();
      expect(client1).toBe(client2);
    });

    it('should reset singleton', () => {
      const client1 = getPrintBridgeClient();
      resetPrintBridgeClient();
      const client2 = getPrintBridgeClient();
      expect(client1).not.toBe(client2);
    });
  });

  describe('isAvailable', () => {
    it('should return true when health check succeeds', async () => {
      const mockResponse: HealthResponse = {
        status: 'healthy',
        version: '1.0.0',
        timestamp: new Date().toISOString(),
      };

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockResponse,
      } as Response);

      const client = new PrintBridgeClient();
      const result = await client.isAvailable();

      expect(result).toBe(true);
      expect(mockFetch).toHaveBeenCalledWith(
        'http://127.0.0.1:9632/api/v1/print/health',
        expect.any(Object)
      );
    });

    it('should return false when health check fails', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Connection refused'));

      const client = new PrintBridgeClient();
      const result = await client.isAvailable();

      expect(result).toBe(false);
    });

    it('should cache availability result', async () => {
      const mockResponse: HealthResponse = {
        status: 'healthy',
        version: '1.0.0',
        timestamp: new Date().toISOString(),
      };

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockResponse,
      } as Response);

      const client = new PrintBridgeClient();

      // First call
      await client.isAvailable();
      expect(mockFetch).toHaveBeenCalledTimes(1);

      // Second call (should use cache)
      await client.isAvailable();
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });
  });

  describe('health', () => {
    it('should return health status', async () => {
      const mockResponse: HealthResponse = {
        status: 'healthy',
        version: '1.0.0',
        defaultPrinter: 'Zebra ZD420',
        timestamp: new Date().toISOString(),
      };

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockResponse,
      } as Response);

      const client = new PrintBridgeClient();
      const result = await client.health();

      expect(result).toEqual(mockResponse);
    });

    it('should throw error on health check failure', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
      } as Response);

      const client = new PrintBridgeClient();

      await expect(client.health()).rejects.toThrow(PrintBridgeError);
    });
  });

  describe('getPrinters', () => {
    it('should return list of printers', async () => {
      const mockResponse: PrintersResponse = {
        printers: [
          {
            name: 'Zebra ZD420',
            type: 'ZPL',
            status: 'Ready',
            isDefault: true,
          },
          {
            name: 'Dymo Label',
            type: 'EPL',
            status: 'Ready',
            isDefault: false,
          },
        ],
      };

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ status: 'healthy', version: '1.0.0', timestamp: new Date().toISOString() }),
        } as Response)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockResponse,
        } as Response);

      const client = new PrintBridgeClient();
      const result = await client.getPrinters();

      expect(result).toEqual(mockResponse.printers);
      expect(result).toHaveLength(2);
      expect(result[0].name).toBe('Zebra ZD420');
    });

    it('should throw error when PrintBridge unavailable', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Connection refused'));

      const client = new PrintBridgeClient();

      await expect(client.getPrinters()).rejects.toThrow(PrintBridgeError);
      await expect(client.getPrinters()).rejects.toThrow('PrintBridge is not available');
    });
  });

  describe('print', () => {
    it('should send print request successfully', async () => {
      const mockPrintResult: PrintResult = {
        success: true,
        message: 'Printed 1 label on Zebra ZD420',
        printerName: 'Zebra ZD420',
      };

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ status: 'healthy', version: '1.0.0', timestamp: new Date().toISOString() }),
        } as Response)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockPrintResult,
        } as Response);

      const client = new PrintBridgeClient();
      const result = await client.print({
        zplContent: '^XA^FDTest^FS^XZ',
        printerName: 'Zebra ZD420',
        copies: 1,
      });

      expect(result).toEqual(mockPrintResult);
      expect(result.success).toBe(true);
    });

    it('should throw error on empty ZPL content', async () => {
      const client = new PrintBridgeClient();

      await expect(
        client.print({ zplContent: '' })
      ).rejects.toThrow('ZPL content cannot be empty');
    });

    it('should throw error on invalid copies count', async () => {
      const client = new PrintBridgeClient();

      await expect(
        client.print({ zplContent: '^XA^XZ', copies: 1000 })
      ).rejects.toThrow('Copies must be between 1 and 999');
    });

    it('should throw error when PrintBridge unavailable', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Connection refused'));

      const client = new PrintBridgeClient();

      await expect(
        client.print({ zplContent: '^XA^XZ' })
      ).rejects.toThrow('PrintBridge is not available');
    });

    it('should use default printer if not specified', async () => {
      const mockPrintResult: PrintResult = {
        success: true,
        message: 'Printed 1 label on Default Printer',
        printerName: 'Default Printer',
      };

      let capturedBody: string | undefined;

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ status: 'healthy', version: '1.0.0', timestamp: new Date().toISOString() }),
        } as Response)
        .mockImplementationOnce(async (_url: string | Request, init?: RequestInit) => {
          if (init?.body) {
            capturedBody = init.body as string;
          }
          return {
            ok: true,
            json: async () => mockPrintResult,
          };
        });

      const client = new PrintBridgeClient();
      await client.print({ zplContent: '^XA^XZ' });

      expect(capturedBody).toBeDefined();
      const body = JSON.parse(capturedBody!);
      expect(body.printerName).toBeUndefined();
    });
  });

  describe('testPrint', () => {
    it('should send test print request', async () => {
      const mockResult: PrintResult = {
        success: true,
        message: 'Printed 1 label on Zebra ZD420',
        printerName: 'Zebra ZD420',
      };

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ status: 'healthy', version: '1.0.0', timestamp: new Date().toISOString() }),
        } as Response)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockResult,
        } as Response);

      const client = new PrintBridgeClient();
      const result = await client.testPrint('Zebra ZD420');

      expect(result.success).toBe(true);
      expect(result.printerName).toBe('Zebra ZD420');
    });
  });

  describe('error handling', () => {
    it('should create PrintBridgeError with code', () => {
      const error = new PrintBridgeError('Test error', 'TEST_CODE');

      expect(error).toBeInstanceOf(Error);
      expect(error).toBeInstanceOf(PrintBridgeError);
      expect(error.message).toBe('Test error');
      expect(error.code).toBe('TEST_CODE');
      expect(error.name).toBe('PrintBridgeError');
    });

    it('should invalidate cache on network error', async () => {
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ status: 'healthy', version: '1.0.0', timestamp: new Date().toISOString() }),
        } as Response)
        .mockRejectedValueOnce(new Error('Network error'));

      const client = new PrintBridgeClient();

      // First, make it available
      await client.isAvailable();
      expect(await client.isAvailable()).toBe(true);

      // Try to print (will fail)
      try {
        await client.print({ zplContent: '^XA^XZ' });
      } catch {
        // Expected
      }

      // Cache should be invalidated
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ status: 'healthy', version: '1.0.0', timestamp: new Date().toISOString() }),
      } as Response);

      expect(await client.isAvailable()).toBe(true);
      expect(mockFetch.mock.calls.length).toBeGreaterThan(2);
    });
  });

  describe('timeout handling', () => {
    it('should timeout on slow requests', async () => {
      vi.useFakeTimers();

      mockFetch.mockImplementationOnce(
        () =>
          new Promise(() => {
            // Never resolves
          })
      );

      const client = new PrintBridgeClient({ timeout: 100 });

      const promise = client.health();

      // Fast-forward timers
      vi.advanceTimersByTime(100);

      await expect(promise).rejects.toThrow(PrintBridgeError);

      vi.useRealTimers();
    });
  });
});
