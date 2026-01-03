using Koinon.Api.Filters;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for campus operations.
/// Provides endpoints for managing campus information.
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

    /// <summary>
    /// Gets a specific campus by IdKey.
    /// </summary>
    /// <param name="idKey">The campus's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Campus details</returns>
    /// <response code="200">Returns campus details</response>
    /// <response code="404">Campus not found</response>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(CampusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdKey(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            logger.LogDebug("Invalid IdKey format: {IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Campus not found",
                Detail = $"Invalid IdKey format: '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        var campus = await context.Campuses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CampusDto
            {
                IdKey = c.IdKey,
                Guid = c.Guid,
                Name = c.Name,
                ShortCode = c.ShortCode,
                Description = c.Description,
                IsActive = c.IsActive,
                Url = c.Url,
                PhoneNumber = c.PhoneNumber,
                TimeZoneId = c.TimeZoneId,
                ServiceTimes = c.ServiceTimes,
                Order = c.Order,
                CreatedDateTime = c.CreatedDateTime,
                ModifiedDateTime = c.ModifiedDateTime
            })
            .FirstOrDefaultAsync(ct);

        if (campus == null)
        {
            logger.LogDebug("Campus not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Campus not found",
                Detail = $"No campus found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug("Campus retrieved: IdKey={IdKey}, Name={Name}", idKey, campus.Name);

        return Ok(new { data = campus });
    }

    /// <summary>
    /// Creates a new campus.
    /// </summary>
    /// <param name="request">Campus creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created campus details</returns>
    /// <response code="201">Campus created successfully</response>
    /// <response code="400">Validation failed</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CampusDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCampusRequest request, CancellationToken ct = default)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "Campus name is required",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var campus = new Domain.Entities.Campus
        {
            Name = request.Name,
            ShortCode = request.ShortCode,
            Description = request.Description,
            IsActive = true,
            Url = request.Url,
            PhoneNumber = request.PhoneNumber,
            TimeZoneId = request.TimeZoneId,
            ServiceTimes = request.ServiceTimes,
            Order = request.Order,
            CreatedDateTime = DateTime.UtcNow
        };

        context.Campuses.Add(campus);
        await context.SaveChangesAsync(ct);

        var campusDto = new CampusDto
        {
            IdKey = campus.IdKey,
            Guid = campus.Guid,
            Name = campus.Name,
            ShortCode = campus.ShortCode,
            Description = campus.Description,
            IsActive = campus.IsActive,
            Url = campus.Url,
            PhoneNumber = campus.PhoneNumber,
            TimeZoneId = campus.TimeZoneId,
            ServiceTimes = campus.ServiceTimes,
            Order = campus.Order,
            CreatedDateTime = campus.CreatedDateTime,
            ModifiedDateTime = campus.ModifiedDateTime
        };

        logger.LogInformation(
            "Campus created successfully: IdKey={IdKey}, Name={Name}",
            campusDto.IdKey, campusDto.Name);

        return CreatedAtAction(
            nameof(GetByIdKey),
            new { idKey = campusDto.IdKey },
            new { data = campusDto });
    }

    /// <summary>
    /// Updates an existing campus.
    /// </summary>
    /// <param name="idKey">The campus's IdKey</param>
    /// <param name="request">Campus update details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated campus details</returns>
    /// <response code="200">Campus updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Campus not found</response>
    [HttpPut("{idKey}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CampusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string idKey,
        [FromBody] UpdateCampusRequest request,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            logger.LogDebug("Invalid IdKey format: {IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Campus not found",
                Detail = $"Invalid IdKey format: '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        var campus = await context.Campuses
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync(ct);

        if (campus == null)
        {
            logger.LogDebug("Campus not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Campus not found",
                Detail = $"No campus found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        // Apply partial updates
        if (request.Name != null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = "Campus name cannot be empty",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }
            campus.Name = request.Name;
        }

        if (request.ShortCode != null)
            campus.ShortCode = request.ShortCode;

        if (request.Description != null)
            campus.Description = request.Description;

        if (request.Url != null)
            campus.Url = request.Url;

        if (request.PhoneNumber != null)
            campus.PhoneNumber = request.PhoneNumber;

        if (request.TimeZoneId != null)
            campus.TimeZoneId = request.TimeZoneId;

        if (request.ServiceTimes != null)
            campus.ServiceTimes = request.ServiceTimes;

        if (request.Order.HasValue)
            campus.Order = request.Order.Value;

        if (request.IsActive.HasValue)
            campus.IsActive = request.IsActive.Value;

        campus.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        var campusDto = new CampusDto
        {
            IdKey = campus.IdKey,
            Guid = campus.Guid,
            Name = campus.Name,
            ShortCode = campus.ShortCode,
            Description = campus.Description,
            IsActive = campus.IsActive,
            Url = campus.Url,
            PhoneNumber = campus.PhoneNumber,
            TimeZoneId = campus.TimeZoneId,
            ServiceTimes = campus.ServiceTimes,
            Order = campus.Order,
            CreatedDateTime = campus.CreatedDateTime,
            ModifiedDateTime = campus.ModifiedDateTime
        };

        logger.LogInformation(
            "Campus updated successfully: IdKey={IdKey}, Name={Name}",
            campusDto.IdKey, campusDto.Name);

        return Ok(new { data = campusDto });
    }

    /// <summary>
    /// Soft-deletes a campus by setting IsActive to false.
    /// </summary>
    /// <param name="idKey">The campus's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Campus deactivated successfully</response>
    /// <response code="404">Campus not found</response>
    [HttpDelete("{idKey}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out var id))
        {
            logger.LogDebug("Invalid IdKey format: {IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Campus not found",
                Detail = $"Invalid IdKey format: '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        var campus = await context.Campuses
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync(ct);

        if (campus == null)
        {
            logger.LogDebug("Campus not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Campus not found",
                Detail = $"No campus found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        // Soft delete by setting IsActive to false
        campus.IsActive = false;
        campus.ModifiedDateTime = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Campus deactivated successfully: IdKey={IdKey}", idKey);

        return NoContent();
    }
}
