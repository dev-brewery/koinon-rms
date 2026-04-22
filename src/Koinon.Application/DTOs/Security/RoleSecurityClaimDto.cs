namespace Koinon.Application.DTOs.Security;

/// <summary>
/// DTO representing a claim association on a security role, including the
/// Allow/Deny decision for that role.
/// </summary>
public record RoleSecurityClaimDto
{
    /// <summary>
    /// Gets the encoded identifier of the underlying security claim.
    /// </summary>
    public required string ClaimIdKey { get; init; }

    /// <summary>
    /// Gets the type or category of the claim.
    /// </summary>
    public required string ClaimType { get; init; }

    /// <summary>
    /// Gets the value associated with the claim type.
    /// </summary>
    public required string ClaimValue { get; init; }

    /// <summary>
    /// Gets the human-readable description of what this claim permits.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets whether this claim is allowed ('A') or denied ('D') for the role.
    /// </summary>
    public required char AllowOrDeny { get; init; }
}
