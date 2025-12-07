using System.Runtime.Versioning;
using Koinon.PrintBridge.Helpers;

namespace Koinon.PrintBridge.Services;

/// <summary>
/// Service for sending ZPL (Zebra Programming Language) content to printers.
/// </summary>
[SupportedOSPlatform("windows")]
public class ZplPrintService
{
    private readonly PrinterDiscoveryService _printerDiscoveryService;
    private readonly ILogger<ZplPrintService> _logger;

    public ZplPrintService(
        PrinterDiscoveryService printerDiscoveryService,
        ILogger<ZplPrintService> logger)
    {
        _printerDiscoveryService = printerDiscoveryService;
        _logger = logger;
    }

    /// <summary>
    /// Sends ZPL content to a printer.
    /// </summary>
    /// <param name="printerName">Name of the printer. If null, uses default printer.</param>
    /// <param name="zplContent">ZPL command string.</param>
    /// <param name="copies">Number of copies to print.</param>
    /// <returns>Result of the print operation.</returns>
    public PrintResult SendZplToPrinter(string? printerName, string zplContent, int copies = 1)
    {
        if (string.IsNullOrWhiteSpace(zplContent))
        {
            return new PrintResult
            {
                Success = false,
                Message = "ZPL content cannot be empty"
            };
        }

        if (copies < 1 || copies > 999)
        {
            return new PrintResult
            {
                Success = false,
                Message = "Copies must be between 1 and 999"
            };
        }

        // Use default printer if none specified
        var targetPrinterName = printerName ?? GetDefaultPrinterName();

        if (string.IsNullOrEmpty(targetPrinterName))
        {
            return new PrintResult
            {
                Success = false,
                Message = "No printer specified and no default printer found"
            };
        }

        // Verify printer exists
        var printer = _printerDiscoveryService.GetPrinterByName(targetPrinterName);
        if (printer == null)
        {
            return new PrintResult
            {
                Success = false,
                Message = $"Printer '{targetPrinterName}' not found"
            };
        }

        try
        {
            // Send ZPL content for each copy
            for (int i = 0; i < copies; i++)
            {
                var success = RawPrinterHelper.SendStringToPrinter(targetPrinterName, zplContent);

                if (!success)
                {
                    _logger.LogError("Failed to send ZPL to printer {PrinterName} (copy {Copy})",
                        targetPrinterName, i + 1);
                    return new PrintResult
                    {
                        Success = false,
                        Message = $"Failed to send print job to printer '{targetPrinterName}'"
                    };
                }

                _logger.LogInformation("Successfully sent ZPL to printer {PrinterName} (copy {Copy}/{Copies})",
                    targetPrinterName, i + 1, copies);
            }

            return new PrintResult
            {
                Success = true,
                Message = $"Printed {copies} label{(copies > 1 ? "s" : "")} on {targetPrinterName}",
                PrinterName = targetPrinterName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while printing to {PrinterName}", targetPrinterName);
            return new PrintResult
            {
                Success = false,
                Message = $"Exception: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Sends a test label to a printer.
    /// </summary>
    public PrintResult SendTestLabel(string? printerName)
    {
        const string testLabel = """
            ^XA
            ^FO50,50
            ^A0N,50,50
            ^FDTest Label^FS
            ^FO50,120
            ^A0N,30,30
            ^FDKoinon PrintBridge^FS
            ^XZ
            """;

        return SendZplToPrinter(printerName, testLabel);
    }

    private string? GetDefaultPrinterName()
    {
        return _printerDiscoveryService.GetDefaultPrinter()?.Name;
    }
}

/// <summary>
/// Result of a print operation.
/// </summary>
public record PrintResult
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public string? PrinterName { get; init; }
}
