using Koinon.Application.DTOs.Communication;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for communication analytics operations.
/// </summary>
public interface ICommunicationAnalyticsService
{
    /// <summary>
    /// Gets detailed analytics for a single communication.
    /// </summary>
    /// <param name="communicationIdKey">The communication's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Communication analytics or null if not found</returns>
    Task<CommunicationAnalyticsDto?> GetCommunicationAnalyticsAsync(
        string communicationIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Gets aggregate analytics summary for a time period.
    /// </summary>
    /// <param name="startDate">Start date of the period</param>
    /// <param name="endDate">End date of the period</param>
    /// <param name="type">Optional communication type filter (Email or Sms)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Analytics summary</returns>
    Task<AnalyticsSummaryDto> GetAnalyticsSummaryAsync(
        DateTime startDate,
        DateTime endDate,
        string? type = null,
        CancellationToken ct = default);

    /// <summary>
    /// Records an email open event for a recipient.
    /// </summary>
    /// <param name="recipientIdKey">The recipient's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    Task RecordOpenAsync(string recipientIdKey, CancellationToken ct = default);

    /// <summary>
    /// Records a link click event for a recipient.
    /// </summary>
    /// <param name="recipientIdKey">The recipient's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    Task RecordClickAsync(string recipientIdKey, CancellationToken ct = default);
}
