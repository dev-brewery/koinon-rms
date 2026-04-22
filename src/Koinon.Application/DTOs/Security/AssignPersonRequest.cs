namespace Koinon.Application.DTOs.Security;

/// <summary>
/// Request payload to add a person to a security role, optionally
/// scheduling the assignment to expire at a given date and time.
/// </summary>
public record AssignPersonRequest
{
    /// <summary>
    /// Gets the encoded identifier of the person to add.
    /// </summary>
    public required string PersonIdKey { get; init; }

    /// <summary>
    /// Gets the optional date and time when this role assignment expires.
    /// If null, the assignment does not expire.
    /// </summary>
    public DateTime? ExpiresDateTime { get; init; }
}
