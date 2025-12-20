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
    // TODO(#258): Add [RequiresClaim("financial", "view")] when claims-based auth is implemented
    [ProducesResponseType(typeof(IReadOnlyList<FundDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFundsAsync(CancellationToken ct = default)
    {
        var funds = await batchDonationService.GetActiveFundsAsync(ct);

        logger.LogInformation("Retrieved {Count} active funds", funds.Count);

        return Ok(funds);
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
    // TODO(#258): Add [RequiresClaim("financial", "view")] when claims-based auth is implemented
    [ProducesResponseType(typeof(FundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFundAsync(string idKey, CancellationToken ct = default)
    {
        var result = await batchDonationService.GetFundAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Fund not found: {IdKey}", idKey);
            return NotFound(new { error = result.Error });
        }

        logger.LogInformation("Retrieved fund: {IdKey}", idKey);

        return Ok(result.Value);
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
    // TODO(#258): Add [RequiresClaim("financial", "view")] when claims-based auth is implemented
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
    // TODO(#258): Add [RequiresClaim("financial", "view")] when claims-based auth is implemented
    [ProducesResponseType(typeof(ContributionBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatchAsync(string idKey, CancellationToken ct = default)
    {
        var result = await batchDonationService.GetBatchAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Batch not found: {IdKey}", idKey);
            return NotFound(new { error = result.Error });
        }

        logger.LogInformation("Retrieved batch: {IdKey}", idKey);

        return Ok(result.Value);
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
    // TODO(#258): Add [RequiresClaim("financial", "view")] when claims-based auth is implemented
    [ProducesResponseType(typeof(BatchSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatchSummaryAsync(string idKey, CancellationToken ct = default)
    {
        var result = await batchDonationService.GetBatchSummaryAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Batch not found for summary: {IdKey}", idKey);
            return NotFound(new { error = result.Error });
        }

        logger.LogInformation("Retrieved batch summary: {IdKey}", idKey);

        return Ok(result.Value);
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
    // TODO(#258): Add [RequiresClaim("financial", "edit")] when claims-based auth is implemented
    [ProducesResponseType(typeof(ContributionBatchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBatchAsync([FromBody] CreateBatchRequest request, CancellationToken ct = default)
    {
        var result = await batchDonationService.CreateBatchAsync(request, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to create batch: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
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
    // TODO(#258): Add [RequiresClaim("financial", "edit")] when claims-based auth is implemented
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
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
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
    // TODO(#258): Add [RequiresClaim("financial", "batch.close")] when claims-based auth is implemented
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
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
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
    // TODO(#258): Add [RequiresClaim("financial", "view")] when claims-based auth is implemented
    [ProducesResponseType(typeof(IReadOnlyList<ContributionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatchContributionsAsync(string batchIdKey, CancellationToken ct = default)
    {
        var result = await batchDonationService.GetBatchContributionsAsync(batchIdKey, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to get contributions for batch {BatchIdKey}: {Error}", batchIdKey, result.Error);
            return NotFound(new { error = result.Error });
        }

        logger.LogInformation("Retrieved {Count} contributions for batch {BatchIdKey}", result.Value!.Count, batchIdKey);

        return Ok(result.Value);
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
    // TODO(#258): Add [RequiresClaim("financial", "edit")] when claims-based auth is implemented
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
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
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
    // TODO(#258): Add [RequiresClaim("financial", "view")] when claims-based auth is implemented
    [ProducesResponseType(typeof(ContributionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContributionAsync(string idKey, CancellationToken ct = default)
    {
        var result = await batchDonationService.GetContributionAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Contribution not found: {IdKey}", idKey);
            return NotFound(new { error = result.Error });
        }

        logger.LogInformation("Retrieved contribution: {IdKey}", idKey);

        return Ok(result.Value);
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
    // TODO(#258): Add [RequiresClaim("financial", "edit")] when claims-based auth is implemented
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
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Updated contribution: {IdKey}", idKey);

        return Ok(result.Value);
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
    // TODO(#258): Add [RequiresClaim("financial", "edit")] when claims-based auth is implemented
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
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Deleted contribution: {IdKey}", idKey);

        return NoContent();
    }

    #endregion
}
