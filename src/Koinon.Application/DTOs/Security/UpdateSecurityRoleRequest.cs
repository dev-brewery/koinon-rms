namespace Koinon.Application.DTOs.Security;

/// <summary>
/// Request payload for updating an existing security role's mutable fields.
/// </summary>
public record UpdateSecurityRoleRequest
{
    /// <summary>
    /// Gets the updated name of the security role.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the updated description of the security role.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets a value indicating whether this role is active and can be assigned.
    /// </summary>
    public required bool IsActive { get; init; }
}
