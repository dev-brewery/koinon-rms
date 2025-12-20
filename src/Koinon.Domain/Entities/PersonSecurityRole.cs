namespace Koinon.Domain.Entities;

/// <summary>
/// Represents the association between a Person and a SecurityRole.
/// This junction entity allows a person to have multiple security roles,
/// each with an optional expiration date.
/// </summary>
public class PersonSecurityRole : Entity
{
    /// <summary>
    /// Gets or sets the foreign key to the Person entity.
    /// </summary>
    public int PersonId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the SecurityRole entity.
    /// </summary>
    public int SecurityRoleId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this role assignment expires.
    /// If null, the role assignment does not expire.
    /// </summary>
    public DateTime? ExpiresDateTime { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the associated Person.
    /// </summary>
    public virtual Person Person { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the associated SecurityRole.
    /// </summary>
    public virtual SecurityRole SecurityRole { get; set; } = null!;
}
