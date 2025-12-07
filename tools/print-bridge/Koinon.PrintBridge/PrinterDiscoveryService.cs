using System.Drawing.Printing;
using System.Management;

namespace Koinon.PrintBridge;

/// <summary>
/// Discovers and manages printer information.
/// Identifies Zebra thermal printers and verifies driver installation.
/// </summary>
public class PrinterDiscoveryService
{
    private readonly ILogger<PrinterDiscoveryService> _logger;
    private List<PrinterInfo> _cachedPrinters = new();
    private DateTime _lastDiscovery = DateTime.MinValue;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public PrinterDiscoveryService(ILogger<PrinterDiscoveryService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize the service and perform initial printer discovery.
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing printer discovery service...");
        var printers = await GetAvailablePrintersAsync();
        _logger.LogInformation("Discovered {Count} printers", printers.Count);

        var zebraPrinters = printers.Where(p => p.IsZebraPrinter).ToList();
        if (zebraPrinters.Count == 0)
        {
            _logger.LogWarning("No Zebra thermal printers found. Label printing will not be available.");
        }
        else
        {
            _logger.LogInformation("Found {Count} Zebra thermal printer(s): {Printers}",
                zebraPrinters.Count,
                string.Join(", ", zebraPrinters.Select(p => p.Name)));
        }
    }

    /// <summary>
    /// Gets all available printers on the system.
    /// Results are cached for 5 minutes to improve performance.
    /// </summary>
    public Task<List<PrinterInfo>> GetAvailablePrintersAsync()
    {
        // Return cached results if still valid
        if (DateTime.UtcNow - _lastDiscovery < CacheExpiration && _cachedPrinters.Count > 0)
        {
            _logger.LogDebug("Returning cached printer list ({Count} printers)", _cachedPrinters.Count);
            return Task.FromResult(_cachedPrinters);
        }

        return Task.Run(() =>
        {
            var printers = new List<PrinterInfo>();

            try
            {
                // Get all installed printers using PrinterSettings
                foreach (string printerName in PrinterSettings.InstalledPrinters)
                {
                    try
                    {
                        var info = GetPrinterInfo(printerName);
                        printers.Add(info);
                        _logger.LogDebug("Discovered printer: {Name} (Zebra: {IsZebra})",
                            info.Name, info.IsZebraPrinter);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get info for printer: {PrinterName}", printerName);
                    }
                }

                _cachedPrinters = printers;
                _lastDiscovery = DateTime.UtcNow;

                _logger.LogInformation("Printer discovery complete: {Count} printers found", printers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Printer discovery failed");
            }

            return printers;
        });
    }

    /// <summary>
    /// Gets detailed information about a specific printer.
    /// </summary>
    public PrinterInfo GetPrinterInfo(string printerName)
    {
        var settings = new PrinterSettings { PrinterName = printerName };

        var info = new PrinterInfo
        {
            Name = printerName,
            Status = settings.IsValid ? "Ready" : "Offline",
            IsDefault = settings.IsDefaultPrinter,
            IsZebraPrinter = IsZebraPrinter(printerName),
            DriverName = GetDriverName(printerName),
            PortName = GetPortName(printerName)
        };

        return info;
    }

    /// <summary>
    /// Forces a refresh of the printer cache.
    /// </summary>
    public async Task RefreshPrintersAsync()
    {
        _logger.LogInformation("Forcing printer refresh...");
        _lastDiscovery = DateTime.MinValue;
        await GetAvailablePrintersAsync();
    }

    /// <summary>
    /// Determines if a printer is a Zebra thermal printer based on name and driver.
    /// </summary>
    private static bool IsZebraPrinter(string printerName)
    {
        // Check printer name
        var nameLower = printerName.ToLowerInvariant();
        if (nameLower.Contains("zebra") || nameLower.Contains("zpl"))
        {
            return true;
        }

        // Check driver name
        var driver = GetDriverName(printerName);
        if (!string.IsNullOrEmpty(driver))
        {
            var driverLower = driver.ToLowerInvariant();
            if (driverLower.Contains("zebra") || driverLower.Contains("zpl"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the driver name for a printer using WMI.
    /// </summary>
    private static string GetDriverName(string printerName)
    {
        try
        {
            var query = $"SELECT DriverName FROM Win32_Printer WHERE Name = '{printerName.Replace("'", "''")}'";
            using var searcher = new ManagementObjectSearcher(query);
            using var results = searcher.Get();

            foreach (ManagementObject printer in results)
            {
                var driverName = printer["DriverName"]?.ToString();
                if (!string.IsNullOrEmpty(driverName))
                {
                    return driverName;
                }
            }
        }
        catch
        {
            // WMI queries can fail in some environments - this is non-critical
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the port name for a printer using WMI.
    /// </summary>
    private static string GetPortName(string printerName)
    {
        try
        {
            var query = $"SELECT PortName FROM Win32_Printer WHERE Name = '{printerName.Replace("'", "''")}'";
            using var searcher = new ManagementObjectSearcher(query);
            using var results = searcher.Get();

            foreach (ManagementObject printer in results)
            {
                var portName = printer["PortName"]?.ToString();
                if (!string.IsNullOrEmpty(portName))
                {
                    return portName;
                }
            }
        }
        catch
        {
            // WMI queries can fail in some environments - this is non-critical
        }

        return string.Empty;
    }
}

/// <summary>
/// Information about a discovered printer.
/// </summary>
public class PrinterInfo
{
    public required string Name { get; init; }
    public required string Status { get; init; }
    public required bool IsDefault { get; init; }
    public required bool IsZebraPrinter { get; init; }
    public required string DriverName { get; init; }
    public required string PortName { get; init; }
}
