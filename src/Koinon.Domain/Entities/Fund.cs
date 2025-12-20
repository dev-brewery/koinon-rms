namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a fund for categorizing contributions (General Fund, Building Fund, etc.).
/// Supports hierarchical fund structures and campaign-based funds with date ranges.
/// </summary>
public class Fund : Entity
{
    /// <summary>
    /// Internal name for the fund.
    /// Maximum 100 characters.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Public display name for givers (optional, uses Name if null).
    /// Maximum 100 characters.
    /// </summary>
    public string? PublicName { get; set; }

    /// <summary>
    /// Detailed description of the fund's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// General Ledger code for accounting integration.
    /// Maximum 50 characters.
    /// </summary>
    public string? GlCode { get; set; }

    /// <summary>
    /// Whether contributions to this fund are tax-deductible.
    /// </summary>
    public bool IsTaxDeductible { get; set; } = true;

    /// <summary>
    /// Whether the fund is available for contributions.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the fund is visible to online givers.
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// Campaign start date (optional, for time-limited funds).
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Campaign end date (optional, for time-limited funds).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Display order for UI sorting.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Parent fund ID for hierarchical structures.
    /// </summary>
    public int? ParentFundId { get; set; }

    /// <summary>
    /// Optional campus association for campus-specific funds.
    /// </summary>
    public int? CampusId { get; set; }

    // Navigation properties

    /// <summary>
    /// Parent fund in hierarchy.
    /// </summary>
    public virtual Fund? ParentFund { get; set; }

    /// <summary>
    /// Child funds in hierarchy.
    /// </summary>
    public virtual ICollection<Fund> ChildFunds { get; set; } = new List<Fund>();

    /// <summary>
    /// Associated campus.
    /// </summary>
    public virtual Campus? Campus { get; set; }
}
