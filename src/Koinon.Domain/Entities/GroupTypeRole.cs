namespace Koinon.Domain.Entities;

/// <summary>
/// Defines a role that can be assigned to members within a specific GroupType
/// (e.g., "Adult" and "Child" roles for Family groups, "Leader" and "Member" for small groups).
/// </summary>
public class GroupTypeRole : Entity
{
    /// <summary>
    /// Indicates whether this is a system-protected role that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Foreign key to the GroupType this role belongs to.
    /// </summary>
    public required int GroupTypeId { get; set; }

    /// <summary>
    /// The name of this role.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of this role.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether this is a leader role.
    /// Leaders typically have elevated permissions within the group.
    /// </summary>
    public bool IsLeader { get; set; }

    /// <summary>
    /// Indicates whether members with this role can view the group.
    /// </summary>
    public bool CanView { get; set; }

    /// <summary>
    /// Indicates whether members with this role can edit the group.
    /// </summary>
    public bool CanEdit { get; set; }

    /// <summary>
    /// Indicates whether members with this role can manage other members.
    /// </summary>
    public bool CanManageMembers { get; set; }

    /// <summary>
    /// Indicates whether members with this role receive requirement notifications.
    /// </summary>
    public bool ReceiveRequirementsNotifications { get; set; }

    /// <summary>
    /// Display order for this role.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Optional maximum number of members that can have this role in a single group.
    /// </summary>
    public int? MaxCount { get; set; }

    /// <summary>
    /// Optional minimum number of members that must have this role in a single group.
    /// </summary>
    public int? MinCount { get; set; }

    /// <summary>
    /// Indicates whether this role has been archived (soft delete).
    /// </summary>
    public bool IsArchived { get; set; }

    // Navigation Properties

    /// <summary>
    /// The GroupType this role belongs to.
    /// </summary>
    public virtual GroupType? GroupType { get; set; }
}
