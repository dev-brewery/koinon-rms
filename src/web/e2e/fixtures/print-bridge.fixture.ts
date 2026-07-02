/**
 * Print Bridge mock fixture
 *
 * The check-in kiosk prints labels through the Koinon Print Bridge — a local
 * Windows tray app serving HTTP on localhost:9632 (tools/print-bridge). Real
 * printer drivers (Zebra ZPL / Dymo GDI) can NEVER be part of a browser test:
 * they're machine-specific, stateful, and the #1 source of "works on my kiosk"
 * bugs. Browser tests therefore mock the bridge at the network layer and
 * assert on the PAYLOAD the app sends — which is exactly the contract the
 * bridge honors.
 *
 * Endpoints mocked (mirror src/web/src/services/printing/PrintBridgeClient.ts):
 *   GET  /health
 *   GET  /api/printers
 *   POST /api/printers/refresh
 *   POST /api/print/test
 *   POST /api/print
 *   POST /api/print/batch
 *
 * Usage:
 *   import { test, expect } from '../../fixtures/print-bridge.fixture';
 *
 *   test('prints labels after check-in', async ({ page, printBridge }) => {
 *     // ... complete a check-in ...
 *     expect(printBridge.jobs).toHaveLength(1);
 *     expect(printBridge.jobs[0].labels[0].zpl).toContain('^XA'); // ZPL start
 *   });
 *
 *   test('kiosk degrades gracefully when bridge is down', async ({ page, printBridge }) => {
 *     printBridge.setMode('offline');
 *     // ... check-in still completes; UI shows the no-printer path ...
 *   });
 */

import { test as base, expect, type Page } from '@playwright/test';

export type PrintBridgeMode = 'ok' | 'offline' | 'printer-error' | 'no-printers';

export interface RecordedPrintJob {
  endpoint: string; // '/api/print' | '/api/print/batch' | '/api/print/test'
  body: unknown;
  labels: Array<{ zpl?: string; image?: string; [k: string]: unknown }>;
}

export interface PrintBridgeMock {
  /** Every print request the app sent, in order. Assert on these. */
  jobs: RecordedPrintJob[];
  /** Change behavior mid-test to exercise failure paths. */
  setMode: (mode: PrintBridgeMode) => void;
  mode: () => PrintBridgeMode;
}

const MOCK_PRINTER = {
  name: 'ZDesigner ZD410-203dpi ZPL (MOCK)',
  driverName: 'ZDesigner ZD410-203dpi ZPL',
  portName: 'USB001',
  isDefault: true,
  printerType: 'Zebra',
  isOnline: true,
};

export async function installPrintBridgeMock(page: Page): Promise<PrintBridgeMock> {
  let mode: PrintBridgeMode = 'ok';
  const jobs: RecordedPrintJob[] = [];

  await page.route('http://localhost:9632/**', async (route) => {
    const url = new URL(route.request().url());
    const path = url.pathname;

    if (mode === 'offline') {
      // Bridge not running: connection refused. abort() is the honest simulation.
      return route.abort('connectionrefused');
    }

    const json = (status: number, body: unknown) =>
      route.fulfill({
        status,
        contentType: 'application/json',
        headers: { 'Access-Control-Allow-Origin': '*' },
        body: JSON.stringify(body),
      });

    if (path === '/health') {
      return json(200, { status: 'healthy', version: 'mock', printersAvailable: mode !== 'no-printers' });
    }
    if (path === '/api/printers' || path === '/api/printers/refresh') {
      return json(200, { data: mode === 'no-printers' ? [] : [MOCK_PRINTER] });
    }
    if (path === '/api/print' || path === '/api/print/batch' || path === '/api/print/test') {
      let body: unknown = null;
      try {
        body = route.request().postDataJSON();
      } catch {
        /* non-JSON body */
      }
      const b = body as { labels?: unknown[]; zpl?: string } | null;
      jobs.push({
        endpoint: path,
        body,
        labels: Array.isArray(b?.labels) ? (b!.labels as RecordedPrintJob['labels']) : b ? [b as never] : [],
      });
      if (mode === 'printer-error') {
        return json(500, { error: { code: 'PRINTER_ERROR', message: 'Mock: printer jam / out of labels' } });
      }
      return json(200, { data: { success: true, jobId: `mock-${jobs.length}` } });
    }

    return json(404, { error: { code: 'NOT_FOUND', message: `Unmocked print-bridge path: ${path}` } });
  });

  return {
    jobs,
    setMode: (m) => {
      mode = m;
    },
    mode: () => mode,
  };
}

/**
 * Test fixture: `printBridge` is installed BEFORE navigation, so the kiosk's
 * printer-availability probe on mount sees the mock.
 */
export const test = base.extend<{ printBridge: PrintBridgeMock }>({
  printBridge: async ({ page }, use) => {
    const mock = await installPrintBridgeMock(page);
    await use(mock);
  },
});

export { expect };
