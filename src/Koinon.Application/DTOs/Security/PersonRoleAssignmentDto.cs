namespace Koinon.Application.DTOs.Security;

/// <summary>
/// Represents a security role assigned to a person, including assignment expiration.
/// Used by <see cref="Interfaces.ISecurityClaimService.GetPersonRolesAsync(int)"/> to describe
/// which roles a given person holds.
/// </summary>
public record PersonRoleAssignmentDto
{
    /// <summary>
    /// Gets the encoded identifier for API responses.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Gets the name of the security role.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description of the security role.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets a value indicating whether this role is active.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Gets the date and time when this role assignment expires, if applicable.
    /// </summary>
    public DateTime? ExpiresDateTime { get; init; }
}
