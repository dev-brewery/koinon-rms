namespace Koinon.Application.DTOs.Security;

/// <summary>
/// DTO representing a single security claim (permission definition).
/// Used in the admin UI to populate the role-claim picker.
/// </summary>
public record SecurityClaimDto
{
    /// <summary>
    /// Gets the encoded identifier for API responses.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Gets the type or category of the claim (e.g., "financial", "person").
    /// </summary>
    public required string ClaimType { get; init; }

    /// <summary>
    /// Gets the value associated with the claim type (e.g., "view", "edit").
    /// </summary>
    public required string ClaimValue { get; init; }

    /// <summary>
    /// Gets the human-readable description of what this claim permits.
    /// </summary>
    public string? Description { get; init; }
}
