# Koinon PrintBridge Integration Guide

## Overview

Koinon PrintBridge is a Windows-only helper application that bridges the gap between the browser-based kiosk and local Zebra/thermal printers. The kiosk sends print jobs via HTTP to PrintBridge, which then communicates with Windows Print Spooler to send ZPL commands to the printer.

## Architecture

```
Kiosk Frontend (React)
    ↓ POST /api/v1/print/print (JSON with ZPL)
PrintBridge Kestrel Server (http://127.0.0.1:9632)
    ↓ P/Invoke winspool.drv
Windows Print Spooler
    ↓
Zebra Printer (TCP/IP or USB)
```

## Integration Points

### 1. From React Kiosk to PrintBridge

The kiosk sends HTTP POST requests to PrintBridge:

```typescript
// Example from kiosk React component
interface PrintRequest {
  printerName?: string;  // Optional, uses default if not provided
  zplContent: string;    // ZPL command string
  copies?: number;       // Optional, defaults to 1
}

async function printLabel(request: PrintRequest) {
  try {
    const response = await fetch('http://127.0.0.1:9632/api/v1/print/print', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request)
    });

    const result = await response.json();
    if (result.success) {
      console.log(`Printed on ${result.printerName}`);
    } else {
      console.error(`Print failed: ${result.message}`);
    }
  } catch (error) {
    console.error('Failed to connect to PrintBridge:', error);
  }
}
```

### 2. PrintBridge Discovery

Before printing, the kiosk should discover available printers:

```typescript
async function getAvailablePrinters() {
  const response = await fetch('http://127.0.0.1:9632/api/v1/print/printers');
  const data = await response.json();
  return data.printers;
  // Returns: [
  //   { name: "Zebra ZD420", type: "ZPL", status: "Ready", isDefault: true },
  //   { name: "Dymo Label", type: "EPL", status: "Ready", isDefault: false }
  // ]
}
```

### 3. Health Checks

The kiosk can check if PrintBridge is running:

```typescript
async function checkPrintBridgeHealth() {
  try {
    const response = await fetch('http://127.0.0.1:9632/api/v1/print/health');
    const health = await response.json();
    console.log(`PrintBridge v${health.version} - ${health.status}`);
    console.log(`Default printer: ${health.defaultPrinter}`);
    return health.status === 'healthy';
  } catch {
    console.error('PrintBridge is not available');
    return false;
  }
}
```

## Deployment Architecture

### Development (Single Machine)

```
┌─ Windows Machine ────────────────────────────────┐
│ ┌─ PrintBridge.exe (Port 9632) ─────────────┐   │
│ │ - Enumerates local printers                │   │
│ │ - Sends ZPL to print spooler              │   │
│ │ - Listens only on 127.0.0.1               │   │
│ └────────────────────────────────────────────┘   │
│           ↑                                      │
│           │ HTTP POST (localhost:9632)          │
│ ┌─ Browser (http://localhost:5173) ───────────┐ │
│ │ Koinon Kiosk UI (React)                    │ │
│ │ - Initiates check-in                       │ │
│ │ - Requests label printing                  │ │
│ └────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────┘
```

### Production (Kiosk Machine)

```
┌─ Kiosk Computer (Windows 10+) ───────────────────────┐
│                                                      │
│ ┌─ PrintBridge.exe (AutoStart via Task Scheduler) ─┐ │
│ │ - Listening on http://127.0.0.1:9632            │ │
│ │ - Persists through kiosk app restarts           │ │
│ └──────────────────────────────────────────────────┘ │
│           ↑                                         │
│           │ HTTP (localhost only)                  │
│ ┌─ Kiosk Browser (Chromium/Edge) ───────────────┐  │
│ │ http://your-church.com/kiosk                  │  │
│ │ - Loads from your Koinon RMS server           │  │
│ │ - Calls PrintBridge on localhost:9632         │  │
│ └──────────────────────────────────────────────────┘ │
│           ↑                                         │
│   Internet Connection (WiFi/LAN)                   │
│           ↓                                         │
│  Your Koinon RMS API Server (Remote)              │
│  - Provides check-in UI                            │
│  - Provides family/person data                     │
│  - Stores attendance records                       │
│                                                     │
└─────────────────────────────────────────────────────┘
```

## Setting Up PrintBridge on a Kiosk

### Prerequisites

- Windows 10 or later
- .NET 8 Runtime
- Printer driver installed (e.g., Zebra ZD420 driver)
- Printer accessible via Windows Print Spooler

### Installation Steps

1. **Copy Application**
   ```powershell
   # Publish as self-contained executable
   dotnet publish -c Release -r win-x64 --self-contained -o C:\KoinonPrintBridge
   ```

