using Koinon.Api.Filters;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for location operations.
/// Provides endpoints for managing physical locations (buildings, rooms) with hierarchical organization.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[ValidateIdKey]
public class LocationsController(
    ILocationService locationService,
    ILogger<LocationsController> logger) : ControllerBase
{
    private readonly ILocationService _locationService = locationService;
    private readonly ILogger<LocationsController> _logger = logger;

    /// <summary>
    /// Gets all locations.
    /// </summary>
    /// <param name="campusIdKey">Optional campus filter - only return locations for this campus</param>
    /// <param name="includeInactive">Include inactive locations (default: false)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of all locations</returns>
    /// <response code="200">Returns list of locations</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LocationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? campusIdKey = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var result = await _locationService.GetAllAsync(campusIdKey, includeInactive, ct);
        
        _logger.LogInformation(
            "Retrieved {Count} locations (campusIdKey: {CampusIdKey}, includeInactive: {IncludeInactive})",
            result.Count,
            campusIdKey ?? "all",
            includeInactive);

        return Ok(new { data = result });
    }

    /// <summary>
    /// Gets locations in hierarchical tree structure.
    /// </summary>
    /// <param name="campusIdKey">Optional campus filter - only return locations for this campus</param>
    /// <param name="includeInactive">Include inactive locations (default: false)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Hierarchical tree of locations</returns>
    /// <response code="200">Returns hierarchical location tree</response>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(IReadOnlyList<LocationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree(
        [FromQuery] string? campusIdKey = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var result = await _locationService.GetTreeAsync(campusIdKey, includeInactive, ct);

        if (result.IsFailure)
        {
            return result.Error!.Code switch
            {
                "VALIDATION_ERROR" => BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "An error occurred",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status500InternalServerError
                })
            };
        }

        _logger.LogInformation(
            "Retrieved location tree with {Count} root locations (campusIdKey: {CampusIdKey}, includeInactive: {IncludeInactive})",
            result.Value!.Count,
            campusIdKey ?? "all",
            includeInactive);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Gets a specific location by IdKey.
    /// </summary>
    /// <param name="idKey">The location's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Location details</returns>
    /// <response code="200">Returns location details</response>
    /// <response code="404">Location not found</response>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdKey(string idKey, CancellationToken ct = default)
    {
        var result = await _locationService.GetByIdKeyAsync(idKey, ct);

        if (result.IsFailure)
        {
            if (result.Error!.Code == "NOT_FOUND")
            {
                _logger.LogDebug("Location not found: IdKey={IdKey}", idKey);
                return NotFound(new ProblemDetails
                {
                    Title = "Location not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }
        }

        _logger.LogDebug("Location retrieved: IdKey={IdKey}, Name={Name}", idKey, result.Value!.Name);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Creates a new location.
    /// </summary>
    /// <param name="request">Location creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created location details</returns>
    /// <response code="201">Location created successfully</response>
    /// <response code="400">Validation failed</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateLocationRequest request, CancellationToken ct = default)
    {
        var result = await _locationService.CreateAsync(request, ct);

        if (result.IsFailure)
        {
            return result.Error!.Code switch
            {
                "VALIDATION_ERROR" => BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path,
                    Extensions = { ["errors"] = result.Error.Details }
                }),
                "UNPROCESSABLE_ENTITY" => BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "An error occurred",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status500InternalServerError
                })
            };
        }

        _logger.LogInformation(
            "Location created successfully: IdKey={IdKey}, Name={Name}",
            result.Value!.IdKey, result.Value.Name);

        return CreatedAtAction(
            nameof(GetByIdKey),
            new { idKey = result.Value.IdKey },
            new { data = result.Value });
    }

    /// <summary>
    /// Updates an existing location.
    /// </summary>
    /// <param name="idKey">The location's IdKey</param>
    /// <param name="request">Location update details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated location details</returns>
    /// <response code="200">Location updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Location not found</response>
    [HttpPut("{idKey}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string idKey,
        [FromBody] UpdateLocationRequest request,
        CancellationToken ct = default)
    {
        var result = await _locationService.UpdateAsync(idKey, request, ct);

        if (result.IsFailure)
        {
            return result.Error!.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Location not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                }),
                "VALIDATION_ERROR" => BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path,
                    Extensions = { ["errors"] = result.Error.Details }
                }),
                "UNPROCESSABLE_ENTITY" => BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "An error occurred",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status500InternalServerError
                })
            };
        }

        _logger.LogInformation(
            "Location updated successfully: IdKey={IdKey}, Name={Name}",
            result.Value!.IdKey, result.Value.Name);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Soft-deletes a location by setting IsActive to false.
    /// </summary>
    /// <param name="idKey">The location's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Location deactivated successfully</response>
    /// <response code="400">Cannot deactivate location with active children</response>
    /// <response code="404">Location not found</response>
    [HttpDelete("{idKey}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string idKey, CancellationToken ct = default)
    {
        var result = await _locationService.DeleteAsync(idKey, ct);

        if (result.IsFailure)
        {
            return result.Error!.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Location not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                }),
                "UNPROCESSABLE_ENTITY" => BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "An error occurred",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status500InternalServerError
                })
            };
        }

        _logger.LogInformation("Location deactivated successfully: IdKey={IdKey}", idKey);

        return NoContent();
    }
}
