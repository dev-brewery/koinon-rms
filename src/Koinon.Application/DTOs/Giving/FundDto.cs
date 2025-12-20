namespace Koinon.Application.DTOs.Giving;

/// <summary>
/// DTO representing a fund for categorizing contributions.
/// </summary>
public record FundDto
{
    /// <summary>
    /// URL-safe IdKey for the fund.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Internal name for the fund.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Public display name for givers (uses Name if null).
    /// </summary>
    public string? PublicName { get; init; }

    /// <summary>
    /// Whether the fund is available for contributions.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Whether the fund is visible to online givers.
    /// </summary>
    public required bool IsPublic { get; init; }
}
