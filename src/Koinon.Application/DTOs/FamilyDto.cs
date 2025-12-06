namespace Koinon.Application.DTOs;

/// <summary>
/// Full family details DTO with members.
/// </summary>
public class FamilyDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
    public CampusSummaryDto? Campus { get; init; }
    public LocationDto? Address { get; init; }
    public required IReadOnlyList<FamilyMemberDto> Members { get; init; }
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}

/// <summary>
/// Family member DTO representing a person's membership in a family.
/// </summary>
public class FamilyMemberDto
{
    public required string IdKey { get; init; }
    public required PersonSummaryDto Person { get; init; }
    public required GroupTypeRoleDto Role { get; init; }
    public required string Status { get; init; }
    public DateTime? DateTimeAdded { get; init; }
}

/// <summary>
/// Location/Address DTO.
/// </summary>
public class LocationDto
{
    public required string IdKey { get; init; }
    public string? Street1 { get; init; }
    public string? Street2 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public required string FormattedAddress { get; init; }
}

/// <summary>
/// Group type role DTO.
/// </summary>
public class GroupTypeRoleDto
{
    public required string IdKey { get; init; }
    public required string Name { get; init; }
    public required bool IsLeader { get; init; }
}
