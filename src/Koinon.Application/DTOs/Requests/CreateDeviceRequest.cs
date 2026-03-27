namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to create a new device.
/// </summary>
public record CreateDeviceRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }

    /// <summary>IdKey of the DefinedValue that represents the device type (e.g. Kiosk, Printer).</summary>
    public string? DeviceTypeValueIdKey { get; init; }

    public string? CampusIdKey { get; init; }
    public string? IpAddress { get; init; }
    public bool IsActive { get; init; } = true;
}
