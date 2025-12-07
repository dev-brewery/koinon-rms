namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a physical or virtual device (kiosk, printer, etc.) used in the system.
/// Primarily used for check-in kiosks but can represent other device types.
/// </summary>
public class Device : Entity
{
    /// <summary>
    /// Name of the device (e.g., "Main Lobby Kiosk", "Children's Check-in #1").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Description of the device and its purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of device (Kiosk, Printer, etc.).
    /// Foreign key to DefinedValue.
    /// </summary>
    public int? DeviceTypeValueId { get; set; }

    /// <summary>
    /// IP address of the device.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Print settings for label printing (JSON format).
    /// </summary>
    public string? PrinterSettings { get; set; }

    /// <summary>
    /// Campus this device is associated with.
    /// </summary>
    public int? CampusId { get; set; }

    /// <summary>
    /// Locations this device can check-in to (comma-separated IdKeys or JSON).
    /// </summary>
    public string? Locations { get; set; }

    /// <summary>
    /// Whether this device is currently active.
    /// Inactive devices cannot be used for check-in.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Authentication token for kiosk access.
    /// Used in X-Kiosk-Token header for API authentication.
    /// Should be a securely generated random string (e.g., 64 character hex string).
    /// </summary>
    public string? KioskToken { get; set; }

    /// <summary>
    /// Date and time when the kiosk token expires.
    /// Null means no expiration.
    /// </summary>
    public DateTime? KioskTokenExpiresAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Navigation property to the device type DefinedValue.
    /// </summary>
    public virtual DefinedValue? DeviceTypeValue { get; set; }

    /// <summary>
    /// Navigation property to the Campus.
    /// </summary>
    public virtual Campus? Campus { get; set; }

    /// <summary>
    /// Collection of attendance records created by this device.
    /// </summary>
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
