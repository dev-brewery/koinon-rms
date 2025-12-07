using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text;

namespace Koinon.PrintBridge;

/// <summary>
/// Service for printing ZPL (Zebra Programming Language) labels to thermal printers.
/// Handles raw printer communication for direct ZPL output.
/// </summary>
public class ZplPrintService
{
    private readonly ILogger<ZplPrintService> _logger;

    public ZplPrintService(ILogger<ZplPrintService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Prints ZPL content to a specified printer.
    /// </summary>
    /// <param name="printerName">Name of the printer (must be installed on the system)</param>
    /// <param name="zplContent">ZPL content to print</param>
    public async Task PrintZplAsync(string printerName, string zplContent)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            throw new ArgumentException("Printer name cannot be empty", nameof(printerName));
        }

        if (string.IsNullOrWhiteSpace(zplContent))
        {
            throw new ArgumentException("ZPL content cannot be empty", nameof(zplContent));
        }

        _logger.LogInformation("Printing ZPL to printer: {PrinterName}", printerName);
        _logger.LogDebug("ZPL Content:\n{ZplContent}", zplContent);

        await Task.Run(() =>
        {
            try
            {
                // Send raw ZPL data directly to printer
                if (!RawPrinterHelper.SendStringToPrinter(printerName, zplContent))
                {
                    throw new InvalidOperationException($"Failed to send ZPL to printer '{printerName}'");
                }

                _logger.LogInformation("Successfully sent ZPL to printer: {PrinterName}", printerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to print ZPL to printer: {PrinterName}", printerName);
                throw;
            }
        });
    }

    /// <summary>
    /// Prints multiple ZPL labels to a printer in batch.
    /// </summary>
    public async Task PrintBatchZplAsync(string printerName, IEnumerable<string> zplContents)
    {
        _logger.LogInformation("Printing {Count} ZPL labels to printer: {PrinterName}",
            zplContents.Count(), printerName);

        foreach (var zpl in zplContents)
        {
            await PrintZplAsync(printerName, zpl);

            // Small delay between labels to prevent overwhelming the printer
            await Task.Delay(100);
        }

        _logger.LogInformation("Batch print complete: {Count} labels sent to {PrinterName}",
            zplContents.Count(), printerName);
    }

    /// <summary>
    /// Validates ZPL content for basic syntax errors.
    /// </summary>
    public bool ValidateZpl(string zplContent)
    {
        if (string.IsNullOrWhiteSpace(zplContent))
        {
            return false;
        }

        // Basic ZPL validation - must start with ^XA and end with ^XZ
        var trimmed = zplContent.Trim();
        return trimmed.StartsWith("^XA", StringComparison.OrdinalIgnoreCase) &&
               trimmed.EndsWith("^XZ", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates ZPL content with security checks to prevent dangerous firmware commands.
    /// </summary>
    /// <param name="zplContent">ZPL content to validate</param>
    /// <returns>Tuple indicating if content is valid and error message if invalid</returns>
    public (bool IsValid, string? ErrorMessage) ValidateZplWithSecurity(string zplContent)
    {
        if (string.IsNullOrWhiteSpace(zplContent))
        {
            return (false, "ZPL content cannot be empty");
        }

        // Check content length (max 100KB per label)
        const int maxLengthBytes = 100 * 1024; // 100KB
        var contentLength = Encoding.UTF8.GetByteCount(zplContent);
        if (contentLength > maxLengthBytes)
        {
            return (false, $"ZPL content exceeds maximum size of {maxLengthBytes} bytes (current: {contentLength} bytes)");
        }

        // Basic ZPL validation - must start with ^XA and end with ^XZ
        var trimmed = zplContent.Trim();
        if (!trimmed.StartsWith("^XA", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "ZPL content must start with ^XA");
        }

        if (!trimmed.EndsWith("^XZ", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "ZPL content must end with ^XZ");
        }

        // Block dangerous ZPL firmware commands that can brick printers
        var dangerousCommands = new[]
        {
            "^JU", // Upload firmware
            "^JF", // Flash firmware
            "~JR", // Reset printer
            "^MC", // Map clear
            "~MT", // Transfer memory
            "~HS"  // Host status
        };

        foreach (var command in dangerousCommands)
        {
            if (trimmed.Contains(command, StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"ZPL content contains prohibited command: {command}");
            }
        }

        return (true, null);
    }
}

/// <summary>
/// Helper class for sending raw data to a printer using Windows API.
/// Based on Microsoft documentation for raw printer access.
/// </summary>
internal static class RawPrinterHelper
{
    // Structure and API declarations for raw printer communication
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private class DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string? pDocName;
        [MarshalAs(UnmanagedType.LPStr)]
        public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)]
        public string? pDataType;
    }

    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

    [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    /// <summary>
    /// Sends a string to a printer using raw printer communication.
    /// </summary>
    public static bool SendStringToPrinter(string printerName, string stringToSend)
    {
        var hPrinter = IntPtr.Zero;
        var pBytes = IntPtr.Zero;

        try
        {
            // Open the printer
            if (!OpenPrinter(printerName.Normalize(), out hPrinter, IntPtr.Zero))
            {
                return false;
            }

            // Start a document
            var di = new DOCINFOA
            {
                pDocName = "Koinon Label",
                pDataType = "RAW" // RAW data type for direct ZPL
            };

            if (!StartDocPrinter(hPrinter, 1, di))
            {
                return false;
            }

            // Start a page
            if (!StartPagePrinter(hPrinter))
            {
                EndDocPrinter(hPrinter);
                return false;
            }

            // Convert string to bytes
            var bytes = Encoding.UTF8.GetBytes(stringToSend);
            var dwCount = bytes.Length;

            // Allocate unmanaged memory
            pBytes = Marshal.AllocCoTaskMem(dwCount);
            Marshal.Copy(bytes, 0, pBytes, dwCount);

            // Send the data to the printer
            var success = WritePrinter(hPrinter, pBytes, dwCount, out var dwWritten);

            // End the page and document
            EndPagePrinter(hPrinter);
            EndDocPrinter(hPrinter);

            return success && dwWritten == dwCount;
        }
        finally
        {
            // Clean up
            if (pBytes != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pBytes);
            }

            if (hPrinter != IntPtr.Zero)
            {
                ClosePrinter(hPrinter);
            }
        }
    }
}
