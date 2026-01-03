namespace Koinon.Application.DTOs.Giving;

/// <summary>
/// DTO representing a generated contribution statement.
/// </summary>
public record ContributionStatementDto
{
    /// <summary>
    /// URL-safe IdKey for the statement.
    /// </summary>
    public required string IdKey { get; init; }

    /// <summary>
    /// Person IdKey this statement is for.
    /// </summary>
    public required string PersonIdKey { get; init; }

    /// <summary>
    /// Person's full name.
    /// </summary>
    public required string PersonName { get; init; }

    /// <summary>
    /// Start date of statement period (inclusive).
    /// </summary>
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// End date of statement period (inclusive).
    /// </summary>
    public required DateTime EndDate { get; init; }

    /// <summary>
    /// Total amount of tax-deductible contributions in the period.
    /// </summary>
    public required decimal TotalAmount { get; init; }

    /// <summary>
    /// Number of contributions included in this statement.
    /// </summary>
    public required int ContributionCount { get; init; }

    /// <summary>
    /// When the statement was generated.
    /// </summary>
    public required DateTime GeneratedDateTime { get; init; }
}

/// <summary>
/// DTO representing a contribution line item in a statement.
/// </summary>
public record StatementContributionDto
{
    /// <summary>
    /// Date of the contribution.
    /// </summary>
    public required DateTime Date { get; init; }

    /// <summary>
    /// Fund name.
    /// </summary>
    public required string FundName { get; init; }

    /// <summary>
    /// Amount allocated to this fund.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Check number or transaction code.
    /// </summary>
    public string? CheckNumber { get; init; }
}

/// <summary>
/// Request to generate a contribution statement.
/// </summary>
public record GenerateStatementRequest
{
    /// <summary>
    /// Person IdKey to generate statement for.
    /// </summary>
    public required string PersonIdKey { get; init; }

    /// <summary>
    /// Start date of statement period (inclusive).
    /// </summary>
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// End date of statement period (inclusive).
    /// </summary>
    public required DateTime EndDate { get; init; }
}

/// <summary>
/// Request for batch statement generation criteria.
/// </summary>
public record BatchStatementRequest
{
    /// <summary>
    /// Start date of statement period (inclusive).
    /// </summary>
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// End date of statement period (inclusive).
    /// </summary>
    public required DateTime EndDate { get; init; }

    /// <summary>
    /// Minimum contribution amount to include a person.
    /// </summary>
    public decimal MinimumAmount { get; init; } = 0;
}

/// <summary>
/// DTO for previewing a statement before generating.
/// </summary>
public record StatementPreviewDto
{
    /// <summary>
    /// Person IdKey.
    /// </summary>
    public required string PersonIdKey { get; init; }

    /// <summary>
    /// Person's full name.
    /// </summary>
    public required string PersonName { get; init; }

    /// <summary>
    /// Person's mailing address.
    /// </summary>
    public required string PersonAddress { get; init; }

    /// <summary>
    /// Start date of statement period (inclusive).
    /// </summary>
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// End date of statement period (inclusive).
    /// </summary>
    public required DateTime EndDate { get; init; }

    /// <summary>
    /// Total amount of tax-deductible contributions.
    /// </summary>
    public required decimal TotalAmount { get; init; }

    /// <summary>
    /// List of contribution line items.
    /// </summary>
    public required List<StatementContributionDto> Contributions { get; init; }

    /// <summary>
    /// Church name for the statement header.
    /// </summary>
    public required string ChurchName { get; init; }

    /// <summary>
    /// Church address for the statement header.
    /// </summary>
    public required string ChurchAddress { get; init; }
}

/// <summary>
/// DTO representing a person eligible for statement generation.
/// </summary>
public record EligiblePersonDto
{
    /// <summary>
    /// Person IdKey.
    /// </summary>
    public required string PersonIdKey { get; init; }

    /// <summary>
    /// Person's full name.
    /// </summary>
    public required string PersonName { get; init; }

    /// <summary>
    /// Total contribution amount in the period.
    /// </summary>
    public required decimal TotalAmount { get; init; }

    /// <summary>
    /// Number of contributions in the period.
    /// </summary>
    public required int ContributionCount { get; init; }
}
