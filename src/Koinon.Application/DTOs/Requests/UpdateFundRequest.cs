namespace Koinon.Application.DTOs.Requests;

/// <summary>
/// Request payload for updating an existing fund.
/// </summary>
public record UpdateFundRequest
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
    /// Whether the fund is visible to online givers.
    /// </summary>
    public bool IsPublic { get; init; }

    /// <summary>
    /// Whether contributions to this fund are tax-deductible.
    /// </summary>
    public bool IsTaxDeductible { get; init; }
}
