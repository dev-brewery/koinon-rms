/**
 * Error Tracking Service
 * Integrates Sentry for production error monitoring
 */

import * as Sentry from '@sentry/react';
import { ErrorInfo } from 'react';

interface ErrorTrackingConfig {
  dsn?: string;
  environment?: string;
  enabled?: boolean;
  tracesSampleRate?: number;
}

/**
 * Initialize error tracking service
 */
export function initErrorTracking(config?: ErrorTrackingConfig): void {
  const dsn = config?.dsn || import.meta.env.VITE_SENTRY_DSN;
  const environment = config?.environment || import.meta.env.VITE_SENTRY_ENVIRONMENT || 'production';
  const enabled = config?.enabled ?? (!import.meta.env.DEV && !!dsn);

  if (!enabled || !dsn) {
    console.info('Error tracking disabled (development mode or missing DSN)');
    return;
  }

  Sentry.init({
    dsn,
    environment,
    integrations: [
      Sentry.browserTracingIntegration(),
      Sentry.replayIntegration({
        maskAllText: true,
        blockAllMedia: true,
      }),
    ],
    // Performance Monitoring
    tracesSampleRate: config?.tracesSampleRate ?? 0.1, // 10% of transactions
    // Session Replay
    replaysSessionSampleRate: 0.1, // 10% of sessions
    replaysOnErrorSampleRate: 1.0, // 100% of sessions with errors

    // Filter out non-error noise
    beforeSend(event) {
      // Don't send events in development
      if (import.meta.env.DEV) {
        return null;
      }
      return event;
    },
  });

  console.info(`Error tracking initialized (environment: ${environment})`);
}

/**
 * Capture error with context
 */
export function captureError(
  error: Error,
  errorInfo?: ErrorInfo,
  context?: Record<string, unknown>
): void {
  if (!import.meta.env.DEV && import.meta.env.VITE_SENTRY_DSN) {
    Sentry.withScope((scope) => {
      // Add React error info
      if (errorInfo) {
        scope.setContext('react', {
          componentStack: errorInfo.componentStack,
        });
      }

      // Add additional context
      if (context) {
        Object.entries(context).forEach(([key, value]) => {
          scope.setContext(key, value as Record<string, unknown>);
        });
      }

      Sentry.captureException(error);
    });
  }
}

/**
 * Set user context for error tracking
 */
export function setUserContext(user: {
  id?: string;
  email?: string;
  username?: string;
}): void {
  if (!import.meta.env.DEV && import.meta.env.VITE_SENTRY_DSN) {
    Sentry.setUser(user);
  }
}

/**
 * Clear user context (e.g., on logout)
 */
export function clearUserContext(): void {
  if (!import.meta.env.DEV && import.meta.env.VITE_SENTRY_DSN) {
    Sentry.setUser(null);
  }
}

/**
 * Add breadcrumb for debugging
 */
export function addBreadcrumb(
  message: string,
  category?: string,
  level?: 'debug' | 'info' | 'warning' | 'error'
): void {
  if (!import.meta.env.DEV && import.meta.env.VITE_SENTRY_DSN) {
    Sentry.addBreadcrumb({
      message,
      category: category || 'app',
      level: level || 'info',
      timestamp: Date.now() / 1000,
    });
  }
}

/**
 * Manually capture a message
 */
export function captureMessage(
  message: string,
  level?: 'debug' | 'info' | 'warning' | 'error'
): void {
  if (!import.meta.env.DEV && import.meta.env.VITE_SENTRY_DSN) {
    Sentry.captureMessage(message, level || 'info');
  }
}
