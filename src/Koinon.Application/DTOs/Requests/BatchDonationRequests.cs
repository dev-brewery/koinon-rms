namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request to create a new contribution batch.
/// </summary>
public record CreateBatchRequest
{
    /// <summary>
    /// Name of the batch.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Date when the batch was created or intended for posting.
    /// </summary>
    public required DateTime BatchDate { get; init; }

    /// <summary>
    /// Expected total amount for reconciliation purposes.
    /// </summary>
    public decimal? ControlAmount { get; init; }

    /// <summary>
    /// Expected number of items for reconciliation purposes.
    /// </summary>
    public int? ControlItemCount { get; init; }

    /// <summary>
    /// Associated campus IdKey.
    /// </summary>
    public string? CampusIdKey { get; init; }

    /// <summary>
    /// Optional notes about the batch.
    /// </summary>
    public string? Note { get; init; }
}

/// <summary>
/// Request to add a contribution to a batch.
/// </summary>
public record AddContributionRequest
{
    /// <summary>
    /// Donor's person IdKey (null for anonymous contributions).
    /// </summary>
    public string? PersonIdKey { get; init; }

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
    /// Line items splitting the contribution across funds.
    /// </summary>
    public required List<ContributionDetailRequest> Details { get; init; }

    /// <summary>
    /// Optional notes about the contribution.
    /// </summary>
    public string? Summary { get; init; }
}

/// <summary>
/// Request for a contribution detail line item.
/// </summary>
public record ContributionDetailRequest
{
    /// <summary>
    /// Fund IdKey for this allocation.
    /// </summary>
    public required string FundIdKey { get; init; }

    /// <summary>
    /// Amount allocated to this fund.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Optional notes for this line item.
    /// </summary>
    public string? Summary { get; init; }
}

/// <summary>
/// Request to update an existing contribution.
/// </summary>
public record UpdateContributionRequest
{
    /// <summary>
    /// Donor's person IdKey (null for anonymous contributions).
    /// </summary>
    public string? PersonIdKey { get; init; }

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
    /// Line items splitting the contribution across funds.
    /// </summary>
    public required List<ContributionDetailRequest> Details { get; init; }

    /// <summary>
    /// Optional notes about the contribution.
    /// </summary>
    public string? Summary { get; init; }
}
