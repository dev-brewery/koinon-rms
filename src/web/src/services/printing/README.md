# Printing Service

TypeScript/React client for communicating with the Koinon PrintBridge application.

## Overview

This service provides a clean TypeScript interface to the PrintBridge Windows application, which handles printing to local Zebra thermal printers.

**Architecture:**
```
React Kiosk (localhost:5173)
    ↓ HTTP POST /api/v1/print/*
PrintBridge (http://127.0.0.1:9632)
    ↓ P/Invoke winspool.drv
Windows Print Spooler
    ↓
Zebra Printer
```

## Usage

### Basic Setup

```typescript
import { getPrintBridgeClient } from '@/services/printing';

// Get the singleton client instance
const printBridge = getPrintBridgeClient();
```

### Check if PrintBridge is Available

```typescript
// Check if the local PrintBridge application is running
const isAvailable = await printBridge.isAvailable();

if (!isAvailable) {
  console.warn('PrintBridge is not running on this kiosk');
  // Handle offline scenario - offer to email labels or skip printing
}
```

### Get Available Printers

```typescript
try {
  const printers = await printBridge.getPrinters();
  console.log('Available printers:', printers);
  // [
  //   { name: "Zebra ZD420", type: "ZPL", status: "Ready", isDefault: true },
  //   { name: "Dymo Label", type: "EPL", status: "Ready", isDefault: false }
  // ]
} catch (error) {
  console.error('Failed to get printers:', error.message);
}
```

### Print a Label

```typescript
import type { PrintRequest } from '@/services/printing';

async function printCheckInLabel(zplContent: string) {
  const printBridge = getPrintBridgeClient();

  try {
    const result = await printBridge.print({
      printerName: 'Zebra ZD420',  // Optional - uses default if not specified
      zplContent,
      copies: 1
    });

    if (result.success) {
      console.log(`Label printed on ${result.printerName}`);
    } else {
      console.error(`Print failed: ${result.message}`);
    }
  } catch (error) {
    console.error('Print request failed:', error.message);
    // Handle error - show user message about printing failure
  }
}
```

### Integration with Label Generation Service

The typical workflow is:

1. User checks in a person/family
2. Backend generates ZPL labels (via LabelGenerationService)
3. Frontend fetches the labels from `/api/v1/checkin/labels/{attendanceIdKey}`
4. Frontend sends ZPL to PrintBridge for printing

```typescript
import { getLabels } from '@/services/api/checkin';
import { getPrintBridgeClient } from '@/services/printing';

async function checkInAndPrint(attendanceIdKey: string) {
  // 1. Get labels from API
  const labels = await getLabels(attendanceIdKey);

  // 2. Print each label
  const printBridge = getPrintBridgeClient();

  for (const label of labels) {
    try {
      const result = await printBridge.print({
        zplContent: label.content,
        copies: 1
      });

      if (!result.success) {
        console.error(`Failed to print ${label.type}: ${result.message}`);
      }
    } catch (error) {
      console.error(`Print error: ${error.message}`);
    }
  }
}
```

### Testing Printers

```typescript
async function testPrinter(printerName?: string) {
  const printBridge = getPrintBridgeClient();

  try {
    const result = await printBridge.testPrint(printerName);
    console.log(result.message);
  } catch (error) {
    console.error(`Test failed: ${error.message}`);
  }
}
```

## Error Handling

The client throws `PrintBridgeError` exceptions with specific error codes:

```typescript
import { getPrintBridgeClient, PrintBridgeError } from '@/services/printing';

async function safelyPrint(zpl: string) {
  const printBridge = getPrintBridgeClient();

  try {
    return await printBridge.print({ zplContent: zpl });
  } catch (error) {
    if (error instanceof PrintBridgeError) {
      switch (error.code) {
        case 'NOT_AVAILABLE':
          // PrintBridge application is not running
          console.warn('PrintBridge not running');
          break;
        case 'TIMEOUT':
          // Request took too long
          console.warn('Print request timed out');
          break;
        case 'NETWORK_ERROR':
          // Network connectivity issue
          console.error('Network error:', error.message);
          break;
        case 'INVALID_ZPL':
        case 'INVALID_COPIES':
          // Validation error
          console.error('Invalid request:', error.message);
          break;
        case 'PRINT_FAILED':
          // Printer error (not found, offline, etc.)
          console.error('Print failed:', error.message);
          break;
        default:
          console.error('Unknown error:', error.message);
      }
    }
  }
}
```

### Error Codes

- `NOT_AVAILABLE` - PrintBridge application is not running
- `TIMEOUT` - Request exceeded timeout threshold
- `NETWORK_ERROR` - Network connectivity issue
- `INVALID_ZPL` - ZPL content is empty
- `INVALID_COPIES` - Copies value out of range (1-999)
- `PRINT_FAILED` - Printer error or not found
- `GET_PRINTERS_FAILED` - Failed to enumerate printers
- `HEALTH_CHECK_FAILED` - Health check failed
- `TEST_PRINT_FAILED` - Test print failed

## Configuration

The client can be configured with custom settings:

```typescript
import { getPrintBridgeClient } from '@/services/printing';

const client = getPrintBridgeClient({
  baseUrl: 'http://127.0.0.1:9632',  // Change if PrintBridge runs on different port
  timeout: 5000,                     // Request timeout in milliseconds
});
```

### Environment Variables

Configure PrintBridge connection via environment variables:

```bash
# .env or .env.local
VITE_PRINTBRIDGE_URL=http://127.0.0.1:9632
VITE_PRINTBRIDGE_TIMEOUT=5000
```

## Performance Considerations

- **Health Check Caching**: The `isAvailable()` method caches results for 5 seconds to reduce unnecessary network calls
- **Request Timeout**: Defaults to 5 seconds to avoid hanging the UI
- **No Queuing**: PrintBridge doesn't persist jobs - each print request is independent

## Offline Handling

When PrintBridge is unavailable (application not running or network issue):

```typescript
const printBridge = getPrintBridgeClient();

try {
  const isAvailable = await printBridge.isAvailable();

  if (isAvailable) {
    await printBridge.print({ zplContent: zpl });
  } else {
    // Offer alternatives:
    // 1. Skip printing
    // 2. Email label as PDF
    // 3. Show on-screen receipt instead
    console.warn('Printing unavailable - please check kiosk configuration');
  }
} catch (error) {
  // Handle error gracefully
  console.error('Print error:', error);
}
```

## Testing

```typescript
import { resetPrintBridgeClient } from '@/services/printing';

// In tests, reset the singleton between tests
afterEach(() => {
  resetPrintBridgeClient();
});

// Mock fetch for unit tests
global.fetch = jest.fn();
```

## PrintBridge Application

For information about running and configuring the PrintBridge application, see:
- `/tools/print-bridge/README.md` - Overview and features
- `/tools/print-bridge/INTEGRATION.md` - Integration guide and deployment

## Architecture

- **Platform**: Windows only (uses WinSpool.drv P/Invoke)
- **Port**: 9632 (configurable via appsettings.json)
- **Authentication**: None (localhost-only, trusted environment)
- **Endpoints**: See `/tools/print-bridge/README.md`

## Related Files

- `src/Koinon.Application/Services/LabelGenerationService.cs` - Generates ZPL content
- `tools/print-bridge/Koinon.PrintBridge/Program.cs` - Print service application
- `src/web/src/services/api/checkin.ts` - Check-in API client
