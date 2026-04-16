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
/// Admin endpoints for managing giving funds (list all incl. inactive, create, edit, deactivate).
/// Separated from GivingController so the view-only fund endpoints there can stay with the
/// batch-entry surface they belong to.
/// </summary>
[ApiController]
[Route("api/v1/giving/admin/funds")]
[Authorize]
public class FundsController(
    IFundService fundService,
    ILogger<FundsController> logger) : ControllerBase
{
    /// <summary>
    /// Gets all funds for administration (active and inactive).
    /// </summary>
    [HttpGet("")]
    [RequiresClaim("financial", "view")]
    [ProducesResponseType(typeof(IReadOnlyList<FundAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllFundsAdminAsync(CancellationToken ct = default)
    {
        var funds = await fundService.GetAllFundsAsync(ct);
        logger.LogInformation("Retrieved {Count} funds for admin", funds.Count);
        return Ok(new { data = funds });
    }

    /// <summary>
    /// Creates a new fund.
    /// </summary>
    [HttpPost("")]
    [RequiresClaim("financial", "edit")]
    [ProducesResponseType(typeof(FundAdminDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFundAsync(
        [FromBody] CreateFundRequest request,
        CancellationToken ct = default)
    {
        var result = await fundService.CreateFundAsync(request, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to create fund: {Error}", result.Error);
            return Problem(
                detail: result.Error?.Message ?? "Failed to create fund",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request"
            );
        }

        logger.LogInformation("Created fund: {IdKey}", result.Value!.IdKey);
        return StatusCode(StatusCodes.Status201Created, new { data = result.Value });
    }

    /// <summary>
    /// Updates an existing fund.
    /// </summary>
    [HttpPut("{idKey}")]
    [ValidateIdKey]
    [RequiresClaim("financial", "edit")]
    [ProducesResponseType(typeof(FundAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFundAsync(
        string idKey,
        [FromBody] UpdateFundRequest request,
        CancellationToken ct = default)
    {
        var result = await fundService.UpdateFundAsync(idKey, request, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to update fund {IdKey}: {Error}", idKey, result.Error);

            if (result.Error?.Code == "NOT_FOUND")
            {
                return Problem(
                    detail: result.Error?.Message ?? "Fund not found",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found"
                );
            }

            return Problem(
                detail: result.Error?.Message ?? "Failed to update fund",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request"
            );
        }

        logger.LogInformation("Updated fund: {IdKey}", idKey);
        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Deactivates a fund (soft delete — sets IsActive = false).
    /// </summary>
    [HttpDelete("{idKey}")]
    [ValidateIdKey]
    [RequiresClaim("financial", "edit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateFundAsync(string idKey, CancellationToken ct = default)
    {
        var result = await fundService.DeactivateFundAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Failed to deactivate fund {IdKey}: {Error}", idKey, result.Error);
            return Problem(
                detail: result.Error?.Message ?? "Fund not found",
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found"
            );
        }

        logger.LogInformation("Deactivated fund: {IdKey}", idKey);
        return NoContent();
    }
}