2. **Create Windows Service (Optional)**
   Using NSSM (Non-Sucking Service Manager):
   ```powershell
   nssm install KoinonPrintBridge C:\KoinonPrintBridge\Koinon.PrintBridge.exe
   nssm set KoinonPrintBridge AppDirectory C:\KoinonPrintBridge
   nssm set KoinonPrintBridge AppNoConsole 1
   nssm start KoinonPrintBridge
   ```

3. **Or: Use Task Scheduler**
   - Create a task to run at system startup
   - Command: `C:\KoinonPrintBridge\Koinon.PrintBridge.exe`
   - Run with highest privileges

4. **Configure Default Printer**
   - In Windows Settings → Printers & Scanners
   - Set your Zebra printer as default (optional but recommended)

5. **Verify Installation**
   ```bash
   # Health check from command line
   curl http://127.0.0.1:9632/api/v1/print/health
   ```

## Error Handling in the Kiosk

The kiosk should handle PrintBridge unavailability gracefully:

```typescript
interface PrintResult {
  success: boolean;
  message: string;
}

async function printLabelWithRetry(
  zpl: string,
  maxRetries: number = 3
): Promise<PrintResult> {
  for (let i = 0; i < maxRetries; i++) {
    try {
      const response = await fetch(
        'http://127.0.0.1:9632/api/v1/print/print',
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ zplContent: zpl })
        }
      );

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.warn(`Print attempt ${i + 1} failed:`, error);

      if (i < maxRetries - 1) {
        // Wait 500ms before retry
        await new Promise(resolve => setTimeout(resolve, 500));
      }
    }
  }

  return {
    success: false,
    message: 'PrintBridge unavailable after 3 attempts'
  };
}
```

## Development Workflow

### Testing PrintBridge Locally

1. **Start PrintBridge in Debug Mode**
   ```bash
   cd tools/print-bridge/Koinon.PrintBridge
   ASPNETCORE_ENVIRONMENT=Development dotnet run
   ```
   Output: `Starting on http://127.0.0.1:9632`

2. **Test from Command Line**
   ```bash
   # List printers
   curl http://127.0.0.1:9632/api/v1/print/printers | jq

   # Get health
   curl http://127.0.0.1:9632/api/v1/print/health | jq

   # Print test label
   curl -X POST http://127.0.0.1:9632/api/v1/print/test \
     -H "Content-Type: application/json"
   ```

3. **Test from React Kiosk**
   - The kiosk (http://localhost:5173) can call PrintBridge via CORS
   - CORS is pre-configured for localhost:5173

### Debugging

Enable debug logging:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run \
  --project tools/print-bridge/Koinon.PrintBridge/Koinon.PrintBridge.csproj
```

Look for output like:
```
[dbg] Discovered printer: Zebra ZD420 (ZPL)
[inf] Successfully sent ZPL to printer Zebra ZD420
```

## Printer-Specific Configuration

### Zebra ZD420

ZPL commands should follow standard Zebra format:

```zpl
^XA                    # Start format
^FO50,50               # Field origin (x,y)
^A0N,50,50             # Font A, normal, height 50
^FDHello World^FS      # Field data
^XZ                    # End format
```

### Dymo Label Printer

For Dymo printers, use EPL format instead of ZPL. PrintBridge detects printer type from name but sending EPL to a ZPL printer will fail with an error message.

## Performance Considerations

- **Local-only communication**: Sub-10ms latency (no network overhead)
- **Print spooler queuing**: Multi-label batches processed in order
- **No persistence**: Each application restart clears the queue
- **Async design**: Non-blocking HTTP endpoints

## Security

- **Localhost binding only**: Prevents network access from other machines
- **No authentication**: Assumes trusted kiosk network
- **CORS restricted**: Only allows http://localhost:5173
- **Input validation**: Printer names and ZPL content validated before processing
- **No file I/O**: Only communicates with printers and Windows APIs

## Troubleshooting

### "Printer not found" Error
- Verify printer name in Windows Print Spooler: `Get-Printer` (PowerShell)
- Check printer is not paused: Settings → Printers & Scanners
- Restart Print Spooler: `Restart-Service spooler` (PowerShell admin)

### PrintBridge Not Responding
- Check if process is running: `Get-Process Koinon.PrintBridge`
- Check for port conflicts: `netstat -ano | findstr 9632`
- Review Windows Event Viewer for errors
- Restart the application

### Print Jobs Queued but Not Printing
- Check printer status in Windows
- Verify printer driver is installed
- Try printing from Notepad to test printer directly
- Check printer firmware compatibility with ZPL

## Future Enhancements

- System tray UI with printer status monitoring
- Printer-specific configuration (DPI, darkness, etc.)
- Print job history and logging
- Support for additional printer types (Dymo, Brother)
- Cross-platform support (Linux/macOS via CUPS)
