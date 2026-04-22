namespace Koinon.Application.DTOs.Security;

/// <summary>
/// Detailed DTO for a security role including its claims and members.
/// Used in the admin role-detail view.
/// </summary>
public record SecurityRoleDetailDto
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
    /// Gets a value indicating whether this is a system-defined role that cannot be deleted.
    /// </summary>
    public required bool IsSystemRole { get; init; }

    /// <summary>
    /// Gets a value indicating whether this role is active and can be assigned.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Gets the list of claims currently associated with this role.
    /// </summary>
    public required IReadOnlyList<RoleSecurityClaimDto> Claims { get; init; }

    /// <summary>
    /// Gets the list of people currently assigned to this role (non-expired assignments).
    /// </summary>
    public required IReadOnlyList<PersonSecurityRoleMemberDto> Members { get; init; }
}
