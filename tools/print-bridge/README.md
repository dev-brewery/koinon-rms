# Koinon PrintBridge

A lightweight .NET 8 Windows desktop application that acts as a bridge between the kiosk browser and local Zebra/thermal printers.

## Architecture

PrintBridge runs a local HTTP web server (Kestrel) on port 9632 that the kiosk browser can communicate with to send ZPL (Zebra Programming Language) print jobs to connected printers.

```
Kiosk Browser (http://localhost:5173)
        |
        | HTTP POST /api/v1/print/print
        v
PrintBridge (http://127.0.0.1:9632)
        |
        | winspool.drv P/Invoke
        v
   Windows Printer Queue
        |
        v
  Zebra ZD420 / Dymo Printer
```

## Requirements

- Windows 10 or later
- .NET 8 runtime
- Network connectivity to local printers via Windows Print Spooler

## Features

- Enumerate installed Windows printers
- Detect printer types (Zebra/ZPL, Dymo/EPL, etc.)
- Send raw ZPL commands to printers
- Health check endpoint for monitoring
- Test label printing
- CORS support for kiosk origins
- Comprehensive logging

## API Endpoints

All endpoints are prefixed with `/api/v1/print`.

### POST /print
Send ZPL content to a printer.

**Request:**
```json
{
  "printerName": "Zebra ZD420",
  "zplContent": "^XA^FO50,50^A0N,50,50^FDTest^FS^XZ",
  "copies": 1
}
```

**Response (Success):**
```json
{
  "success": true,
  "message": "Printed 1 label on Zebra ZD420",
  "printerName": "Zebra ZD420"
}
```

### GET /printers
List all available printers on the system.

**Response:**
```json
{
  "printers": [
    {
      "name": "Zebra ZD420",
      "type": "ZPL",
      "status": "Ready",
      "isDefault": true
    }
  ]
}
```

### GET /health
Check the health status of PrintBridge.

**Response:**
```json
{
  "status": "healthy",
  "version": "1.0.0",
  "defaultPrinter": "Zebra ZD420",
  "timestamp": "2024-12-06T15:30:00.000Z"
}
```

### POST /test
Print a test label to verify printer connectivity.

**Request:**
```json
{
  "printerName": "Zebra ZD420"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Printed 1 label on Zebra ZD420",
  "printerName": "Zebra ZD420"
}
```

## Configuration

Configuration is managed via `appsettings.json` and environment-specific overrides.

**appsettings.json:**
```json
{
  "PrintBridge": {
    "Port": 9632,
    "DefaultPrinterName": null,
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://127.0.0.1:5173"
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

## Development

### Build
```bash
dotnet build tools/print-bridge/Koinon.PrintBridge/Koinon.PrintBridge.csproj
```

### Run
```bash
dotnet run --project tools/print-bridge/Koinon.PrintBridge/Koinon.PrintBridge.csproj
```

### Run with Debug Logging
```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project tools/print-bridge/Koinon.PrintBridge/Koinon.PrintBridge.csproj
```

## Production Deployment

For production kiosks:

1. Build as Release:
   ```bash
   dotnet publish -c Release -o ./dist tools/print-bridge/Koinon.PrintBridge/Koinon.PrintBridge.csproj
   ```

2. Run the published executable from `./dist/Koinon.PrintBridge.exe`

3. Consider creating a Windows Service or Task Scheduler entry for automatic startup

## Security Considerations

- **Local-only listening**: PrintBridge listens only on 127.0.0.1, preventing network exposure
- **CORS restrictions**: Only allows requests from kiosk origins
- **No authentication**: Assumes trusted kiosk network (on-site use only)
- **Input validation**: Validates printer names and ZPL content before sending

## Troubleshooting

### "Printer not found" error
- Verify the printer name matches the Windows Print Spooler name
- Use the `/printers` endpoint to list available printers
- Check Windows Print Spooler service is running: `Get-Service spooler`

### Print job queued but not printing
- Check printer firmware is compatible with ZPL
- Verify network connectivity to the printer
- Check printer status in Windows Settings > Printers & Scanners

### Port 9632 already in use
- Change the port in `appsettings.json`
- Or use `netstat -ano | findstr 9632` to find and terminate the conflicting process

## Logging

Logs are output to console. Use environment variable to control logging level:

```bash
# Verbose logging for debugging
ASPNETCORE_ENVIRONMENT=Development dotnet run --project tools/print-bridge/Koinon.PrintBridge/Koinon.PrintBridge.csproj
```

## Platform Support

This application **requires Windows**. The use of Windows-specific APIs (winspool.drv, System.Printing) makes it Windows-only.

Future versions could support:
- CUPS on Linux/macOS
- USB direct printer communication
- Network printer protocols (LPR, IPP)

## License

See root repository LICENSE.
