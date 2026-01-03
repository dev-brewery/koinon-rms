namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a generated contribution statement for a person's giving history.
/// Statements summarize tax-deductible contributions over a specific time period.
/// </summary>
public class ContributionStatement : Entity
{
    /// <summary>
    /// Person this statement is for (required).
    /// </summary>
    public int PersonId { get; set; }

    /// <summary>
    /// Start date of statement period (inclusive).
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of statement period (inclusive).
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Total amount of tax-deductible contributions in the period.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Number of contributions included in this statement.
    /// </summary>
    public int ContributionCount { get; set; }

    /// <summary>
    /// When the statement was generated.
    /// </summary>
    public DateTime GeneratedDateTime { get; set; }

    /// <summary>
    /// Binary file containing the PDF (optional - may be generated on demand).
    /// </summary>
    public int? BinaryFileId { get; set; }

    // Navigation properties
    /// <summary>
    /// The person this statement belongs to.
    /// </summary>
    public virtual Person Person { get; set; } = null!;

    /// <summary>
    /// The binary file containing the generated PDF statement.
    /// </summary>
    public virtual BinaryFile? BinaryFile { get; set; }
}
