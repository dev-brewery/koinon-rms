namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to update an existing location.
/// All fields are optional for partial updates.
/// </summary>
public record UpdateLocationRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool? IsActive { get; init; }

    // Hierarchy
    public string? ParentLocationIdKey { get; init; }

    // Campus relationship
    public string? CampusIdKey { get; init; }

    // Location type
    public string? LocationTypeValueIdKey { get; init; }

    // Capacity and staffing
    public int? SoftRoomThreshold { get; init; }
    public int? FirmRoomThreshold { get; init; }
    public int? StaffToChildRatio { get; init; }

    // Overflow handling
    public string? OverflowLocationIdKey { get; init; }
    public bool? AutoAssignOverflow { get; init; }

    // Address fields
    public string? Street1 { get; init; }
    public string? Street2 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }

    // Geographic coordinates
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public bool? IsGeoPointLocked { get; init; }

    public int? Order { get; init; }
}
