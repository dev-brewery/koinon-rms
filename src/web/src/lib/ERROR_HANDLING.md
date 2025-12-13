# Error Handling System

This document describes the centralized error handling system in Koinon RMS.

## Overview

The error handling system provides:
- User-friendly error messages mapped from HTTP status codes
- Toast notifications for global error display
- Consistent error logging for debugging
- Type-safe error handling utilities

## Architecture

```
API Error → getErrorMessage() → User-Friendly Message → Toast Notification
                ↓
           logError() → Console (Debug)
```

## Components

### 1. Error Messages (`errorMessages.ts`)

Maps errors to user-friendly messages:

```typescript
import { getErrorMessage, logError } from '@/lib/errorMessages';

try {
  await apiCall();
} catch (error) {
  const userError = getErrorMessage(error);
  // userError = { title, message, variant }

  logError(error, 'Context');
}
```

### 2. Toast Context (`ToastContext.tsx`)

Global notification system:

```typescript
import { useToast } from '@/contexts/ToastContext';

function MyComponent() {
  const toast = useToast();

  toast.success('Success', 'Operation completed');
  toast.error('Error', 'Operation failed');
  toast.warning('Warning', 'Please be careful');
  toast.info('Info', 'Here is some information');
}
```

### 3. Error Handler Hook (`useErrorHandler.ts`)

Combines error mapping and toast notifications:

```typescript
import { useErrorHandler } from '@/hooks/useErrorHandler';

function MyComponent() {
  const { handleError } = useErrorHandler();

  try {
    await apiCall();
  } catch (error) {
    handleError(error, 'My Operation');
    // Automatically logs and shows toast
  }
}
```

## Error Message Mapping

| HTTP Status | User Message | Variant |
|-------------|--------------|---------|
| 400 | Validation error or invalid request | error |
| 401 | Your session has expired. Please sign in again. | warning |
| 403 | You do not have permission to perform this action. | error |
| 404 | The requested resource was not found. | error |
| 408 | The request took too long. Please try again. | error |
| 409 | Conflict with existing data | error |
| 429 | You are making requests too quickly. | warning |
| 500+ | A server error occurred. Please try again later. | error |
| Network Error | Network connection lost. Please check your internet connection. | error |

## Usage Examples

### Basic Usage with useErrorHandler

```typescript
import { useErrorHandler } from '@/hooks/useErrorHandler';

function CreatePersonForm() {
  const { handleError } = useErrorHandler();
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (data) => {
    setIsLoading(true);
    try {
      await createPerson(data);
      toast.success('Success', 'Person created successfully');
    } catch (error) {
      handleError(error, 'Create Person');
      // Toast automatically shown, error logged
    } finally {
      setIsLoading(false);
    }
  };

  return <form onSubmit={handleSubmit}>...</form>;
}
```

### Custom Error Handling

```typescript
import { getErrorMessage, logError } from '@/lib/errorMessages';
import { useToast } from '@/contexts/ToastContext';

function CustomErrorHandler() {
  const toast = useToast();

  try {
    await apiCall();
  } catch (error) {
    const userError = getErrorMessage(error);
    logError(error, 'Custom Context');

    // Custom logic based on error type
    if (userError.variant === 'warning') {
      // Handle warnings differently
      console.warn('Warning occurred:', userError.message);
    }

    toast[userError.variant](userError.title, userError.message);
  }
}
```

### Validation Errors (400)

When the API returns validation details:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "details": {
      "email": ["Email is required", "Email must be valid"],
      "password": ["Password must be at least 8 characters"]
    }
  }
}
```

The error handler shows the first validation error:

```
Title: Validation Error
Message: Email is required
```

### Network Errors

Network errors (e.g., offline, DNS failure) are detected automatically:

```typescript
// API client detects TypeError with "Failed to fetch"
// Converted to:
{
  title: 'Network Error',
  message: 'Network connection lost. Please check your internet connection and try again.',
  variant: 'error'
}
```

## Toast Notification Features

- **Auto-dismiss**: Toasts automatically disappear after a duration (configurable)
  - Success: 5 seconds
  - Error: 7 seconds
  - Warning: 6 seconds
  - Info: 5 seconds
- **Manual dismiss**: Click X button to dismiss immediately
- **Multiple toasts**: Stack vertically in bottom-right corner
- **Accessibility**: Proper ARIA attributes and focus management
- **Responsive**: Works on mobile and desktop

## API Client Integration

The API client (`client.ts`) automatically converts errors:

1. **Network errors** (TypeError) → `NETWORK_ERROR` (status 0)
2. **Timeouts** (AbortError) → `REQUEST_TIMEOUT` (status 408)
3. **HTTP errors** → Parsed from response JSON
4. **Unknown errors** → `UNKNOWN_ERROR` (status 0)

All errors are thrown as `ApiClientError` for consistent handling.

## Best Practices

1. **Use useErrorHandler for most cases**: It handles logging and toast notifications automatically
2. **Provide context**: Always pass a context string to help with debugging
3. **Don't show duplicate errors**: If you show an inline error, consider skipping the toast
4. **Handle sensitive data**: Error logging sanitizes sensitive information
5. **Test error states**: Use the test utilities to verify error handling

## Testing

### Unit Tests

```typescript
import { getErrorMessage } from '@/lib/errorMessages';
import { ApiClientError } from '@/services/api/client';

it('should map 401 to user-friendly message', () => {
  const error = new ApiClientError(401, {
    code: 'UNAUTHORIZED',
    message: 'Invalid credentials',
  });

  const result = getErrorMessage(error);

  expect(result.title).toBe('Authentication Required');
  expect(result.variant).toBe('warning');
});
```

### Integration Tests

```typescript
import { render, screen } from '@testing-library/react';
import { ToastProvider } from '@/contexts/ToastContext';

it('should show error toast on API failure', async () => {
  render(
    <ToastProvider>
      <MyComponent />
    </ToastProvider>
  );

  // Trigger error
  screen.getByText('Submit').click();

  // Wait for error toast
  await screen.findByText('Network Error');
});
```

## Future Enhancements

- [ ] Retry logic for transient failures (429, 503)
- [ ] Error boundary integration for React errors
- [ ] Sentry/error tracking service integration
- [ ] Offline queue for failed requests
- [ ] User-configurable toast preferences
