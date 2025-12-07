using System.Management;
using System.Runtime.Versioning;

namespace Koinon.PrintBridge.Services;

/// <summary>
/// Service for discovering and querying available printers on the local system.
/// </summary>
[SupportedOSPlatform("windows")]
public class PrinterDiscoveryService
{
    private readonly ILogger<PrinterDiscoveryService> _logger;
    private string? _defaultPrinterName;

    public PrinterDiscoveryService(ILogger<PrinterDiscoveryService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets all available printers on the local system.
    /// </summary>
    public IEnumerable<PrinterInfo> GetAvailablePrinters()
    {
        var printers = new List<PrinterInfo>();

        try
        {
            var defaultPrinter = GetDefaultPrinterName();

            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer");
            using var collection = searcher.Get();

            foreach (var obj in collection.Cast<ManagementObject>())
            {
                try
                {
                    var name = obj["Name"]?.ToString();
                    if (string.IsNullOrEmpty(name))
                        continue;

                    var status = GetPrinterStatus(obj);
                    var isDefault = name == defaultPrinter;

                    var printerInfo = new PrinterInfo
                    {
                        Name = name,
                        Status = status,
                        Type = DeterminePrinterType(name),
                        IsDefault = isDefault
                    };

                    printers.Add(printerInfo);
                    _logger.LogDebug("Discovered printer: {PrinterName} ({Type})", name, printerInfo.Type);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error querying printer");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enumerating printers");
        }

        return printers;
    }

    /// <summary>
    /// Gets a specific printer by name.
    /// </summary>
    public PrinterInfo? GetPrinterByName(string printerName)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_Printer WHERE Name = '{EscapeWmiString(printerName)}'");
            using var collection = searcher.Get();

            var obj = collection.Cast<ManagementObject>().FirstOrDefault();
            if (obj == null)
            {
                _logger.LogWarning("Printer not found: {PrinterName}", printerName);
                return null;
            }

            var defaultPrinter = GetDefaultPrinterName();
            var status = GetPrinterStatus(obj);

            return new PrinterInfo
            {
                Name = printerName,
                Status = status,
                Type = DeterminePrinterType(printerName),
                IsDefault = printerName == defaultPrinter
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Printer query failed: {PrinterName}", printerName);
            return null;
        }
    }

    /// <summary>
    /// Gets the default printer for the system.
    /// </summary>
    public PrinterInfo? GetDefaultPrinter()
    {
        var defaultName = GetDefaultPrinterName();
        if (string.IsNullOrEmpty(defaultName))
            return null;

        return GetPrinterByName(defaultName);
    }

    /// <summary>
    /// Gets the name of the default printer.
    /// </summary>
    private string? GetDefaultPrinterName()
    {
        if (_defaultPrinterName != null)
            return _defaultPrinterName;

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Printer WHERE Default = true");
            using var collection = searcher.Get();

            var obj = collection.Cast<ManagementObject>().FirstOrDefault();
            if (obj != null)
            {
                _defaultPrinterName = obj["Name"]?.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving default printer");
        }

        return _defaultPrinterName;
    }

    /// <summary>
    /// Gets the status of a printer from WMI object.
    /// </summary>
    private static string GetPrinterStatus(ManagementObject printerObj)
    {
        try
        {
            var statusValue = printerObj["PrinterStatus"]?.ToString();
            return statusValue switch
            {
                "0" => "Ready",
                "1" => "Paused",
                "2" => "Error",
                "3" => "PaperJam",
                "4" => "PaperOut",
                "5" => "ManualFeed",
                _ => statusValue ?? "Unknown"
            };
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Escapes a string for use in WMI queries.
    /// </summary>
    private static string EscapeWmiString(string input)
    {
        return input.Replace("\\", "\\\\").Replace("'", "\\'");
    }

    /// <summary>
    /// Determines the printer type based on name patterns.
    /// </summary>
    private static string DeterminePrinterType(string printerName)
    {
        var lowerName = printerName.ToLowerInvariant();

        if (lowerName.Contains("zebra"))
            return "ZPL";

        if (lowerName.Contains("dymo"))
            return "EPL";

        if (lowerName.Contains("thermal"))
            return "ZPL";

        return "Unknown";
    }
}

/// <summary>
/// Represents information about an available printer.
/// </summary>
public record PrinterInfo
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string Status { get; init; }
    public required bool IsDefault { get; init; }
}
