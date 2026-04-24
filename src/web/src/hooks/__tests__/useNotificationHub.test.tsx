/**
 * useNotificationHub tests - wave 7 of #679.
 *
 * The hook dynamically imports @microsoft/signalr and connects to the
 * /hubs/notifications endpoint. We mock both the signalr package and the
 * api/client `getAccessToken` helper at the module boundary. That keeps the
 * test deterministic — no network, no browser WebSocket, no auth.
 *
 * Rationale for unit-not-E2E: reconnect timing, network-error branches, and
 * token-missing paths are impossible to reliably trigger from Playwright.
 * Mocking HubConnection directly gives us direct control over every state
 * transition the hook cares about.
 */

import { waitFor, act } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

// --- Mock @microsoft/signalr ------------------------------------------------
// The HubConnectionBuilder fluent API returns a mock connection whose methods
// are spies. Tests reach into `lastConnection` to drive handlers the hook
// registered via `.on(...)`, `.onreconnecting(...)`, etc.

type Handler = (...args: unknown[]) => void;

interface MockHubConnection {
  start: ReturnType<typeof vi.fn>;
  stop: ReturnType<typeof vi.fn>;
  on: ReturnType<typeof vi.fn>;
  off: ReturnType<typeof vi.fn>;
  invoke: ReturnType<typeof vi.fn>;
  onreconnecting: ReturnType<typeof vi.fn>;
  onreconnected: ReturnType<typeof vi.fn>;
  onclose: ReturnType<typeof vi.fn>;
  state: string;
  eventHandlers: Map<string, Handler>;
  reconnectingHandler?: Handler;
  reconnectedHandler?: Handler;
  closeHandler?: (err?: Error) => void;
}

const hubState: { last: MockHubConnection | null; startError: Error | null } = {
  last: null,
  startError: null,
};

function makeConnection(): MockHubConnection {
  const conn: MockHubConnection = {
    start: vi.fn(async () => {
      if (hubState.startError) {
        const e = hubState.startError;
        hubState.startError = null;
        throw e;
      }
      conn.state = 'Connected';
    }),
    stop: vi.fn(async () => {
      conn.state = 'Disconnected';
    }),
    on: vi.fn((event: string, cb: Handler) => {
      conn.eventHandlers.set(event, cb);
    }),
    off: vi.fn(),
    invoke: vi.fn(async () => undefined),
    onreconnecting: vi.fn((cb: Handler) => {
      conn.reconnectingHandler = cb;
    }),
    onreconnected: vi.fn((cb: Handler) => {
      conn.reconnectedHandler = cb;
    }),
    onclose: vi.fn((cb: (err?: Error) => void) => {
      conn.closeHandler = cb;
    }),
    state: 'Disconnected',
    eventHandlers: new Map(),
  };
  return conn;
}

vi.mock('@microsoft/signalr', () => {
  class HubConnectionBuilder {
    withUrl() {
      return this;
    }
    withAutomaticReconnect() {
      return this;
    }
    build() {
      const conn = makeConnection();
      hubState.last = conn;
      return conn;
    }
  }
  const HubConnectionState = {
    Disconnected: 'Disconnected',
    Connecting: 'Connecting',
    Connected: 'Connected',
    Disconnecting: 'Disconnecting',
    Reconnecting: 'Reconnecting',
  };
  return { HubConnectionBuilder, HubConnectionState };
});

// --- Mock api/client `getAccessToken` --------------------------------------
// The hook dynamically imports this, so we mock the module used.
const tokenState = { token: 'fake-jwt-token' as string | null };
vi.mock('@/services/api/client', () => ({
  getAccessToken: () => tokenState.token,
  // Preserve ApiClientError shape in case anything else imports from here in
  // the same test transform run. Keeping it as a named class avoids surprises.
  ApiClientError: class ApiClientError extends Error {
    constructor(
      public status: number,
      message: string
    ) {
      super(message);
    }
  },
}));

