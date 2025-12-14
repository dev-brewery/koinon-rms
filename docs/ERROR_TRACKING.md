# Error Tracking with Sentry

This document describes the error tracking setup for Koinon RMS using Sentry.

## Overview

Sentry is integrated for production error monitoring, providing:
- Automatic error capture from React ErrorBoundary
- Performance monitoring (10% sample rate)
- Session replay for debugging (10% of sessions, 100% of errors)
- User context tracking
- Breadcrumb logging

## Configuration

### Environment Variables

Add the following to your `.env` file (production only):

```bash
VITE_SENTRY_DSN=https://your-sentry-dsn@sentry.io/project-id
VITE_SENTRY_ENVIRONMENT=production  # or staging, development, etc.
```

**Important:** Sentry is automatically disabled in development mode and when `VITE_SENTRY_DSN` is not set.

### Sentry Project Setup

1. Create a Sentry account at https://sentry.io
2. Create a new React project
3. Copy your DSN from Project Settings → Client Keys (DSN)
4. Add the DSN to your environment variables

## Usage

### Automatic Error Capture

Errors thrown in React components are automatically captured by the ErrorBoundary:

```tsx
// Automatically captured
throw new Error('Something went wrong');
```

### Manual Error Capture

```typescript
import { captureError, captureMessage } from '@/services/errorTracking';

try {
  // risky operation
} catch (error) {
  captureError(error as Error, undefined, {
    context: 'payment-processing',
    userId: user.id,
  });
}

// Log a message
captureMessage('User completed checkout', 'info');
```

### User Context

Track which user experienced an error:

```typescript
import { setUserContext, clearUserContext } from '@/services/errorTracking';

// On login
setUserContext({
  id: user.idKey,
  email: user.email,
  username: user.username,
});

// On logout
clearUserContext();
```

### Breadcrumbs

Add debugging context before errors occur:

```typescript
import { addBreadcrumb } from '@/services/errorTracking';

addBreadcrumb('User clicked checkout button', 'navigation');
addBreadcrumb('Payment API called', 'api', 'info');
addBreadcrumb('Payment failed', 'api', 'error');
```

## Features

### Performance Monitoring

Tracks page load times and API request performance at a 10% sample rate.

### Session Replay

- Records 10% of normal sessions
- Records 100% of sessions with errors
- All text and media are masked for privacy

### Privacy

The integration is configured with privacy in mind:
- Text masking enabled
- Media blocking enabled
- Only active in production
- No PII in error reports (unless explicitly set)

## Development vs Production

| Feature | Development | Production |
|---------|-------------|------------|
| Error Tracking | Console only | Sentry |
| Performance Monitoring | Disabled | Enabled (10%) |
| Session Replay | Disabled | Enabled (10%/100%) |
| User Context | Not tracked | Tracked |

## Troubleshooting

### Errors not appearing in Sentry

1. Check `VITE_SENTRY_DSN` is set
2. Verify not in development mode (`import.meta.env.DEV === false`)
3. Check browser console for initialization message
4. Verify Sentry project is active

### Too many events

Adjust sample rates in `src/services/errorTracking.ts`:

```typescript
tracesSampleRate: 0.1, // Lower for less performance events
replaysSessionSampleRate: 0.05, // Lower for fewer session replays
```

## Cost Management

Sentry pricing is based on:
- Number of errors
- Number of transactions (performance)
- Number of replays

Current configuration targets:
- 10% of transactions
- 10% of normal sessions
- 100% of error sessions

For a typical deployment, this should fit within the free tier (5K errors/month, 10K transactions/month).
