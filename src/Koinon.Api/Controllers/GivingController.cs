using Koinon.Api.Authorization;
using Koinon.Api.Filters;
using Koinon.Application.Common;
using Koinon.Application.DTOs.Giving;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for financial giving operations including batches, contributions, and funds.
/// Provides endpoints for batch donation entry and reconciliation.
/// </summary>
[ApiController]
[Route("api/v1/giving")]
[Authorize]
public class GivingController(
    IBatchDonationEntryService batchDonationService,
    IContributionStatementService statementService,
    ILogger<GivingController> logger) : ControllerBase
{
    #region Funds

    /// <summary>
    /// Gets all active funds for contribution entry.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of active funds</returns>
    /// <response code="200">Returns list of active funds</response>
    [HttpGet("funds")]
    [RequiresClaim("financial", "view")]
    [ProducesResponseType(typeof(IReadOnlyList<FundDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFundsAsync(CancellationToken ct = default)
    {
        var funds = await batchDonationService.GetActiveFundsAsync(ct);

        logger.LogInformation("Retrieved {Count} active funds", funds.Count);

        return Ok(new { data = funds });
    }

    /// <summary>
    /// Gets a fund by its IdKey.
    /// </summary>
    /// <param name="idKey">The fund IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The fund if found</returns>
    /// <response code="200">Returns the fund</response>
    /// <response code="404">Fund not found</response>
    [HttpGet("funds/{idKey}")]
    [ValidateIdKey]
    [RequiresClaim("financial", "view")]
    [ProducesResponseType(typeof(FundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFundAsync(string idKey, CancellationToken ct = default)
    {
        var result = await batchDonationService.GetFundAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Fund not found: {IdKey}", idKey);
            return Problem(
                detail: result.Error?.Message ?? "Fund not found",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found"
            );
        }

        logger.LogInformation("Retrieved fund: {IdKey}", idKey);

        return Ok(new { data = result.Value });
    }

    #endregion

    #region Batches

    /// <summary>
    /// Gets a paginated list of batches with optional filters.
    /// </summary>
    /// <param name="filter">Filter and pagination parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of batches</returns>
    /// <response code="200">Returns paginated list of batches</response>
    [HttpGet("batches")]
    [RequiresClaim("financial", "view")]
    [ProducesResponseType(typeof(PagedResult<ContributionBatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBatchesAsync([FromQuery] BatchFilterRequest filter, CancellationToken ct = default)
    {
        var result = await batchDonationService.GetBatchesAsync(filter, ct);

        logger.LogInformation(
            "Batches search completed: Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            result.Page, result.PageSize, result.TotalCount);

        return Ok(new
        {
            data = result.Items,
            meta = new
            {
                page = result.Page,
                pageSize = result.PageSize,
                totalCount = result.TotalCount,
                totalPages = (int)Math.Ceiling(result.TotalCount / (double)result.PageSize)
            }
        });
    }

    /// <summary>
    /// Gets a batch by its IdKey.
    /// </summary>
    /// <param name="idKey">The batch IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The batch if found</returns>
    /// <response code="200">Returns the batch</response>
    /// <response code="404">Batch not found</response>
    [HttpGet("batches/{idKey}")]
    [ValidateIdKey]
    [RequiresClaim("financial", "view")]
    [ProducesResponseType(typeof(ContributionBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatchAsync(string idKey, CancellationToken ct = default)
    {
        var result = await batchDonationService.GetBatchAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Batch not found: {IdKey}", idKey);
            return Problem(
                detail: result.Error?.Message ?? "Batch not found",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found"
            );
        }

        logger.LogInformation("Retrieved batch: {IdKey}", idKey);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Gets a batch summary with reconciliation status.
    /// </summary>
    /// <param name="idKey">The batch IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The batch summary with variance calculation</returns>
    /// <response code="200">Returns the batch summary</response>
    /// <response code="404">Batch not found</response>
    [HttpGet("batches/{idKey}/summary")]
    [ValidateIdKey]
    [RequiresClaim("financial", "view")]
    [ProducesResponseType(typeof(BatchSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatchSummaryAsync(string idKey, CancellationToken ct = default)
    {
        var result = await batchDonationService.GetBatchSummaryAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Batch not found for summary: {IdKey}", idKey);
            return Problem(
                detail: result.Error?.Message ?? "Batch not found",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found"
            );
        }

        logger.LogInformation("Retrieved batch summary: {IdKey}", idKey);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Creates a new contribution batch.
    /// </summary>
    /// <param name="request">The batch creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created batch</returns>
    /// <response code="201">Batch created successfully</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("batches")]
    [RequiresClaim("financial", "edit")]
    [ProducesResponseType(typeof(ContributionBatchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBatchAsync([FromBody] CreateBatchRequest request, CancellationToken ct = default)
    {
        var result = await batchDonationService.CreateBatchAsync(request, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to create batch: {Error}", result.Error);
            return Problem(
                detail: result.Error?.Message ?? "Failed to create batch",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request"
            );
        }

        logger.LogInformation("Created batch: {IdKey}", result.Value!.IdKey);

        return CreatedAtAction(
            nameof(GetBatchAsync),
            new { idKey = result.Value.IdKey },
            result.Value);
    }

    /// <summary>
    /// Opens a batch for editing (no-op if already open, error if closed/posted).
    /// </summary>
    /// <param name="idKey">The batch IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    /// <response code="200">Batch opened successfully</response>
    /// <response code="400">Cannot open batch (already closed or posted)</response>
    /// <response code="404">Batch not found</response>
    [HttpPost("batches/{idKey}/open")]
    [ValidateIdKey]
    [RequiresClaim("financial", "edit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OpenBatchAsync(string idKey, CancellationToken ct = default)
    {
        var result = await batchDonationService.OpenBatchAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to open batch {IdKey}: {Error}", idKey, result.Error);

            if (result.Error?.Code == "NOT_FOUND")
            {
                return Problem(
                    detail: result.Error?.Message ?? "Batch not found",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found"
                );
            }

            return Problem(
                detail: result.Error?.Message ?? "Failed to open batch",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request"
            );
        }

        logger.LogInformation("Opened batch: {IdKey}", idKey);

        return Ok(new { message = "Batch opened successfully" });
    }

    /// <summary>
    /// Closes a batch (can only close open batches).
    /// </summary>
    /// <param name="idKey">The batch IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    /// <response code="200">Batch closed successfully</response>
    /// <response code="400">Cannot close batch (not open)</response>
    /// <response code="404">Batch not found</response>
    [HttpPost("batches/{idKey}/close")]
    [ValidateIdKey]
    [RequiresClaim("financial", "batch.close")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloseBatchAsync(string idKey, CancellationToken ct = default)
    {
        var result = await batchDonationService.CloseBatchAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to close batch {IdKey}: {Error}", idKey, result.Error);

            if (result.Error?.Code == "NOT_FOUND")
            {
                return Problem(
                    detail: result.Error?.Message ?? "Batch not found",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found"
                );
            }

            return Problem(
                detail: result.Error?.Message ?? "Failed to close batch",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request"
            );
        }

        logger.LogInformation("Closed batch: {IdKey}", idKey);

        return Ok(new { message = "Batch closed successfully" });
    }

    #endregion

    #region Contributions

    /// <summary>
    /// Gets all contributions in a batch.
    /// </summary>
    /// <param name="batchIdKey">The batch IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of contributions in the batch</returns>
    /// <response code="200">Returns list of contributions</response>
    /// <response code="404">Batch not found</response>
    [HttpGet("batches/{batchIdKey}/contributions")]
    [ValidateIdKey]
    [RequiresClaim("financial", "view")]
    [ProducesResponseType(typeof(IReadOnlyList<ContributionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatchContributionsAsync(string batchIdKey, CancellationToken ct = default)
    {
        var result = await batchDonationService.GetBatchContributionsAsync(batchIdKey, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to get contributions for batch {BatchIdKey}: {Error}", batchIdKey, result.Error);
            return Problem(
                detail: result.Error?.Message ?? "Batch not found",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found"
            );
        }

        logger.LogInformation("Retrieved {Count} contributions for batch {BatchIdKey}", result.Value!.Count, batchIdKey);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Adds a contribution to a batch (only allowed for open batches).
    /// </summary>
    /// <param name="batchIdKey">The batch IdKey</param>
    /// <param name="request">The contribution request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created contribution</returns>
    /// <response code="201">Contribution created successfully</response>
    /// <response code="400">Invalid request or batch not open</response>
    /// <response code="404">Batch not found</response>
    [HttpPost("batches/{batchIdKey}/contributions")]
    [ValidateIdKey]
    [RequiresClaim("financial", "edit")]
    [ProducesResponseType(typeof(ContributionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddContributionAsync(
        string batchIdKey,
        [FromBody] AddContributionRequest request,
        CancellationToken ct = default)
    {
        var result = await batchDonationService.AddContributionAsync(batchIdKey, request, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to add contribution to batch {BatchIdKey}: {Error}", batchIdKey, result.Error);

            if (result.Error?.Code == "NOT_FOUND")
            {
                return Problem(
                    detail: result.Error?.Message ?? "Batch not found",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found"
                );
            }

            return Problem(
                detail: result.Error?.Message ?? "Failed to add contribution",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request"
            );
        }

        logger.LogInformation("Added contribution {IdKey} to batch {BatchIdKey}", result.Value!.IdKey, batchIdKey);

        return CreatedAtAction(
            nameof(GetContributionAsync),
            new { idKey = result.Value.IdKey },
            result.Value);
    }

    /// <summary>
    /// Gets a contribution by its IdKey.
    /// </summary>
    /// <param name="idKey">The contribution IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The contribution if found</returns>
    /// <response code="200">Returns the contribution</response>
    /// <response code="404">Contribution not found</response>
    [HttpGet("contributions/{idKey}")]
    [ValidateIdKey]
    [RequiresClaim("financial", "view")]
    [ProducesResponseType(typeof(ContributionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContributionAsync(string idKey, CancellationToken ct = default)
    {
        var result = await batchDonationService.GetContributionAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Contribution not found: {IdKey}", idKey);
            return Problem(
                detail: result.Error?.Message ?? "Contribution not found",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found"
            );
        }

        logger.LogInformation("Retrieved contribution: {IdKey}", idKey);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Updates an existing contribution (only allowed if parent batch is open).
    /// </summary>
    /// <param name="idKey">The contribution IdKey</param>
    /// <param name="request">The update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The updated contribution</returns>
    /// <response code="200">Contribution updated successfully</response>
    /// <response code="400">Invalid request or batch not open</response>
    /// <response code="404">Contribution not found</response>
    [HttpPut("contributions/{idKey}")]
    [ValidateIdKey]
    [RequiresClaim("financial", "edit")]
    [ProducesResponseType(typeof(ContributionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContributionAsync(
        string idKey,
        [FromBody] UpdateContributionRequest request,
        CancellationToken ct = default)
    {
        var result = await batchDonationService.UpdateContributionAsync(idKey, request, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to update contribution {IdKey}: {Error}", idKey, result.Error);

            if (result.Error?.Code == "NOT_FOUND")
            {
                return Problem(
                    detail: result.Error?.Message ?? "Contribution not found",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found"
                );
            }

            return Problem(
                detail: result.Error?.Message ?? "Failed to update contribution",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request"
            );
        }

        logger.LogInformation("Updated contribution: {IdKey}", idKey);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Deletes a contribution (only allowed if parent batch is open).
    /// </summary>
    /// <param name="idKey">The contribution IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Contribution deleted successfully</response>
    /// <response code="400">Batch not open</response>
    /// <response code="404">Contribution not found</response>
    [HttpDelete("contributions/{idKey}")]
    [ValidateIdKey]
    [RequiresClaim("financial", "edit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteContributionAsync(string idKey, CancellationToken ct = default)
    {
        var result = await batchDonationService.DeleteContributionAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to delete contribution {IdKey}: {Error}", idKey, result.Error);

            if (result.Error?.Code == "NOT_FOUND")
            {
                return Problem(
                    detail: result.Error?.Message ?? "Contribution not found",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found"
                );
            }

            return Problem(
                detail: result.Error?.Message ?? "Failed to delete contribution",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request"
            );
        }

        logger.LogInformation("Deleted contribution: {IdKey}", idKey);

        return NoContent();
    }

    #endregion

    #region Contribution Statements

    /// <summary>
    /// Gets a paginated list of all generated contribution statements.
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 25, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of contribution statements</returns>
    /// <response code="200">Returns list of contribution statements</response>
    [HttpGet("statements")]
    [RequiresClaim("financial", "view")]
    [ProducesResponseType(typeof(PagedResult<ContributionStatementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatementsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        // Validate pagination parameters
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1 || pageSize > 100)
        {
            pageSize = 25;
        }

        var result = await statementService.GetStatementsAsync(page, pageSize, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to get statements: {Error}", result.Error);
            return Problem(
                detail: result.Error?.Message ?? "Failed to get statements",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request"
            );
        }

        var statements = result.Value!;

        logger.LogInformation(
            "Statements search completed: Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            statements.Page, statements.PageSize, statements.TotalCount);

        return Ok(new
        {
            data = statements.Items,
            meta = new
            {
                page = statements.Page,
                pageSize = statements.PageSize,
                totalCount = statements.TotalCount,
                totalPages = (int)Math.Ceiling(statements.TotalCount / (double)statements.PageSize)
            }
        });
    }

    /// <summary>
    /// Gets a specific contribution statement by IdKey.
    /// </summary>
    /// <param name="idKey">The statement IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The contribution statement if found</returns>
    /// <response code="200">Returns the contribution statement</response>
    /// <response code="404">Statement not found</response>
    [HttpGet("statements/{idKey}")]
    [ValidateIdKey]
    [RequiresClaim("financial", "view")]
    [ProducesResponseType(typeof(ContributionStatementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatementAsync(string idKey, CancellationToken ct = default)
    {
        var result = await statementService.GetStatementAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Statement not found: {IdKey}", idKey);
            return Problem(
                detail: result.Error?.Message ?? "Statement not found",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found"
            );
        }

        logger.LogInformation("Retrieved statement: {IdKey}", idKey);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Previews a contribution statement without saving.
    /// </summary>
    /// <param name="request">The statement generation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Preview of the statement with all contribution details</returns>
    /// <response code="200">Returns statement preview</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Person not found</response>
    [HttpPost("statements/preview")]
    [RequiresClaim("financial", "view")]
    [ProducesResponseType(typeof(StatementPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewStatementAsync(
        [FromBody] GenerateStatementRequest request,
        CancellationToken ct = default)
    {
        var result = await statementService.PreviewStatementAsync(request, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to preview statement: {Error}", result.Error);

            if (result.Error?.Code == "NOT_FOUND")
            {
                return Problem(
                    detail: result.Error?.Message ?? "Person not found",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found"
                );
            }

            return Problem(
                detail: result.Error?.Message ?? "Failed to preview statement",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request"
            );
        }

        logger.LogInformation(
            "Previewed statement for person: {PersonIdKey}, Period: {StartDate} to {EndDate}",
            request.PersonIdKey, request.StartDate, request.EndDate);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Generates and saves a contribution statement.
    /// </summary>
    /// <param name="request">The statement generation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The generated contribution statement</returns>
    /// <response code="201">Statement created successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Person not found</response>
    [HttpPost("statements")]
    [RequiresClaim("financial", "edit")]
    [ProducesResponseType(typeof(ContributionStatementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateStatementAsync(
        [FromBody] GenerateStatementRequest request,
        CancellationToken ct = default)
    {
        var result = await statementService.GenerateStatementAsync(request, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to generate statement: {Error}", result.Error);

            if (result.Error?.Code == "NOT_FOUND")
            {
                return Problem(
                    detail: result.Error?.Message ?? "Person not found",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found"
                );
            }

            return Problem(
                detail: result.Error?.Message ?? "Failed to generate statement",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request"
            );
        }

        logger.LogInformation(
            "Generated statement {IdKey} for person: {PersonIdKey}",
            result.Value!.IdKey, request.PersonIdKey);

        return CreatedAtAction(
            nameof(GetStatementAsync),
            new { idKey = result.Value.IdKey },
            new { data = result.Value });
    }

    /// <summary>
    /// Downloads a contribution statement as PDF.
    /// </summary>
    /// <param name="idKey">The statement IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>PDF file</returns>
    /// <response code="200">Returns PDF file</response>
    /// <response code="404">Statement not found</response>
    [HttpGet("statements/{idKey}/pdf")]
    [ValidateIdKey]
    [RequiresClaim("financial", "view")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatementPdfAsync(string idKey, CancellationToken ct = default)
    {
        var result = await statementService.GenerateStatementPdfAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to generate PDF for statement {IdKey}: {Error}", idKey, result.Error);
            return Problem(
                detail: result.Error?.Message ?? "Statement not found",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found"
            );
        }

        logger.LogInformation("Generated PDF for statement: {IdKey}", idKey);

        return File(
            result.Value!,
            "application/pdf",
            $"contribution-statement-{idKey}.pdf");
    }

    /// <summary>
    /// Gets a list of people eligible for statement generation based on criteria.
    /// </summary>
    /// <param name="startDate">Start date of statement period (inclusive)</param>
    /// <param name="endDate">End date of statement period (inclusive)</param>
    /// <param name="minimumAmount">Minimum contribution amount to include (default: 0)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of eligible people with contribution totals</returns>
    /// <response code="200">Returns list of eligible people</response>
    /// <response code="400">Invalid date range</response>
    [HttpGet("statements/eligible")]
    [RequiresClaim("financial", "view")]
    [ProducesResponseType(typeof(List<EligiblePersonDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEligiblePeopleAsync(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] decimal minimumAmount = 0,
        CancellationToken ct = default)
    {
        var request = new BatchStatementRequest
        {
            StartDate = startDate,
            EndDate = endDate,
            MinimumAmount = minimumAmount
        };

        var result = await statementService.GetEligiblePeopleAsync(request, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to get eligible people: {Error}", result.Error);
            return Problem(
                detail: result.Error?.Message ?? "Failed to get eligible people",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request"
            );
        }

        logger.LogInformation(
            "Retrieved {Count} eligible people for period: {StartDate} to {EndDate}",
            result.Value!.Count, startDate, endDate);

        return Ok(new { data = result.Value });
    }

    #endregion
}
