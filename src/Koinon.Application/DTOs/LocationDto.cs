namespace Koinon.Application.DTOs;

/// <summary>
/// Full location DTO with all fields including hierarchical structure and address.
/// Represents physical locations (buildings, rooms) that may also have postal addresses.
/// </summary>
public record LocationDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
    public required int Order { get; init; }
    
    // Hierarchical structure
    public string? ParentLocationIdKey { get; init; }
    public string? ParentLocationName { get; init; }
    public required IReadOnlyList<LocationDto> Children { get; init; }
    
    // Campus relationship
    public string? CampusIdKey { get; init; }
    public string? CampusName { get; init; }
    
    // Location type
    public string? LocationTypeName { get; init; }
    
    // Capacity thresholds
    public int? SoftRoomThreshold { get; init; }
    public int? FirmRoomThreshold { get; init; }
    public int? StaffToChildRatio { get; init; }
    
    // Overflow handling
    public string? OverflowLocationIdKey { get; init; }
    public string? OverflowLocationName { get; init; }
    public bool AutoAssignOverflow { get; init; }
    
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
    public bool IsGeoPointLocked { get; init; }
    
    // Audit fields
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}

/// <summary>
/// Summary location DTO for lists and references.
/// </summary>
public record LocationSummaryDto
{
    public required string IdKey { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
    public string? ParentLocationName { get; init; }
    public string? CampusName { get; init; }
    public string? LocationTypeName { get; init; }
}
