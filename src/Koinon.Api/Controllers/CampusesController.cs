using Koinon.Api.Filters;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for campus operations.
/// Provides endpoints for retrieving campus information.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[ValidateIdKey]
public class CampusesController(
    IApplicationDbContext context,
    ILogger<CampusesController> logger) : ControllerBase
{
    /// <summary>
    /// Gets all campuses.
    /// </summary>
    /// <param name="includeInactive">Include inactive campuses (default: false)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of all campuses</returns>
    /// <response code="200">Returns list of campuses</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CampusSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var query = context.Campuses.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        var campuses = await query
            .OrderBy(c => c.Order)
            .ThenBy(c => c.Name)
            .Select(c => new CampusSummaryDto
            {
                IdKey = c.IdKey,
                Name = c.Name,
                ShortCode = c.ShortCode
            })
            .ToListAsync(ct);

        logger.LogInformation(
            "Retrieved {Count} campuses (includeInactive: {IncludeInactive})",
            campuses.Count,
            includeInactive);

        return Ok(new { data = campuses });
    }
}
