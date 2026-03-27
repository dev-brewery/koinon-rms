using Koinon.Api.Filters;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for device (kiosk) management.
/// Provides endpoints for administering physical devices used for check-in.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
[ValidateIdKey]
public class DevicesController(
    IDeviceService deviceService,
    ILogger<DevicesController> logger) : ControllerBase
{

    /// <summary>
    /// Gets all devices.
    /// </summary>
    /// <param name="campusIdKey">Optional campus filter</param>
    /// <param name="includeInactive">Include inactive devices (default: false)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of devices</returns>
    /// <response code="200">Returns list of devices</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DeviceSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? campusIdKey = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var result = await deviceService.GetAllAsync(campusIdKey, includeInactive, ct);
        logger.LogInformation(
            "Retrieved {Count} devices (campusIdKey: {CampusIdKey}, includeInactive: {IncludeInactive})",
            result.Count,
            campusIdKey ?? "all",
            includeInactive);

        return Ok(new { data = result });
    }

    /// <summary>
    /// Gets a specific device by IdKey.
    /// </summary>
    /// <param name="idKey">The device's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Device details</returns>
    /// <response code="200">Returns device details</response>
    /// <response code="404">Device not found</response>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(DeviceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdKey(string idKey, CancellationToken ct = default)
    {
        var result = await deviceService.GetByIdKeyAsync(idKey, ct);

        if (result.IsFailure)
        {
            if (result.Error!.Code == "NOT_FOUND")
            {
                logger.LogDebug("Device not found: IdKey={IdKey}", idKey);
                return NotFound(new ProblemDetails
                {
                    Title = "Device not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }
        }

        logger.LogDebug("Device retrieved: IdKey={IdKey}, Name={Name}", idKey, result.Value!.Name);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Creates a new device.
    /// </summary>
    /// <param name="request">Device creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created device details</returns>
    /// <response code="201">Device created successfully</response>
    /// <response code="400">Validation failed</response>
    [HttpPost]
    [ProducesResponseType(typeof(DeviceDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDeviceRequest request, CancellationToken ct = default)
    {
        var result = await deviceService.CreateAsync(request, ct);

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

        logger.LogInformation(
            "Device created successfully: IdKey={IdKey}, Name={Name}",
            result.Value!.IdKey, result.Value.Name);

        return CreatedAtAction(
            nameof(GetByIdKey),
            new { idKey = result.Value.IdKey },
            new { data = result.Value });
    }

    /// <summary>
    /// Updates an existing device.
    /// </summary>
    /// <param name="idKey">The device's IdKey</param>
    /// <param name="request">Device update details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated device details</returns>
    /// <response code="200">Device updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Device not found</response>
    [HttpPut("{idKey}")]
    [ProducesResponseType(typeof(DeviceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string idKey,
        [FromBody] UpdateDeviceRequest request,
        CancellationToken ct = default)
    {
        var result = await deviceService.UpdateAsync(idKey, request, ct);

        if (result.IsFailure)
        {
            return result.Error!.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Device not found",
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

        logger.LogInformation(
            "Device updated successfully: IdKey={IdKey}, Name={Name}",
            result.Value!.IdKey, result.Value.Name);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Soft-deletes a device by setting IsActive to false.
    /// </summary>
    /// <param name="idKey">The device's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Device deactivated successfully</response>
    /// <response code="404">Device not found</response>
    [HttpDelete("{idKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string idKey, CancellationToken ct = default)
    {
        var result = await deviceService.DeleteAsync(idKey, ct);

        if (result.IsFailure)
        {
            return result.Error!.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Device not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
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

        logger.LogInformation("Device deactivated successfully: IdKey={IdKey}", idKey);

        return NoContent();
    }

    /// <summary>
    /// Generates or regenerates the kiosk authentication token for a device.
    /// </summary>
    /// <param name="idKey">The device's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated device details including the new token status</returns>
    /// <response code="200">Token generated successfully</response>
    /// <response code="404">Device not found</response>
    [HttpPost("{idKey}/token")]
    [ProducesResponseType(typeof(GenerateKioskTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateToken(string idKey, CancellationToken ct = default)
    {
        var result = await deviceService.GenerateKioskTokenAsync(idKey, ct);

        if (result.IsFailure)
        {
            return result.Error!.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Device not found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound,
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

        logger.LogInformation("Kiosk token generated for device IdKey={IdKey}", idKey);

        return Ok(new { data = result.Value });
    }
}
