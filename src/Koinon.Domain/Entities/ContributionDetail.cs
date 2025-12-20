namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a line item allocation of a contribution to a specific fund.
/// </summary>
public class ContributionDetail : Entity
{
    /// <summary>
    /// Parent contribution (required).
    /// </summary>
    public required int ContributionId { get; set; }

    /// <summary>
    /// Fund for this allocation (required).
    /// </summary>
    public required int FundId { get; set; }

    /// <summary>
    /// Amount allocated to this fund.
    /// Can be negative for corrections/refunds.
    /// </summary>
    public required decimal Amount { get; set; }

    /// <summary>
    /// Optional notes for this line item.
    /// </summary>
    public string? Summary { get; set; }

    // Navigation properties

    /// <summary>
    /// Parent contribution.
    /// </summary>
    public virtual Contribution? Contribution { get; set; }

    /// <summary>
    /// Associated fund.
    /// </summary>
    public virtual Fund? Fund { get; set; }
}
