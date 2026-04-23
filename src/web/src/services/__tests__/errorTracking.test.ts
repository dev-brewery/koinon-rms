/**
 * errorTracking.ts tests
 *
 * The module wraps Sentry for opt-in production error reporting. The key
 * behaviours we need to catch in regression:
 *   - initErrorTracking skips init when no DSN (or explicit enabled:false).
 *   - initErrorTracking calls Sentry.init when provided an explicit DSN
 *     with enabled:true.
 *   - captureError / setUserContext / clearUserContext / addBreadcrumb /
 *     captureMessage are gated on (!DEV && VITE_SENTRY_DSN). In vitest
 *     (DEV=true by default), they must be no-ops.
 *
 * Strategy: mock @sentry/react and assert which functions were invoked.
 */
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

// Mock Sentry module BEFORE importing the code under test.
vi.mock('@sentry/react', () => ({
  init: vi.fn(),
  browserTracingIntegration: vi.fn(() => 'browserTracing'),
  replayIntegration: vi.fn(() => 'replay'),
  withScope: vi.fn((cb: (scope: unknown) => void) => {
    const scope = { setContext: vi.fn() };
    cb(scope);
  }),
  captureException: vi.fn(),
  setUser: vi.fn(),
  addBreadcrumb: vi.fn(),
  captureMessage: vi.fn(),
}));

import * as Sentry from '@sentry/react';
import {
  addBreadcrumb,
  captureError,
  captureMessage,
  clearUserContext,
  initErrorTracking,
  setUserContext,
} from '../errorTracking';

describe('errorTracking', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('initErrorTracking', () => {
    it('is a no-op when no DSN is provided and env has none', () => {
      const info = vi.spyOn(console, 'info').mockImplementation(() => {});
      initErrorTracking({ enabled: true, dsn: undefined });
      expect(Sentry.init).not.toHaveBeenCalled();
      info.mockRestore();
    });

    it('is a no-op when explicitly disabled', () => {
      const info = vi.spyOn(console, 'info').mockImplementation(() => {});
      initErrorTracking({ enabled: false, dsn: 'https://example.sentry/1' });
      expect(Sentry.init).not.toHaveBeenCalled();
      info.mockRestore();
    });

    it('initializes Sentry when DSN present and enabled:true', () => {
      const info = vi.spyOn(console, 'info').mockImplementation(() => {});
      initErrorTracking({
        dsn: 'https://example.sentry/1',
        environment: 'staging',
        enabled: true,
        tracesSampleRate: 0.25,
      });
      expect(Sentry.init).toHaveBeenCalledTimes(1);
      const call = (Sentry.init as ReturnType<typeof vi.fn>).mock.calls[0][0];
      expect(call.dsn).toBe('https://example.sentry/1');
      expect(call.environment).toBe('staging');
      expect(call.tracesSampleRate).toBe(0.25);
      // beforeSend filter should be a function; DEV mode returns null.
      expect(typeof call.beforeSend).toBe('function');
      expect(call.beforeSend({ event: 'x' })).toBeNull();
      info.mockRestore();
    });
  });

  describe('captureError / setUserContext / clearUserContext', () => {
    // In vitest (DEV=true by default), these are ALL guarded off. Calling
    // them proves the guards function (no Sentry calls are made).
    it('captureError is a no-op in DEV', () => {
      captureError(new Error('x'));
      expect(Sentry.withScope).not.toHaveBeenCalled();
      expect(Sentry.captureException).not.toHaveBeenCalled();
    });

    it('setUserContext is a no-op in DEV', () => {
      setUserContext({ id: '1', email: 'a@b' });
      expect(Sentry.setUser).not.toHaveBeenCalled();
    });

    it('clearUserContext is a no-op in DEV', () => {
      clearUserContext();
      expect(Sentry.setUser).not.toHaveBeenCalled();
    });

    it('addBreadcrumb is a no-op in DEV', () => {
      addBreadcrumb('m', 'c', 'info');
      expect(Sentry.addBreadcrumb).not.toHaveBeenCalled();
    });

    it('captureMessage is a no-op in DEV', () => {
      captureMessage('hi', 'warning');
      expect(Sentry.captureMessage).not.toHaveBeenCalled();
    });

  });

  // initErrorTracking's beforeSend filter is a function we can execute
  // regardless of DEV flags. Verifying it returns the event in non-DEV
  // environments is hard here (Vite's DEV is compile-time constant), so
  // we only assert the init-branch behaviour via initErrorTracking above.
});
