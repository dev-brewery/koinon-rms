using Koinon.Api.Filters;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    ICampusService campusService,
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
        var campuses = await campusService.GetAllAsync(includeInactive, ct);

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
        var result = await campusService.GetByIdKeyAsync(idKey, ct);

        if (result.IsFailure)
        {
            logger.LogDebug("Campus not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Campus not found",
                Detail = result.Error!.Message,
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug("Campus retrieved: IdKey={IdKey}, Name={Name}", idKey, result.Value!.Name);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Creates a new campus.
    /// </summary>
    /// <param name="request">Campus creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created campus details</returns>
    /// <response code="201">Campus created successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CampusDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateCampusRequest request, CancellationToken ct = default)
    {
        var result = await campusService.CreateAsync(request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to create campus: Code={Code}, Message={Message}",
                result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "VALIDATION_ERROR" => BadRequest(new ProblemDetails
                {
                    Title = result.Error.Message,
                    Detail = result.Error.Details != null
                        ? string.Join("; ", result.Error.Details.SelectMany(kvp => kvp.Value))
                        : null,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path,
                    Extensions = { ["errors"] = result.Error.Details }
                }),
                _ => UnprocessableEntity(new ProblemDetails
                {
                    Title = result.Error.Code,
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Instance = HttpContext.Request.Path
                })
            };
        }

        var campus = result.Value!;

        logger.LogInformation(
            "Campus created successfully: IdKey={IdKey}, Name={Name}",
            campus.IdKey, campus.Name);

        return CreatedAtAction(
            nameof(GetByIdKey),
            new { idKey = campus.IdKey },
            new { data = campus });
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
    /// <response code="422">Business rule violation</response>
    [HttpPut("{idKey}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CampusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        string idKey,
        [FromBody] UpdateCampusRequest request,
        CancellationToken ct = default)
    {
        var result = await campusService.UpdateAsync(idKey, request, ct);

        if (result.IsFailure)
        {
            logger.LogDebug(
                "Failed to update campus: IdKey={IdKey}, Code={Code}, Message={Message}",
                idKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Campus not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                }),
                "VALIDATION_ERROR" => BadRequest(new ProblemDetails
                {
                    Title = result.Error.Message,
                    Detail = result.Error.Details != null
                        ? string.Join("; ", result.Error.Details.SelectMany(kvp => kvp.Value))
                        : null,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path,
                    Extensions = { ["errors"] = result.Error.Details }
                }),
                _ => UnprocessableEntity(new ProblemDetails
                {
                    Title = result.Error.Code,
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Instance = HttpContext.Request.Path
                })
            };
        }

        var campus = result.Value!;

        logger.LogInformation(
            "Campus updated successfully: IdKey={IdKey}, Name={Name}",
            campus.IdKey, campus.Name);

        return Ok(new { data = campus });
    }

    /// <summary>
    /// Soft-deletes a campus by setting IsActive to false.
    /// </summary>
    /// <param name="idKey">The campus's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Campus deactivated successfully</response>
    /// <response code="404">Campus not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpDelete("{idKey}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(string idKey, CancellationToken ct = default)
    {
        var result = await campusService.DeleteAsync(idKey, ct);

        if (result.IsFailure)
        {
            logger.LogDebug(
                "Failed to deactivate campus: IdKey={IdKey}, Code={Code}, Message={Message}",
                idKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Campus not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                }),
                _ => UnprocessableEntity(new ProblemDetails
                {
                    Title = result.Error.Code,
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Instance = HttpContext.Request.Path
                })
            };
        }

        logger.LogInformation("Campus deactivated successfully: IdKey={IdKey}", idKey);

        return NoContent();
    }
}
