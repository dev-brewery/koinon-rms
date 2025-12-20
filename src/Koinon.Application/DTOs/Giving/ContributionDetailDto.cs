namespace Koinon.Application.DTOs.Giving;

/// <summary>
/// DTO representing a line item allocation of a contribution to a specific fund.
/// </summary>
public record ContributionDetailDto
{
    /// <summary>
    /// URL-safe IdKey for the contribution detail.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Fund IdKey for this allocation.
    /// </summary>
    public required string FundIdKey { get; init; }

    /// <summary>
    /// Fund name for display.
    /// </summary>
    public required string FundName { get; init; }

    /// <summary>
    /// Amount allocated to this fund.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Optional notes for this line item.
    /// </summary>
    public string? Summary { get; init; }
}
