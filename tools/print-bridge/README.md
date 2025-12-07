# Koinon Print Bridge

A Windows desktop application that provides a local REST API for printing labels to Zebra and Dymo printers from the Koinon check-in kiosk web application.

## Overview

The Print Bridge runs as a Windows system tray application and provides a local web server (localhost:9632) that the kiosk web app can communicate with to print labels. Supports both Zebra thermal printers (via ZPL) and Dymo label printers (via Windows GDI).

## Features

- **Multi-Printer Support** - Works with Zebra thermal printers (ZPL) and Dymo label printers (GDI/image)
- **Auto-Discovery** - Automatically detects and identifies installed label printers
- **System Tray Integration** - Minimal footprint, runs quietly in background
- **Multiple Print Methods**:
  - ZPL (Zebra Programming Language) for Zebra printers
  - Image printing (base64) for all printers including Dymo
  - Text label printing with automatic layout
- **Health Monitoring** - Built-in diagnostics and test print utility
- **Secure API** - Localhost-only access with input validation
- **Security Features** - Validates all print jobs, blocks dangerous firmware commands

## Requirements

- Windows 10 or later
- .NET 8.0 Runtime
- Zebra thermal printer and/or Dymo label printer installed in Windows

## Installation

1. Build the application:
   ```bash
   dotnet build tools/print-bridge/Koinon.PrintBridge/Koinon.PrintBridge.csproj
   ```

2. Run the application:
   ```bash
   dotnet run --project tools/print-bridge/Koinon.PrintBridge/Koinon.PrintBridge.csproj
   ```

3. The application will start in the system tray with a blue "P" icon.

## Usage

### System Tray Menu

Right-click the tray icon to access:
- **Test Print** - Print a test label to verify printer functionality (auto-detects Zebra or Dymo)
- **View Printers** - Show all discovered printers with their capabilities
- **Exit** - Close the application

### API Endpoints

The print bridge exposes the following endpoints on `http://localhost:9632`:

#### GET /health
Check if the print bridge is running.

**Response:**
```json
{
  "status": "healthy",
  "version": "1.0.0",
  "timestamp": "2024-12-06T22:00:00Z"
}
```

#### GET /api/printers
Get all available printers.

**Response:**
```json
{
  "printers": [
    {
      "name": "Zebra ZD420",
      "status": "Ready",
      "isDefault": false,
      "isZebraPrinter": true,
      "isDymoPrinter": false,
      "printerType": "Zebra",
      "supportsZpl": true,
      "supportsImage": true,
      "driverName": "ZDesigner ZD420-203dpi ZPL",
      "portName": "USB001"
    },
    {
      "name": "DYMO LabelWriter 450",
      "status": "Ready",
      "isDefault": false,
      "isZebraPrinter": false,
      "isDymoPrinter": true,
      "printerType": "Dymo",
      "supportsZpl": false,
      "supportsImage": true,
      "driverName": "DYMO LabelWriter 450",
      "portName": "USB002"
    }
  ],
  "count": 2,
  "zebraCount": 1,
  "dymoCount": 1
}
```

#### POST /api/printers/refresh
Force refresh the printer cache.

**Response:**
```json
{
  "message": "Printer cache refreshed",
  "count": 1
}
```

#### POST /api/print/test
Print a test label.

**Request:**
```json
{
  "printerName": "Zebra ZP450"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Test label sent to Zebra ZP450",
  "printerName": "Zebra ZP450"
}
```

#### POST /api/print
Print a single ZPL label (Zebra printers only).

**Request:**
```json
{
  "printerName": "Zebra ZD420",
  "zplContent": "^XA^FO50,50^A0N,50,50^FDTest^FS^XZ"
}
```

**Security Notes:**
- ZPL content is validated for dangerous firmware commands
- Maximum size: 100KB per label
- Must start with `^XA` and end with `^XZ`

**Response:**
```json
{
  "success": true,
  "message": "Label sent to Zebra ZD420",
  "printerName": "Zebra ZD420"
}
```

#### POST /api/print/image
Print a label from a base64-encoded image (all printers).

**Request:**
```json
{
  "printerName": "DYMO LabelWriter 450",
  "base64Image": "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJ...",
  "labelSize": "default"
}
```

**Label Sizes:**
- `default` - 2.25" x 1.25" (standard address label)
- `small` - 1.125" x 3.5" (file folder label)
- `medium` - 2.25" x 1.25" (same as default)
- `large` - 4.0" x 6.0" (shipping label)
- `badge` - 3.0" x 4.0" (name badge)

