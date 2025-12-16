namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a household unit (parents, children living together).
/// Completely independent from Group entity.
/// </summary>
public class Family : Entity
{
    /// <summary>
    /// Family name (typically last name or compound name like "Smith-Jones").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional foreign key to Campus for the family's primary campus.
    /// </summary>
    public int? CampusId { get; set; }

    /// <summary>
    /// Whether this family is active in the system.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation Properties

    /// <summary>
    /// The campus this family primarily attends.
    /// </summary>
    public virtual Campus? Campus { get; set; }

    /// <summary>
    /// Members of this family.
    /// </summary>
    public virtual ICollection<FamilyMember> Members { get; set; } = new List<FamilyMember>();
}
