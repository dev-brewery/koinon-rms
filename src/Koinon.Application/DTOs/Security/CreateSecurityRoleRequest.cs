namespace Koinon.Application.DTOs.Security;

/// <summary>
/// Request payload for creating a new security role.
/// </summary>
public record CreateSecurityRoleRequest
{
    /// <summary>
    /// Gets the name of the new security role.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description of the new security role.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets a value indicating whether this role should be active on creation.
    /// Defaults to true.
    /// </summary>
    public bool IsActive { get; init; } = true;
}
