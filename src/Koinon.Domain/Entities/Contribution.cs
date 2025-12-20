namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a financial contribution/donation transaction.
/// Total amount is calculated from sum of ContributionDetail records.
/// </summary>
public class Contribution : Entity
{
    /// <summary>
    /// Donor's PersonAlias (nullable for anonymous contributions).
    /// </summary>
    public int? PersonAliasId { get; set; }

    /// <summary>
    /// Optional batch association (FK to ContributionBatch - entity coming in #253).
    /// </summary>
    public int? BatchId { get; set; }

    /// <summary>
    /// Date and time when the contribution occurred.
    /// </summary>
    public required DateTime TransactionDateTime { get; set; }

    /// <summary>
    /// Transaction reference (check number, confirmation code, etc.).
    /// Maximum 50 characters.
    /// </summary>
    public string? TransactionCode { get; set; }

    /// <summary>
    /// Transaction type (Cash, Check, Card, ACH) - FK to DefinedValue.
    /// </summary>
    public required int TransactionTypeValueId { get; set; }

    /// <summary>
    /// Source of the contribution (Website, Kiosk, Manual Entry) - FK to DefinedValue.
    /// </summary>
    public required int SourceTypeValueId { get; set; }

    /// <summary>
    /// Optional notes about the contribution.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Optional campus association.
    /// </summary>
    public int? CampusId { get; set; }

    // Navigation properties

    /// <summary>
    /// Donor's person alias.
    /// </summary>
    public virtual PersonAlias? PersonAlias { get; set; }

    /// <summary>
    /// Transaction type defined value.
    /// </summary>
    public virtual DefinedValue? TransactionTypeValue { get; set; }

    /// <summary>
    /// Source type defined value.
    /// </summary>
    public virtual DefinedValue? SourceTypeValue { get; set; }

    /// <summary>
    /// Associated campus.
    /// </summary>
    public virtual Campus? Campus { get; set; }

    /// <summary>
    /// Optional batch association.
    /// </summary>
    public virtual ContributionBatch? Batch { get; set; }

    /// <summary>
    /// Line items splitting the contribution across funds.
    /// </summary>
    public virtual ICollection<ContributionDetail> ContributionDetails { get; set; } = new List<ContributionDetail>();
}
