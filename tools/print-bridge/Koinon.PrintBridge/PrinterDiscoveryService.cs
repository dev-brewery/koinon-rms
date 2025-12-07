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
        var dymoPrinters = printers.Where(p => p.IsDymoPrinter).ToList();

        if (zebraPrinters.Count == 0 && dymoPrinters.Count == 0)
        {
            _logger.LogWarning("No Zebra or Dymo label printers found. Label printing will not be available.");
        }
        else
        {
            if (zebraPrinters.Count > 0)
            {
                _logger.LogInformation("Found {Count} Zebra thermal printer(s): {Printers}",
                    zebraPrinters.Count,
                    string.Join(", ", zebraPrinters.Select(p => p.Name)));
            }

            if (dymoPrinters.Count > 0)
            {
                _logger.LogInformation("Found {Count} Dymo label printer(s): {Printers}",
                    dymoPrinters.Count,
                    string.Join(", ", dymoPrinters.Select(p => p.Name)));
            }
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
                        _logger.LogDebug("Discovered printer: {Name} (Type: {Type})",
                            info.Name, info.PrinterType);
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
        var driverName = GetDriverName(printerName);
        var isZebra = IsZebraPrinter(printerName, driverName);
        var isDymo = IsDymoPrinter(printerName, driverName);

        var info = new PrinterInfo
        {
            Name = printerName,
            Status = settings.IsValid ? "Ready" : "Offline",
            IsDefault = settings.IsDefaultPrinter,
            IsZebraPrinter = isZebra,
            IsDymoPrinter = isDymo,
            DriverName = driverName,
            PortName = GetPortName(printerName),
            PrinterType = DeterminePrinterType(isZebra, isDymo),
            SupportsZpl = isZebra,
            SupportsImage = true  // All Windows printers support GDI image printing
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
    private static bool IsZebraPrinter(string printerName, string driverName)
    {
        // Check printer name
        var nameLower = printerName.ToLowerInvariant();
        if (nameLower.Contains("zebra") || nameLower.Contains("zpl"))
        {
            return true;
        }

        // Check driver name
        if (!string.IsNullOrEmpty(driverName))
        {
            var driverLower = driverName.ToLowerInvariant();
            if (driverLower.Contains("zebra") || driverLower.Contains("zpl"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if a printer is a Dymo label printer based on name and driver.
    /// </summary>
    private static bool IsDymoPrinter(string printerName, string driverName)
    {
        // Check printer name
        var nameLower = printerName.ToLowerInvariant();
        if (nameLower.Contains("dymo") || nameLower.Contains("labelwriter"))
        {
            return true;
        }

        // Check driver name
        if (!string.IsNullOrEmpty(driverName))
        {
            var driverLower = driverName.ToLowerInvariant();
            if (driverLower.Contains("dymo") || driverLower.Contains("labelwriter"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines the printer type based on detected capabilities.
    /// </summary>
    private static string DeterminePrinterType(bool isZebra, bool isDymo)
    {
        if (isZebra) return "Zebra";
        if (isDymo) return "Dymo";
        return "Generic";
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
