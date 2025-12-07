# Building Koinon Print Bridge

## Platform Requirements

The Koinon Print Bridge is a **Windows-only** application that requires:
- Windows 10 or later
- .NET 8.0 SDK
- Visual Studio 2022 (recommended) or Visual Studio Code with C# extension

## Building on Windows

```bash
# Build the application
dotnet build tools/print-bridge/Koinon.PrintBridge/Koinon.PrintBridge.csproj

# Run the application
dotnet run --project tools/print-bridge/Koinon.PrintBridge/Koinon.PrintBridge.csproj

# Run tests
dotnet test tools/print-bridge/Koinon.PrintBridge.Tests/Koinon.PrintBridge.Tests.csproj

# Publish for distribution
dotnet publish tools/print-bridge/Koinon.PrintBridge/Koinon.PrintBridge.csproj -c Release -r win-x64 --self-contained
```

## Building on Linux/macOS

**This project cannot be built on Linux or macOS** because it uses:
- Windows Forms for system tray integration
- Windows Printing APIs (winspool.dll)
- Windows Management Instrumentation (WMI)

If you're developing on Linux/macOS, you can:
1. Use a Windows VM or remote Windows machine for building
2. Set up a CI/CD pipeline that builds on Windows runners
3. Skip building the print bridge and focus on other components

## Why Windows-Only?

The print bridge requires direct access to Windows printer drivers and system APIs:
- `System.Drawing.Printing` - Windows printing subsystem
- `System.Management` - WMI for printer discovery
- `winspool.dll` - Raw printer communication
- `System.Windows.Forms` - System tray integration

These APIs are not available on Linux/macOS.

## Cross-Platform Alternatives

If cross-platform printing is needed in the future, consider:
- CUPS (Common Unix Printing System) for Linux/macOS
- IPP (Internet Printing Protocol) for network printers
- Web-based print services with platform-specific agents
