namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request payload for creating a new fund.
/// </summary>
public record CreateFundRequest
{
    /// <summary>
    /// Internal name for the fund. Required, max 100 characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Public display name for givers. Optional, max 100 characters.
    /// </summary>
    public string? PublicName { get; init; }

    /// <summary>
    /// Detailed description of the fund's purpose.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// General Ledger code for accounting integration. Optional, max 50 characters.
    /// </summary>
    public string? GlCode { get; init; }

    /// <summary>
    /// Whether the fund is visible to online givers. Defaults to true.
    /// </summary>
    public bool IsPublic { get; init; } = true;

    /// <summary>
    /// Whether contributions to this fund are tax-deductible. Defaults to true.
    /// </summary>
    public bool IsTaxDeductible { get; init; } = true;
}
