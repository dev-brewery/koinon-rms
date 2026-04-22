namespace Koinon.Application.DTOs.Security;

/// <summary>
/// DTO representing a person assigned to a security role.
/// Used by the admin UI to list role members.
/// </summary>
public record PersonSecurityRoleMemberDto
{
    /// <summary>
    /// Gets the encoded identifier of the person.
    /// </summary>
    public required string PersonIdKey { get; init; }

    /// <summary>
    /// Gets the full display name of the person.
    /// </summary>
    public required string PersonName { get; init; }

    /// <summary>
    /// Gets the email address of the person, if any.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets the date and time when this role assignment expires, if applicable.
    /// </summary>
    public DateTime? ExpiresDateTime { get; init; }
}
