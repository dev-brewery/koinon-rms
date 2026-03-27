namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to update an existing device.
/// All fields are optional for partial updates.
/// </summary>
public record UpdateDeviceRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }

    /// <summary>IdKey of the DefinedValue that represents the device type. Pass empty string to clear.</summary>
    public string? DeviceTypeValueIdKey { get; init; }

    /// <summary>IdKey of the campus. Pass empty string to clear.</summary>
    public string? CampusIdKey { get; init; }

    public string? IpAddress { get; init; }
    public bool? IsActive { get; init; }

    /// <summary>Raw JSON printer settings blob. Pass empty string to clear.</summary>
    public string? PrinterSettings { get; init; }
}
