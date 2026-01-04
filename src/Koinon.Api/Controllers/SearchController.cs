using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for global search operations across multiple entity types.
/// Provides endpoints for searching People, Families, and Groups.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SearchController(
    IGlobalSearchService globalSearchService,
    ILogger<SearchController> logger) : ControllerBase
{
    /// <summary>
    /// Performs a global search across People, Families, and Groups.
    /// </summary>
    /// <param name="q">Search query (minimum 2 characters required)</param>
    /// <param name="category">Optional filter for specific category ("People", "Families", or "Groups")</param>
    /// <param name="pageNumber">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated search results with category counts</returns>
    /// <response code="200">Returns search results with pagination and category counts</response>
    /// <response code="400">Invalid search query or parameters</response>
    [HttpGet]
    [ProducesResponseType(typeof(GlobalSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string? category,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // Validate query is not empty
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new
            {
                error = "Query parameter 'q' is required."
            });
        }

        // Validate minimum query length
        if (q.Trim().Length < 2)
        {
            return BadRequest(new
            {
                error = "Query must be at least 2 characters long."
            });
        }

        // Validate page number
        if (pageNumber < 1)
        {
            return BadRequest(new
            {
                error = "Page number must be at least 1."
            });
        }

        // Validate page size
        if (pageSize > 100)
        {
            pageSize = 100;
        }

        // Validate category if provided
        if (!string.IsNullOrWhiteSpace(category))
        {
            var validCategories = new[] { "People", "Families", "Groups" };
            if (!validCategories.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    error = $"Invalid category. Must be one of: {string.Join(", ", validCategories)}"
                });
            }
        }

        logger.LogInformation(
            "Global search: Query={Query}, Category={Category}, Page={PageNumber}, PageSize={PageSize}",
            q, category, pageNumber, pageSize);

        var result = await globalSearchService.SearchAsync(
            q,
            category,
            pageNumber,
            pageSize,
            ct);

        logger.LogInformation(
            "Global search completed: Query={Query}, TotalResults={TotalCount}, Categories={Categories}",
            q, result.TotalCount, string.Join(", ", result.CategoryCounts.Keys));

        return Ok(result);
    }
}
