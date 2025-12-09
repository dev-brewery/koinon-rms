namespace Koinon.Application.DTOs.Communication;

/// <summary>
/// Aggregate analytics summary for a time period.
/// </summary>
public record AnalyticsSummaryDto
{
    /// <summary>
    /// Total number of communications in the period.
    /// </summary>
    public required int TotalCommunications { get; init; }

    /// <summary>
    /// Total number of recipients across all communications.
    /// </summary>
    public required int TotalRecipients { get; init; }

    /// <summary>
    /// Total number of recipients where communications were delivered.
    /// </summary>
    public required int TotalDelivered { get; init; }

    /// <summary>
    /// Total number of recipients where delivery failed.
    /// </summary>
    public required int TotalFailed { get; init; }

    /// <summary>
    /// Total number of recipients who opened communications (email only).
    /// </summary>
    public required int TotalOpened { get; init; }

    /// <summary>
    /// Total number of recipients who clicked links (email only).
    /// </summary>
    public required int TotalClicked { get; init; }

    /// <summary>
    /// Overall delivery rate as a percentage (0-100).
    /// </summary>
    public decimal DeliveryRate { get; init; }

    /// <summary>
    /// Overall open rate as a percentage (0-100). Email only.
    /// </summary>
    public decimal OpenRate { get; init; }

    /// <summary>
    /// Overall click rate as a percentage (0-100). Email only.
    /// </summary>
    public decimal ClickRate { get; init; }

    /// <summary>
    /// Breakdown by communication type.
    /// </summary>
    public required ByTypeBreakdownDto ByType { get; init; }

    /// <summary>
    /// Start date of the analytics period.
    /// </summary>
    public DateTime StartDate { get; init; }

    /// <summary>
    /// End date of the analytics period.
    /// </summary>
    public DateTime EndDate { get; init; }
}

/// <summary>
/// Breakdown of statistics by communication type.
/// </summary>
public record ByTypeBreakdownDto
{
    /// <summary>
    /// Email statistics.
    /// </summary>
    public required TypeStatsDto Email { get; init; }

    /// <summary>
    /// SMS statistics.
    /// </summary>
    public required TypeStatsDto Sms { get; init; }
}

/// <summary>
/// Statistics for a specific communication type.
/// </summary>
public record TypeStatsDto
{
    /// <summary>
    /// Number of communications of this type.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Total recipients for this type.
    /// </summary>
    public required int Recipients { get; init; }

    /// <summary>
    /// Total delivered for this type.
    /// </summary>
    public required int Delivered { get; init; }

    /// <summary>
    /// Total opened for this type (email only).
    /// </summary>
    public required int Opened { get; init; }

    /// <summary>
    /// Total clicked for this type (email only).
    /// </summary>
    public required int Clicked { get; init; }
}
