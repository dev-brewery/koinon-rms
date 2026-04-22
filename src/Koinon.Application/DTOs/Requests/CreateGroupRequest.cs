namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to create a new group.
/// </summary>
public record CreateGroupRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string GroupTypeId { get; init; }
    public string? ParentGroupId { get; init; }
    public string? CampusId { get; init; }
    public bool IsActive { get; init; } = true;
    public bool IsPublic { get; init; } = false;
    public bool AllowGuests { get; init; } = false;
    public int? GroupCapacity { get; init; }
    public int Order { get; init; } = 0;
}

/// <summary>
/// Request to update an existing group.
/// </summary>
public class UpdateGroupRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? CampusId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsPublic { get; set; }
    public bool? AllowGuests { get; set; }
    public int? GroupCapacity { get; set; }
    public int? Order { get; set; }
}

/// <summary>
/// Request to add a member to a group.
/// </summary>
public record AddGroupMemberRequest
{
    public required string PersonId { get; init; }
    public required string RoleId { get; init; }
    public string? Note { get; init; }
}

/// <summary>
/// Search parameters for groups.
/// </summary>
public record GroupSearchParameters
{
    public string? Query { get; init; }
    public string? GroupTypeId { get; init; }
    public string? CampusId { get; init; }
    public string? ParentGroupId { get; init; }
    public bool IncludeInactive { get; init; } = false;
    public bool IncludeArchived { get; init; } = false;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
