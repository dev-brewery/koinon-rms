namespace Koinon.PrintBridge;

/// <summary>
/// Information about a discovered printer.
/// </summary>
public class PrinterInfo
{
    public required string Name { get; init; }
    public required string Status { get; init; }
    public required bool IsDefault { get; init; }
    public required bool IsZebraPrinter { get; init; }
    public required bool IsDymoPrinter { get; init; }
    public required string DriverName { get; init; }
    public required string PortName { get; init; }
    public required string PrinterType { get; init; }  // "Zebra", "Dymo", "Generic"
    public required bool SupportsZpl { get; init; }     // Only Zebra
    public required bool SupportsImage { get; init; }   // All printers
}
