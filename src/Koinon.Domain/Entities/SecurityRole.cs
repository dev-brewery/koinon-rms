namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a security role that can be assigned to persons and contains security claims.
/// </summary>
public class SecurityRole : Entity
{
    /// <summary>
    /// Gets or sets the name of the security role.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the optional description of the security role.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a system-defined role that cannot be deleted.
    /// </summary>
    public bool IsSystemRole { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether this role is active and can be assigned.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets the collection of person-role assignments.
    /// </summary>
    public virtual ICollection<PersonSecurityRole> PersonRoles { get; set; } = new List<PersonSecurityRole>();

    /// <summary>
    /// Gets the collection of security claims associated with this role.
    /// </summary>
    public virtual ICollection<RoleSecurityClaim> RoleClaims { get; set; } = new List<RoleSecurityClaim>();
}
