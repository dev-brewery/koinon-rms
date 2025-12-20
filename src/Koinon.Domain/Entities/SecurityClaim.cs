namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a security claim that can be assigned to roles.
/// Claims define specific permissions or capabilities within the system.
/// </summary>
public class SecurityClaim : Entity
{
    /// <summary>
    /// The type or category of the claim (e.g., "person:edit", "finance:view", "admin:configure").
    /// </summary>
    public required string ClaimType { get; set; }

    /// <summary>
    /// The value associated with the claim type (typically "true" for boolean permissions).
    /// </summary>
    public required string ClaimValue { get; set; }

    /// <summary>
    /// Human-readable description of what this claim permits.
    /// </summary>
    public string? Description { get; set; }

    // Navigation properties

    /// <summary>
    /// Collection of role assignments for this claim.
    /// </summary>
    public virtual ICollection<RoleSecurityClaim> RoleClaims { get; set; } = new List<RoleSecurityClaim>();
}