**Image Requirements:**
- Base64-encoded PNG, JPEG, or other image format
- Maximum size: 5MB

**Response:**
```json
{
  "success": true,
  "message": "Image label sent to DYMO LabelWriter 450",
  "printerName": "DYMO LabelWriter 450"
}
```

#### POST /api/print/text
Print a simple text label (all printers).

**Request:**
```json
{
  "printerName": "DYMO LabelWriter 450",
  "text": "John Doe\nVisitor\n2025-12-07",
  "labelSize": "badge"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Text label sent to DYMO LabelWriter 450",
  "printerName": "DYMO LabelWriter 450"
}
```

#### GET /api/label-sizes
Get available label size presets.

**Response:**
```json
{
  "labelSizes": [
    {
      "name": "default",
      "widthInches": 2.25,
      "heightInches": 1.25
    },
    {
      "name": "badge",
      "widthInches": 3.0,
      "heightInches": 4.0
    }
  ]
}
```

#### POST /api/print/batch
Print multiple ZPL labels in batch.

**Request:**
```json
{
  "printerName": "Zebra ZP450",
  "zplContents": [
    "^XA^FO50,50^A0N,50,50^FDLabel 1^FS^XZ",
    "^XA^FO50,50^A0N,50,50^FDLabel 2^FS^XZ"
  ]
}
```

**Response:**
```json
{
  "success": true,
  "message": "2 labels sent to Zebra ZP450",
  "printerName": "Zebra ZP450",
  "labelCount": 2
}
```

## Printer Detection

The print bridge automatically identifies printer types:

### Zebra Printers
Detected if printer name or driver contains: "zebra" or "zpl"

**Supports:** ZPL commands via raw printer access

### Dymo Printers
Detected if printer name or driver contains: "dymo" or "labelwriter"

**Supports:** Image printing via Windows GDI

### Generic Printers
All other printers support image printing via Windows GDI.

## Architecture

The application consists of five main components:

1. **PrinterDiscoveryService** - Discovers printers using WMI and identifies Zebra/Dymo printers
2. **ZplPrintService** - Handles raw ZPL printing to Zebra thermal printers via Windows API
3. **WindowsPrintService** - Handles GDI-based image/text printing for all printers
4. **DiagnosticsController** - Provides REST API for all printing operations
5. **SystemTrayIcon** - Windows Forms system tray integration

## Development

### Running Tests

**Note:** Tests must be run on Windows as they depend on Windows APIs.

```bash
dotnet test tools/print-bridge/Koinon.PrintBridge.Tests/Koinon.PrintBridge.Tests.csproj
```

### Building for Distribution

```bash
dotnet publish tools/print-bridge/Koinon.PrintBridge/Koinon.PrintBridge.csproj -c Release -r win-x64 --self-contained
```

## Troubleshooting

### No printers found
- Ensure printer is connected, powered on, and installed in Windows
- Verify printer drivers are installed (Settings > Devices > Printers)
- Check printer appears in Windows "Printers & Scanners" and shows "Ready"
- Try "Refresh Printers" from system tray menu

### Print fails
- For Zebra: Verify ZPL content is valid (starts with ^XA, ends with ^XZ)
- Check printer has labels loaded
- View Windows print queue for error messages
- Check printer status in system tray menu
- Try test print from system tray menu
- Check logs in `%LocalAppData%\Koinon\PrintBridge\logs`

### Can't connect from web app
- Ensure Print Bridge is running (check system tray icon)
- Verify web app is accessing `http://localhost:9632`
- Check Windows Firewall is not blocking localhost connections
- Test health endpoint: `http://localhost:9632/api/health`

## Security

The print bridge implements several security measures:

1. **Localhost Only** - API only accepts requests from localhost (127.0.0.1)
2. **CORS Restrictions** - Only allows requests from approved local origins (localhost:5173)
3. **ZPL Validation** - Blocks dangerous firmware commands (^JU, ^JF, ~JR, ^MC, ~MT, ~HS)
4. **Size Limits**:
   - ZPL content: 100KB max per label
   - Image data: 5MB max
   - Batch size: 50 labels max
5. **Input Validation** - All inputs validated and sanitized
6. **No External Access** - No external network access required
7. **No Data Storage** - No sensitive data is stored or transmitted

## Logs

Logs are written to:
```
%LocalAppData%\Koinon\PrintBridge\logs\printbridge-YYYYMMDD.log
```

Logs include:
- Printer discovery events
- Print job submissions
- Errors and warnings
- Security validation failures

Logs are retained for 7 days.

## License

Part of the Koinon RMS project.
