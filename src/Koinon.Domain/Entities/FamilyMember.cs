namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a person's membership in a family (household).
/// Junction entity between Family and Person.
/// </summary>
public class FamilyMember : Entity
{
    /// <summary>
    /// Foreign key to the Family.
    /// </summary>
    public int FamilyId { get; set; }

    /// <summary>
    /// Foreign key to the Person.
    /// </summary>
    public int PersonId { get; set; }

    /// <summary>
    /// Foreign key to GroupTypeRole indicating the family role (Adult, Child, etc.).
    /// </summary>
    public int FamilyRoleId { get; set; }

    /// <summary>
    /// Whether this is the person's primary family (for people in multiple families).
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Date when this person was added to the family.
    /// </summary>
    public DateTime DateAdded { get; set; }

    // Navigation Properties

    /// <summary>
    /// The family this member belongs to.
    /// </summary>
    public virtual Family Family { get; set; } = null!;

    /// <summary>
    /// The person who is a member of the family.
    /// </summary>
    public virtual Person Person { get; set; } = null!;

    /// <summary>
    /// The group type role representing the family role (Adult, Child, etc.).
    /// </summary>
    public virtual GroupTypeRole FamilyRole { get; set; } = null!;
}
