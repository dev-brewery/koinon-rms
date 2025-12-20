namespace Koinon.Application.DTOs.Giving;

/// <summary>
/// DTO representing a financial contribution/donation.
/// </summary>
public record ContributionDto
{
    /// <summary>
    /// URL-safe IdKey for the contribution.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Donor's person IdKey (null for anonymous contributions).
    /// </summary>
    public string? PersonIdKey { get; init; }

    /// <summary>
    /// Donor's full name (null for anonymous contributions).
    /// </summary>
    public string? PersonName { get; init; }

    /// <summary>
    /// Associated batch IdKey.
    /// </summary>
    public string? BatchIdKey { get; init; }

    /// <summary>
    /// Date and time when the contribution occurred.
    /// </summary>
    public required DateTime TransactionDateTime { get; init; }

    /// <summary>
    /// Transaction reference (check number, confirmation code, etc.).
    /// </summary>
    public string? TransactionCode { get; init; }

    /// <summary>
    /// Transaction type IdKey (Cash, Check, Card, ACH).
    /// </summary>
    public required string TransactionTypeValueIdKey { get; init; }

    /// <summary>
    /// Source type IdKey (Website, Kiosk, Manual Entry).
    /// </summary>
    public required string SourceTypeValueIdKey { get; init; }

    /// <summary>
    /// Optional notes about the contribution.
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>
    /// Associated campus IdKey.
    /// </summary>
    public string? CampusIdKey { get; init; }

    /// <summary>
    /// Line items splitting the contribution across funds.
    /// </summary>
    public required List<ContributionDetailDto> Details { get; init; }

    /// <summary>
    /// Total amount of the contribution (sum of all details).
    /// </summary>
    public required decimal TotalAmount { get; init; }
}
