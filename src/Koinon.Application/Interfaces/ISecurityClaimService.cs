using Koinon.Application.DTOs.Security;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for RBAC (Role-Based Access Control) authorization operations.
/// Implements claim resolution with DENY-takes-precedence logic.
/// </summary>
public interface ISecurityClaimService
{
    /// <summary>
    /// Checks if a person has a specific claim. DENY always takes precedence over ALLOW.
    /// </summary>
    /// <param name="personId">The person's ID.</param>
    /// <param name="claimType">The claim type (e.g., "permission").</param>
    /// <param name="claimValue">The claim value (e.g., "person:edit").</param>
    /// <returns>True if the person has the claim and it's not denied; otherwise false.</returns>
    Task<bool> HasClaimAsync(int personId, string claimType, string claimValue);

    /// <summary>
    /// Checks if a person has any of the specified claim values.
    /// </summary>
    /// <param name="personId">The person's ID.</param>
    /// <param name="claimType">The claim type.</param>
    /// <param name="claimValues">The list of claim values to check.</param>
    /// <returns>True if the person has at least one of the claims; otherwise false.</returns>
    Task<bool> HasAnyClaimAsync(int personId, string claimType, IEnumerable<string> claimValues);

    /// <summary>
    /// Checks if a person has all of the specified claim values.
    /// </summary>
    /// <param name="personId">The person's ID.</param>
    /// <param name="claimType">The claim type.</param>
    /// <param name="claimValues">The list of claim values to check.</param>
    /// <returns>True if the person has all of the claims; otherwise false.</returns>
    Task<bool> HasAllClaimsAsync(int personId, string claimType, IEnumerable<string> claimValues);

    /// <summary>
    /// Gets all allowed claims for a person (excluding denied claims).
    /// </summary>
    /// <param name="personId">The person's ID.</param>
    /// <param name="claimType">The claim type to filter by.</param>
    /// <returns>List of claim values that are allowed for the person.</returns>
    Task<IEnumerable<string>> GetPersonClaimsAsync(int personId, string claimType);

    /// <summary>
    /// Gets all active security roles for a person (non-expired roles only).
    /// </summary>
    /// <param name="personId">The person's ID.</param>
    /// <returns>List of security roles with their details.</returns>
    Task<IEnumerable<SecurityRoleDto>> GetPersonRolesAsync(int personId);
}
