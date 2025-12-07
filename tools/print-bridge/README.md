# Koinon Print Bridge

A lightweight Windows desktop application that enables local label printing for the Koinon RMS kiosk system.

## Overview

The Print Bridge runs as a Windows system tray application and provides a local web server (localhost:9632) that the kiosk web app can communicate with to print ZPL labels on Zebra thermal printers.

## Features

- **Automatic printer discovery** - Finds and identifies Zebra thermal printers
- **System tray integration** - Minimal footprint, runs in background
- **ZPL printing** - Direct support for Zebra Programming Language
- **Health monitoring** - Built-in diagnostics and test print utility
- **CORS-enabled API** - Secure localhost-only communication

## Requirements

- Windows 10 or later
- .NET 8.0 Runtime
- Zebra thermal printer with ZPL driver installed

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
- **Test Print** - Print a test label to verify printer functionality
- **View Printers** - Show all discovered printers
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
      "name": "Zebra ZP450",
      "status": "Ready",
      "isDefault": true,
      "isZebraPrinter": true,
      "driverName": "ZDesigner ZP 450-203dpi ZPL",
      "portName": "USB001"
    }
  ],
  "count": 1,
  "zebraCount": 1
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
Print a single ZPL label.

**Request:**
```json
{
  "printerName": "Zebra ZP450",
  "zplContent": "^XA^FO50,50^A0N,50,50^FDTest^FS^XZ"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Label sent to Zebra ZP450",
  "printerName": "Zebra ZP450"
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

## Architecture

The application consists of three main components:

1. **PrinterDiscoveryService** - Discovers printers and identifies Zebra thermal printers
2. **ZplPrintService** - Handles raw ZPL printing to thermal printers
3. **DiagnosticsController** - Provides REST API for health checks and printing operations

## Development

### Running Tests

```bash
dotnet test tools/print-bridge/Koinon.PrintBridge.Tests/Koinon.PrintBridge.Tests.csproj
```

### Building for Distribution

```bash
dotnet publish tools/print-bridge/Koinon.PrintBridge/Koinon.PrintBridge.csproj -c Release -r win-x64 --self-contained
```

## Troubleshooting

### No printers found
- Ensure Zebra printer is connected and turned on
- Verify printer drivers are installed in Windows
- Check printer appears in Windows "Printers & Scanners"

### Print fails
- Verify ZPL content is valid (starts with ^XA, ends with ^XZ)
- Check printer status in system tray menu
- Try test print from system tray menu

### Can't connect from web app
- Ensure Print Bridge is running (check system tray)
- Verify web app is accessing `http://localhost:9632`
- Check Windows Firewall is not blocking localhost connections

## Security

- Print Bridge only listens on localhost (127.0.0.1)
- CORS is configured to only allow requests from local web app
- No external network access required
- No sensitive data is stored or transmitted

## License

Part of the Koinon RMS project.
