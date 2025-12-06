namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to create a new group.
/// </summary>
public class CreateGroupRequest
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
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? CampusId { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsPublic { get; init; }
    public bool? AllowGuests { get; init; }
    public int? GroupCapacity { get; init; }
    public int? Order { get; init; }
}

/// <summary>
/// Request to add a member to a group.
/// </summary>
public class AddGroupMemberRequest
{
    public required string PersonId { get; init; }
    public required string RoleId { get; init; }
    public string? Note { get; init; }
}

/// <summary>
/// Search parameters for groups.
/// </summary>
public class GroupSearchParameters
{
    public string? Query { get; set; }
    public string? GroupTypeId { get; set; }
    public string? CampusId { get; set; }
    public string? ParentGroupId { get; set; }
    public bool IncludeInactive { get; set; } = false;
    public bool IncludeArchived { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    public void ValidatePageSize()
    {
        if (PageSize > 100)
        {
            PageSize = 100;
        }

        if (PageSize < 1)
        {
            PageSize = 50;
        }
    }
}
