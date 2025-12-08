namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a group of people with a specific purpose.
/// Groups are the universal container in the system - families, serving teams, small groups,
/// security roles, and check-in areas are all represented as groups.
/// </summary>
public class Group : Entity
{
    /// <summary>
    /// Indicates whether this is a system-protected group that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Foreign key to the GroupType that defines the template for this group.
    /// </summary>
    public required int GroupTypeId { get; set; }

    /// <summary>
    /// Optional foreign key to the parent group (for hierarchical group structures).
    /// </summary>
    public int? ParentGroupId { get; set; }

    /// <summary>
    /// Optional foreign key to the Campus this group is associated with.
    /// </summary>
    public int? CampusId { get; set; }

    /// <summary>
    /// The name of this group.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of this group.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether this group functions as a security role.
    /// </summary>
    public bool IsSecurityRole { get; set; }

    /// <summary>
    /// Indicates whether this group is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Indicates whether this group has been archived (soft delete).
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// PersonAlias ID of the user who archived this group.
    /// </summary>
    public int? ArchivedByPersonAliasId { get; set; }

    /// <summary>
    /// Date and time when this group was archived.
    /// </summary>
    public DateTime? ArchivedDateTime { get; set; }

    /// <summary>
    /// Display order for this group within its parent (if any).
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Indicates whether this group allows guests who are not members.
    /// </summary>
    public bool AllowGuests { get; set; }

    /// <summary>
    /// Indicates whether this group is visible to the public.
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Optional maximum number of members this group can have.
    /// </summary>
    public int? GroupCapacity { get; set; }

    /// <summary>
    /// Optional foreign key to the Schedule for when this group meets.
    /// </summary>
    public int? ScheduleId { get; set; }

    /// <summary>
    /// Optional foreign key to the SystemCommunication sent to new members.
    /// </summary>
    public int? WelcomeSystemCommunicationId { get; set; }

    /// <summary>
    /// Optional foreign key to the SystemCommunication sent to departing members.
    /// </summary>
    public int? ExitSystemCommunicationId { get; set; }

    /// <summary>
    /// Optional foreign key to the document template required for group membership.
    /// </summary>
    public int? RequiredSignatureDocumentTemplateId { get; set; }

    /// <summary>
    /// Optional foreign key to the DefinedValue indicating the status of this group.
    /// </summary>
    public int? StatusValueId { get; set; }

    /// <summary>
    /// Minimum age in months for eligibility in this group (for age-based filtering).
    /// Null means no minimum age restriction.
    /// </summary>
    public int? MinAgeMonths { get; set; }

    /// <summary>
    /// Maximum age in months for eligibility in this group (for age-based filtering).
    /// Null means no maximum age restriction.
    /// </summary>
    public int? MaxAgeMonths { get; set; }

    /// <summary>
    /// Minimum grade for eligibility in this group (for grade-based filtering).
    /// Grade scale: -1 = Pre-K, 0 = Kindergarten, 1 = 1st grade, etc.
    /// Null means no minimum grade restriction.
    /// </summary>
    public int? MinGrade { get; set; }

    /// <summary>
    /// Maximum grade for eligibility in this group (for grade-based filtering).
    /// Grade scale: -1 = Pre-K, 0 = Kindergarten, 1 = 1st grade, etc.
    /// Null means no maximum grade restriction.
    /// </summary>
    public int? MaxGrade { get; set; }

    // Navigation Properties

    /// <summary>
    /// The GroupType that defines the template for this group.
    /// </summary>
    public virtual GroupType? GroupType { get; set; }

    /// <summary>
    /// The Schedule for when this group meets (optional).
    /// </summary>
    public virtual Schedule? Schedule { get; set; }

    /// <summary>
    /// The Campus this group is associated with (if any).
    /// </summary>
    public virtual Campus? Campus { get; set; }

    /// <summary>
    /// The parent group (if this group is part of a hierarchy).
    /// </summary>
    public virtual Group? ParentGroup { get; set; }

    /// <summary>
    /// Child groups (if this group has subgroups).
    /// </summary>
    public virtual ICollection<Group> ChildGroups { get; set; } = new List<Group>();

    /// <summary>
    /// Collection of members in this group.
    /// </summary>
    public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
}
