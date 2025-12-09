namespace Koinon.Application.DTOs;

/// <summary>
/// Full group details DTO.
/// </summary>
public record GroupDto
{
    public required string IdKey { get; init; }
    public required Guid Guid { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsArchived { get; init; }
    public required bool IsSecurityRole { get; init; }
    public required bool IsPublic { get; init; }
    public required bool AllowGuests { get; init; }
    public int? GroupCapacity { get; init; }
    public required int Order { get; init; }
    public required GroupTypeSummaryDto GroupType { get; init; }
    public CampusSummaryDto? Campus { get; init; }
    public GroupSummaryDto? ParentGroup { get; init; }
    public required IReadOnlyList<GroupMemberDto> Members { get; init; }
    public required IReadOnlyList<GroupSummaryDto> ChildGroups { get; init; }
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
    public DateTime? ArchivedDateTime { get; init; }
}

/// <summary>
/// Summary group DTO for lists and references.
/// </summary>
public record GroupSummaryDto
{
    public required string IdKey { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
    public bool IsArchived { get; init; }
    public required int MemberCount { get; init; }
    public required string GroupTypeName { get; init; }
}

/// <summary>
/// Group member DTO representing a person's membership in a group.
/// </summary>
public record GroupMemberDto
{
    public required string IdKey { get; init; }
    public required PersonSummaryDto Person { get; init; }
    public required GroupTypeRoleDto Role { get; init; }
    public required string Status { get; init; }
    public DateTime? DateTimeAdded { get; init; }
    public DateTime? InactiveDateTime { get; init; }
    public string? Note { get; init; }
}

