namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a physical location such as a building, room, or address.
/// Locations support hierarchical organization (e.g., rooms within buildings).
/// Used by campuses, groups, and check-in systems.
/// </summary>
public class Location : Entity
{
    /// <summary>
    /// Foreign key to the parent location (for hierarchical organization).
    /// For example, a room would reference the building as its parent.
    /// </summary>
    public int? ParentLocationId { get; set; }

    /// <summary>
    /// Navigation property to the parent location.
    /// </summary>
    public virtual Location? ParentLocation { get; set; }

    /// <summary>
    /// Collection of child locations under this location.
    /// </summary>
    public virtual ICollection<Location> ChildLocations { get; set; } = new List<Location>();

    /// <summary>
    /// The name of the location.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Foreign key to DefinedValue specifying the location type (Room, Building, Campus, etc.).
    /// </summary>
    public int? LocationTypeValueId { get; set; }

    /// <summary>
    /// Navigation property to the location type DefinedValue.
    /// </summary>
    public virtual DefinedValue? LocationTypeValue { get; set; }

    /// <summary>
    /// Indicates whether this location is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional description of the location.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Foreign key to the printer device associated with this location (for check-in labels).
    /// </summary>
    public int? PrinterDeviceId { get; set; }

    /// <summary>
    /// Foreign key to a BinaryFile containing an image of the location.
    /// </summary>
    public int? ImageId { get; set; }

    /// <summary>
    /// Soft capacity threshold that triggers a warning when exceeded.
    /// </summary>
    public int? SoftRoomThreshold { get; set; }

    /// <summary>
    /// Hard capacity limit that prevents additional check-ins when exceeded.
    /// </summary>
    public int? FirmRoomThreshold { get; set; }

    /// <summary>
    /// Indicates whether the geographic point (GeoPoint) should not be automatically updated.
    /// </summary>
    public bool IsGeoPointLocked { get; set; }

    // Address components
    /// <summary>
    /// Street address line 1.
    /// </summary>
    public string? Street1 { get; set; }

    /// <summary>
    /// Street address line 2 (apartment, suite, etc.).
    /// </summary>
    public string? Street2 { get; set; }

    /// <summary>
    /// City name.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// State or province.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Postal code or ZIP code.
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Country name.
    /// </summary>
    public string? Country { get; set; }

    // Geographic coordinates
    /// <summary>
    /// Latitude coordinate for geocoded locations.
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate for geocoded locations.
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// Display order for sorting locations.
    /// </summary>
    public int Order { get; set; }
}