import { useNotificationHub } from '../useNotificationHub';

beforeEach(() => {
  hubState.last = null;
  hubState.startError = null;
  tokenState.token = 'fake-jwt-token';
});

afterEach(() => {
  vi.restoreAllMocks();
});

describe('useNotificationHub', () => {
  it('builds a connection and calls start() on mount', async () => {
    const { result, unmount } = renderHookWithQuery(() => useNotificationHub());

    await waitFor(() => expect(hubState.last).not.toBeNull());
    await waitFor(() => expect(hubState.last?.start).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(result.current.isConnected).toBe(true));
    expect(result.current.error).toBeNull();
    unmount();
  });

  it('does not connect when enabled=false', async () => {
    const { result, unmount } = renderHookWithQuery(() => useNotificationHub(false));

    // Give any async import a chance; nothing should happen.
    await new Promise((r) => setTimeout(r, 20));
    expect(hubState.last).toBeNull();
    expect(result.current.isConnected).toBe(false);
    unmount();
  });

  it('reports an error when no access token is available', async () => {
    tokenState.token = null;
    const { result, unmount } = renderHookWithQuery(() => useNotificationHub());

    await waitFor(() => expect(result.current.error).toBe('No access token available'));
    // Connection should not have been built without a token.
    expect(hubState.last).toBeNull();
    unmount();
  });

  it('reports an error when connection.start() rejects', async () => {
    hubState.startError = new Error('boom');
    const { result, unmount } = renderHookWithQuery(() => useNotificationHub());

    await waitFor(() => expect(result.current.error).toBe('boom'));
    expect(result.current.isConnected).toBe(false);
    unmount();
  });

  it('invalidates notification queries when ReceiveNotification fires', async () => {
    const { client, unmount } = renderHookWithQuery(() => useNotificationHub());

    await waitFor(() => expect(hubState.last).not.toBeNull());
    await waitFor(() => expect(hubState.last?.start).toHaveBeenCalled());

    const invalidate = vi.spyOn(client, 'invalidateQueries');
    act(() => {
      hubState.last?.eventHandlers.get('ReceiveNotification')?.({ id: 1 });
    });

    // Fired twice: once for ['notifications'], once for unread-count.
    expect(invalidate).toHaveBeenCalled();
    expect(invalidate.mock.calls.length).toBeGreaterThanOrEqual(2);
    unmount();
  });

  it('updates the unread-count cache when UnreadCountUpdated fires', async () => {
    const { client, unmount } = renderHookWithQuery(() => useNotificationHub());

    await waitFor(() => expect(hubState.last).not.toBeNull());
    await waitFor(() => expect(hubState.last?.start).toHaveBeenCalled());

    act(() => {
      hubState.last?.eventHandlers.get('UnreadCountUpdated')?.(7);
    });

    expect(client.getQueryData(['notifications', 'unread-count'])).toBe(7);
    unmount();
  });

  it('flips isConnecting true during a reconnect and back to connected', async () => {
    const { result, unmount } = renderHookWithQuery(() => useNotificationHub());

    await waitFor(() => expect(result.current.isConnected).toBe(true));

    act(() => {
      hubState.last?.reconnectingHandler?.();
    });
    expect(result.current.isConnecting).toBe(true);
    expect(result.current.isConnected).toBe(false);

    act(() => {
      hubState.last?.reconnectedHandler?.();
    });
    expect(result.current.isConnected).toBe(true);
    expect(result.current.isConnecting).toBe(false);
    unmount();
  });

  it('calls stop() on unmount when connected', async () => {
    const { unmount } = renderHookWithQuery(() => useNotificationHub());

    await waitFor(() => expect(hubState.last).not.toBeNull());
    await waitFor(() => expect(hubState.last?.start).toHaveBeenCalled());
    const conn = hubState.last!;
    // state is 'Connected' after start() resolves, so cleanup should call stop().
    unmount();
    expect(conn.stop).toHaveBeenCalled();
  });
});
