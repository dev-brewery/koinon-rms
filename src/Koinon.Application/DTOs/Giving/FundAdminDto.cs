namespace Koinon.Application.DTOs.Giving;

/// <summary>
/// Full DTO for fund administration, including audit fields and all configurable properties.
/// </summary>
public record FundAdminDto
{
    /// <summary>
    /// URL-safe IdKey for the fund.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Internal name for the fund.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Public display name for givers (uses Name if null).
    /// </summary>
    public string? PublicName { get; init; }

    /// <summary>
    /// Detailed description of the fund's purpose.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// General Ledger code for accounting integration.
    /// </summary>
    public string? GlCode { get; init; }

    /// <summary>
    /// Whether the fund is available for contributions.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Whether the fund is visible to online givers.
    /// </summary>
    public required bool IsPublic { get; init; }

    /// <summary>
    /// Whether contributions to this fund are tax-deductible.
    /// </summary>
    public required bool IsTaxDeductible { get; init; }

    /// <summary>
    /// Display order for UI sorting.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// When the fund was created.
    /// </summary>
    public required DateTime CreatedDateTime { get; init; }

    /// <summary>
    /// When the fund was last modified, if ever.
    /// </summary>
    public DateTime? ModifiedDateTime { get; init; }
}
