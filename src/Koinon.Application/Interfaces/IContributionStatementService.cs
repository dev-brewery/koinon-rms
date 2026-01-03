using Koinon.Application.Common;
using Koinon.Application.DTOs.Giving;

namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for generating contribution statements with PDF output.
/// </summary>
public interface IContributionStatementService
{
    /// <summary>
    /// Gets a paginated list of all generated contribution statements.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of contribution statements.</returns>
    Task<Result<PagedResult<ContributionStatementDto>>> GetStatementsAsync(
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a specific contribution statement by IdKey.
    /// </summary>
    /// <param name="idKey">The statement IdKey.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The contribution statement if found.</returns>
    Task<Result<ContributionStatementDto>> GetStatementAsync(
        string idKey,
        CancellationToken ct = default);

    /// <summary>
    /// Previews a contribution statement without saving.
    /// </summary>
    /// <param name="request">The statement generation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Preview of the statement with all contribution details.</returns>
    Task<Result<StatementPreviewDto>> PreviewStatementAsync(
        GenerateStatementRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Generates and saves a contribution statement.
    /// </summary>
    /// <param name="request">The statement generation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The generated contribution statement.</returns>
    Task<Result<ContributionStatementDto>> GenerateStatementAsync(
        GenerateStatementRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a PDF for an existing contribution statement.
    /// </summary>
    /// <param name="statementIdKey">The statement IdKey.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>PDF file bytes.</returns>
    Task<Result<byte[]>> GenerateStatementPdfAsync(
        string statementIdKey,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a list of people eligible for statement generation based on criteria.
    /// </summary>
    /// <param name="request">The batch statement criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of eligible people with contribution totals.</returns>
    Task<Result<List<EligiblePersonDto>>> GetEligiblePeopleAsync(
        BatchStatementRequest request,
        CancellationToken ct = default);
}
