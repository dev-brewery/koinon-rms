namespace Koinon.Application.DTOs.Giving;

/// <summary>
/// DTO representing a person's giving summary including YTD total and recent contributions.
/// </summary>
public record PersonGivingSummaryDto
{
    /// <summary>
    /// Sum of all contributions in the current calendar year.
    /// </summary>
    public required decimal YearToDateTotal { get; init; }

    /// <summary>
    /// Date and time of the most recent contribution, or null if no contributions exist.
    /// </summary>
    public DateTime? LastContributionDate { get; init; }

    /// <summary>
    /// The 10 most recent contributions, ordered by transaction date descending.
    /// </summary>
    public required List<RecentContributionDto> RecentContributions { get; init; }
}

/// <summary>
/// DTO representing a single recent contribution line item.
/// </summary>
public record RecentContributionDto
{
    /// <summary>
    /// URL-safe IdKey for the contribution detail record.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Date and time when the contribution occurred.
    /// </summary>
    public required DateTime TransactionDateTime { get; init; }

    /// <summary>
    /// Amount allocated to the fund for this line item.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Name of the fund this contribution was allocated to.
    /// </summary>
    public required string FundName { get; init; }

    /// <summary>
    /// Transaction type label (Cash, Check, Card, ACH), or null if not set.
    /// </summary>
    public string? TransactionType { get; init; }
}
