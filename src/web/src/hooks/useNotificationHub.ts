/**
 * SignalR notification hub hook for real-time updates
 * 
 * IMPORTANT: This hook requires @microsoft/signalr package to be installed:
 * npm install @microsoft/signalr
 */

import { useEffect, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';

// Type-only import to avoid errors if package is not installed
type HubConnection = unknown;

// Lazy import SignalR to avoid errors if package is not installed
// eslint-disable-next-line @typescript-eslint/no-explicit-any
let HubConnectionBuilder: any = null;
// eslint-disable-next-line @typescript-eslint/no-explicit-any
let HubConnectionState: any = null;

async function loadSignalR() {
  if (!HubConnectionBuilder) {
    try {
      // Dynamic import with proper error handling for missing package
      // Using string variable to avoid TypeScript module resolution
      const moduleName = '@microsoft/signalr';
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const signalR: any = await import(/* @vite-ignore */ moduleName).catch(() => {
        throw new Error('@microsoft/signalr package is not installed');
      });
      HubConnectionBuilder = signalR.HubConnectionBuilder;
      HubConnectionState = signalR.HubConnectionState;
    } catch (error: unknown) {
      if (import.meta.env.DEV) {
        console.error(
          'Failed to load @microsoft/signalr. Install it with: npm install @microsoft/signalr',
          error
        );
      }
      throw error;
    }
  }
}

// ============================================================================
// Configuration
// ============================================================================

const HUB_URL = import.meta.env.VITE_API_URL
  ? `${import.meta.env.VITE_API_URL.replace('/api/v1', '')}/hubs/notifications`
  : 'http://localhost:5000/hubs/notifications';

const RECONNECT_DELAY_MS = 5000; // 5 seconds

// ============================================================================
// Query Keys (matching useNotifications.ts)
// ============================================================================

const QUERY_KEYS = {
  all: ['notifications'] as const,
  unreadCount: ['notifications', 'unread-count'] as const,
};

// ============================================================================
// Hook
// ============================================================================

export interface NotificationHubState {
  isConnected: boolean;
  isConnecting: boolean;
  error: string | null;
}

/**
 * Connect to the notifications SignalR hub for real-time updates
 * 
 * This hook:
 * - Connects to /hubs/notifications using the current auth token
 * - Listens for ReceiveNotification events and invalidates notification queries
 * - Listens for UnreadCountUpdated events and updates the unread count cache
 * - Automatically reconnects on disconnect
 * 
 * @param enabled - If false, the hub connection will not be established
 * @returns Connection state
 */
export function useNotificationHub(enabled = true): NotificationHubState {
  const queryClient = useQueryClient();
  const connectionRef = useRef<HubConnection | null>(null);
  const reconnectTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  
  const [state, setState] = useState<NotificationHubState>({
    isConnected: false,
    isConnecting: false,
    error: null,
  });

  useEffect(() => {
    if (!enabled) {
      return;
    }

    let isMounted = true;

    async function connect() {
      try {
        await loadSignalR();

        if (!HubConnectionBuilder || !isMounted) {
          return;
        }

        setState(prev => ({ ...prev, isConnecting: true, error: null }));

        // Import getAccessToken dynamically
        const { getAccessToken } = await import('@/services/api/client');
        const token = getAccessToken();

        if (!token) {
          if (import.meta.env.DEV) {
            console.warn('No access token available for SignalR connection');
          }
          setState({
            isConnected: false,
            isConnecting: false,
            error: 'No access token available',
          });
          return;
        }

        // Build the connection
        const connection = new HubConnectionBuilder()
          .withUrl(HUB_URL, {
            accessTokenFactory: () => token,
          })
          .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: () => RECONNECT_DELAY_MS,
          })
          .build();

        connectionRef.current = connection;

        // Handle connection events
        connection.onreconnecting(() => {
          if (isMounted) {
            setState({
              isConnected: false,
              isConnecting: true,
              error: null,
            });
          }
        });

        connection.onreconnected(() => {
          if (isMounted) {
            setState({
              isConnected: true,
              isConnecting: false,
              error: null,
            });
            // Refetch all notifications data after reconnection
            queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
            queryClient.invalidateQueries({ queryKey: QUERY_KEYS.unreadCount });
          }
        });

        connection.onclose((error?: Error) => {
          if (isMounted) {
            const errorMessage = error ? error.message : 'Connection closed';
            setState({
              isConnected: false,
              isConnecting: false,
              error: errorMessage,
            });

            // Attempt to reconnect after delay
            reconnectTimeoutRef.current = setTimeout(() => {
              if (isMounted) {
                connect();
              }
            }, RECONNECT_DELAY_MS);
          }
        });

        // Set up event handlers
        connection.on('ReceiveNotification', (notification: unknown) => {
          if (import.meta.env.DEV) {
            console.info('Received notification via SignalR:', notification);
          }
          // Invalidate notification queries to refetch with new notification
          queryClient.invalidateQueries({ queryKey: QUERY_KEYS.all });
          queryClient.invalidateQueries({ queryKey: QUERY_KEYS.unreadCount });
        });

        connection.on('UnreadCountUpdated', (count: number) => {
          if (import.meta.env.DEV) {
            console.info('Unread count updated via SignalR:', count);
          }
          // Update the unread count in the cache
          queryClient.setQueryData(QUERY_KEYS.unreadCount, count);
        });

        // Start the connection
        await connection.start();

        if (isMounted) {
          setState({
            isConnected: true,
            isConnecting: false,
            error: null,
          });

          if (import.meta.env.DEV) {
            console.info('SignalR notification hub connected');
          }
        }
      } catch (error) {
        if (isMounted) {
          const errorMessage = error instanceof Error ? error.message : 'Failed to connect';
          setState({
            isConnected: false,
            isConnecting: false,
            error: errorMessage,
          });

          if (import.meta.env.DEV) {
            console.error('SignalR connection error:', error);
          }

          // Attempt to reconnect after delay
          reconnectTimeoutRef.current = setTimeout(() => {
            if (isMounted) {
              connect();
            }
          }, RECONNECT_DELAY_MS);
        }
      }
    }

    connect();

    // Cleanup
    return () => {
      isMounted = false;

      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
      }

      const connection = connectionRef.current as any; // eslint-disable-line @typescript-eslint/no-explicit-any
      if (connection && HubConnectionState) {
        if (connection.state !== HubConnectionState.Disconnected) {
          connection.stop().catch((error: unknown) => {
            if (import.meta.env.DEV) {
              console.error('Error stopping SignalR connection:', error);
            }
          });
        }
      }
    };
  }, [enabled, queryClient]);

  return state;
}
