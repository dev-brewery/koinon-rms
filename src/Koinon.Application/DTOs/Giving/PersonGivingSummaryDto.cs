namespace Koinon.Application.DTOs.Giving;

/// <summary>
/// Summary of a person's giving activity.
/// </summary>
public record PersonGivingSummaryDto
{
    public required decimal YearToDateTotal { get; init; }
    public DateTime? LastContributionDate { get; init; }
    public required List<RecentContributionDto> RecentContributions { get; init; }
}

/// <summary>
/// A single contribution detail line item for the giving history view.
/// </summary>
public record RecentContributionDto
{
    public required string IdKey { get; init; }
    public required DateTime TransactionDateTime { get; init; }
    public required decimal Amount { get; init; }
    public required string FundName { get; init; }
    public string? TransactionType { get; init; }
}
