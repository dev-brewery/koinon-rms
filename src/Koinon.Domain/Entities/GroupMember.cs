using Koinon.Domain.Enums;

namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a person's membership in a group with a specific role.
/// This is the join table between Person and Group, establishing the many-to-many relationship.
/// </summary>
public class GroupMember : Entity
{
    /// <summary>
    /// Indicates whether this is a system-protected group membership that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Foreign key to the Person who is a member of the group.
    /// </summary>
    public required int PersonId { get; set; }

    /// <summary>
    /// Foreign key to the Group this person is a member of.
    /// </summary>
    public required int GroupId { get; set; }

    /// <summary>
    /// Foreign key to the GroupTypeRole defining this person's role in the group.
    /// </summary>
    public required int GroupRoleId { get; set; }

    /// <summary>
    /// The current status of this membership (Active, Inactive, or Pending).
    /// </summary>
    public GroupMemberStatus GroupMemberStatus { get; set; } = GroupMemberStatus.Active;

    /// <summary>
    /// Date and time when the person was added to the group.
    /// </summary>
    public DateTime? DateTimeAdded { get; set; }

    /// <summary>
    /// Date and time when the person became inactive in the group.
    /// </summary>
    public DateTime? InactiveDateTime { get; set; }

    /// <summary>
    /// Indicates whether this group membership has been archived (soft delete).
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Date and time when this membership was archived.
    /// </summary>
    public DateTime? ArchivedDateTime { get; set; }

    /// <summary>
    /// PersonAlias ID of the user who archived this membership.
    /// </summary>
    public int? ArchivedByPersonAliasId { get; set; }

    /// <summary>
    /// Optional note about this membership.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Indicates whether the person has been notified of their membership.
    /// </summary>
    public bool IsNotified { get; set; }

    /// <summary>
    /// Communication preference for this specific group membership.
    /// Can override the person's general communication preference.
    /// </summary>
    public int? CommunicationPreference { get; set; }

    /// <summary>
    /// Optional number of guests this member is bringing (for groups that track attendance).
    /// </summary>
    public int? GuestCount { get; set; }

    // Navigation Properties

    /// <summary>
    /// The Person who is a member of the group.
    /// </summary>
    public virtual Person? Person { get; set; }

    /// <summary>
    /// The Group this person is a member of.
    /// </summary>
    public virtual Group? Group { get; set; }

    /// <summary>
    /// The role this person has in the group.
    /// </summary>
    public virtual GroupTypeRole? GroupRole { get; set; }
}
