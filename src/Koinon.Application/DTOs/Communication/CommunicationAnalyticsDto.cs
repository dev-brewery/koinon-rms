namespace Koinon.Application.DTOs.Communication;

/// <summary>
/// Detailed analytics for a single communication.
/// </summary>
public record CommunicationAnalyticsDto
{
    /// <summary>
    /// The communication's IdKey.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// The type of communication (Email or SMS).
    /// </summary>
    public required string CommunicationType { get; init; }

    /// <summary>
    /// Total number of recipients.
    /// </summary>
    public required int TotalRecipients { get; init; }

    /// <summary>
    /// Number of recipients where the communication was sent.
    /// </summary>
    public required int Sent { get; init; }

    /// <summary>
    /// Number of recipients where the communication was delivered.
    /// </summary>
    public required int Delivered { get; init; }

    /// <summary>
    /// Number of recipients where delivery failed.
    /// </summary>
    public required int Failed { get; init; }

    /// <summary>
    /// Number of recipients who opened the communication (email only).
    /// </summary>
    public required int Opened { get; init; }

    /// <summary>
    /// Number of recipients who clicked links in the communication (email only).
    /// </summary>
    public required int Clicked { get; init; }

    /// <summary>
    /// Open rate as a percentage (0-100). Email only.
    /// </summary>
    public decimal OpenRate { get; init; }

    /// <summary>
    /// Click rate as a percentage (0-100). Email only.
    /// </summary>
    public decimal ClickRate { get; init; }

    /// <summary>
    /// Click-through rate (clicks / opens) as a percentage (0-100). Email only.
    /// </summary>
    public decimal ClickThroughRate { get; init; }

    /// <summary>
    /// Delivery rate as a percentage (0-100).
    /// </summary>
    public decimal DeliveryRate { get; init; }

    /// <summary>
    /// Breakdown of recipients by status.
    /// </summary>
    public required RecipientStatusBreakdownDto StatusBreakdown { get; init; }

    /// <summary>
    /// Date and time when the communication was sent.
    /// </summary>
    public DateTime? SentDateTime { get; init; }
}

/// <summary>
/// Breakdown of recipients by status.
/// </summary>
public record RecipientStatusBreakdownDto
{
    /// <summary>
    /// Number of recipients with pending status.
    /// </summary>
    public required int Pending { get; init; }

    /// <summary>
    /// Number of recipients with delivered status.
    /// </summary>
    public required int Delivered { get; init; }

    /// <summary>
    /// Number of recipients with failed status.
    /// </summary>
    public required int Failed { get; init; }

    /// <summary>
    /// Number of recipients who opened the communication.
    /// </summary>
    public required int Opened { get; init; }
}
