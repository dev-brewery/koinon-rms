namespace Koinon.Domain.Entities;

/// <summary>
/// Defines a type or template for groups (Family, Security Role, Serving Team, Small Group, etc.).
/// Groups are categorized by their GroupType, which defines the purpose and behavior of the group.
/// </summary>
public class GroupType : Entity
{
    /// <summary>
    /// Indicates whether this is a system-protected GroupType that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// The name of this GroupType.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of this GroupType.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The term to use for a group of this type (e.g., "Group", "Family", "Team").
    /// </summary>
    public string GroupTerm { get; set; } = "Group";

    /// <summary>
    /// The term to use for a member of a group of this type (e.g., "Member", "Participant").
    /// </summary>
    public string GroupMemberTerm { get; set; } = "Member";

    /// <summary>
    /// Optional foreign key to the default GroupTypeRole for new members.
    /// </summary>
    public int? DefaultGroupRoleId { get; set; }

    /// <summary>
    /// Optional CSS class for the icon representing this GroupType.
    /// </summary>
    public string? IconCssClass { get; set; }

    /// <summary>
    /// Hex color code for visual distinction (e.g., "#3B82F6").
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Whether groups of this type default to public visibility.
    /// </summary>
    public bool DefaultIsPublic { get; set; }

    /// <summary>
    /// Whether members can request to join groups of this type.
    /// </summary>
    public bool AllowSelfRegistration { get; set; }

    /// <summary>
    /// Whether membership requests require leader approval.
    /// </summary>
    public bool RequiresMemberApproval { get; set; } = true;

    /// <summary>
    /// Default capacity for groups of this type (null = unlimited).
    /// </summary>
    public int? DefaultGroupCapacity { get; set; }

    /// <summary>
    /// Indicates whether groups of this type can have multiple locations.
    /// </summary>
    public bool AllowMultipleLocations { get; set; }

    /// <summary>
    /// Indicates whether groups of this type should be shown in group lists.
    /// </summary>
    public bool ShowInGroupList { get; set; }

    /// <summary>
    /// Indicates whether groups of this type should be shown in navigation menus.
    /// </summary>
    public bool ShowInNavigation { get; set; }

    /// <summary>
    /// Indicates whether groups of this type track attendance.
    /// </summary>
    public bool TakesAttendance { get; set; }

    /// <summary>
    /// Indicates whether attendance at groups of this type counts as weekend service attendance.
    /// </summary>
    public bool AttendanceCountsAsWeekendService { get; set; }

    /// <summary>
    /// Indicates whether to send attendance reminders for groups of this type.
    /// </summary>
    public bool SendAttendanceReminder { get; set; }

    /// <summary>
    /// Indicates whether to show connection status for members of groups of this type.
    /// </summary>
    public bool ShowConnectionStatus { get; set; }

    /// <summary>
    /// Indicates whether groups of this type can have specific group requirements.
    /// </summary>
    public bool EnableSpecificGroupRequirements { get; set; }

    /// <summary>
    /// Indicates whether groups of this type can be synced with external systems.
    /// </summary>
    public bool AllowGroupSync { get; set; }

    /// <summary>
    /// Indicates whether specific group members can have additional attributes.
    /// </summary>
    public bool AllowSpecificGroupMemberAttributes { get; set; }

    /// <summary>
    /// Optional foreign key to the DefinedValue indicating the purpose of this GroupType.
    /// </summary>
    public int? GroupTypePurposeValueId { get; set; }

    /// <summary>
    /// Indicates whether to ignore person inactivation for groups of this type.
    /// </summary>
    public bool IgnorePersonInactivated { get; set; }

    /// <summary>
    /// Indicates whether this GroupType has been archived (soft delete).
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// PersonAlias ID of the user who archived this GroupType.
    /// </summary>
    public int? ArchivedByPersonAliasId { get; set; }

    /// <summary>
    /// Date and time when this GroupType was archived.
    /// </summary>
    public DateTime? ArchivedDateTime { get; set; }

    /// <summary>
    /// Display order for this GroupType.
    /// </summary>
    public int Order { get; set; }


    // Navigation Properties

    /// <summary>
    /// The roles available for groups of this type.
    /// </summary>
    public virtual ICollection<GroupTypeRole> Roles { get; set; } = new List<GroupTypeRole>();

    /// <summary>
    /// The groups of this type.
    /// </summary>
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
}
