namespace Koinon.Domain.Entities;

/// <summary>
/// Represents the junction entity between SecurityRole and SecurityClaim with permission control.
/// Defines whether a specific claim is allowed or denied for a security role.
/// </summary>
public class RoleSecurityClaim : Entity
{
    /// <summary>
    /// Gets or sets the foreign key to the SecurityRole.
    /// </summary>
    public int SecurityRoleId { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the SecurityClaim.
    /// </summary>
    public int SecurityClaimId { get; set; }

    /// <summary>
    /// Gets or sets whether the claim is allowed ('A') or denied ('D') for this role.
    /// Valid values: 'A' (Allow), 'D' (Deny).
    /// </summary>
    public char AllowOrDeny { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the associated SecurityRole.
    /// </summary>
    public virtual SecurityRole SecurityRole { get; set; } = null!;

    /// <summary>
    /// Gets or sets the navigation property to the associated SecurityClaim.
    /// </summary>
    public virtual SecurityClaim SecurityClaim { get; set; } = null!;
}
