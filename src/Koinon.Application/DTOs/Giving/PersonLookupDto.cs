namespace Koinon.Application.DTOs.Giving;

/// <summary>
/// DTO for person lookup results in contribution entry.
/// </summary>
public record PersonLookupDto
{
    /// <summary>
    /// URL-safe IdKey for the person.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Full name of the person.
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// Email address (for display/verification).
    /// </summary>
    public string? Email { get; init; }
}
