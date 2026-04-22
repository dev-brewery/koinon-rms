using Koinon.Application.Common;
using Koinon.Application.DTOs.CheckinOperations;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Live check-in operations dashboard (#482).
/// Coordinator-only endpoints that surface a consolidated view of rooms, attendees,
/// and summary stats during Sunday morning operations, plus a room open/close toggle.
/// </summary>
[ApiController]
[Route("api/v1/checkin-operations")]
[Authorize]
public class CheckinOperationsController(
    ICheckinOperationsService service,
    ILogger<CheckinOperationsController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the live dashboard payload (rooms, attendees, summary).
    /// Intended to be polled every 5 seconds from the client.
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(CheckinOperationsDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDashboardAsync(CancellationToken ct = default)
    {
        var result = await service.GetDashboardAsync(ct);
        if (result.IsFailure)
        {
            return MapError(result.Error!);
        }

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Toggles a room's open/closed state for check-ins.
    /// Coordinator-only; gated by the controller-level [Authorize] attribute and the
    /// admin-area sidebar only exposes this page to authenticated admins.
    /// </summary>
    [HttpPost("rooms/{idKey}/toggle")]
    [ProducesResponseType(typeof(ToggleRoomResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleRoomAsync(string idKey, CancellationToken ct = default)
    {
        var result = await service.ToggleRoomAsync(idKey, ct);
        if (result.IsFailure)
        {
            return MapError(result.Error!);
        }

        logger.LogInformation(
            "Check-in room toggled: IdKey={IdKey}, IsOpen={IsOpen}",
            result.Value!.LocationIdKey, result.Value.IsOpen);

        return Ok(new { data = result.Value });
    }

    private IActionResult MapError(Error error)
    {
        logger.LogWarning(
            "CheckinOperationsController error: Code={Code}, Message={Message}",
            error.Code, error.Message);

        return error.Code switch
        {
            "NOT_FOUND" => NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = error.Message,
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            }),
            "VALIDATION_ERROR" => BadRequest(new ProblemDetails
            {
                Title = error.Message,
                Detail = error.Details != null
                    ? string.Join("; ", error.Details.SelectMany(kvp => kvp.Value))
                    : null,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path,
                Extensions = { ["errors"] = error.Details }
            }),
            _ => UnprocessableEntity(new ProblemDetails
            {
                Title = error.Code,
                Detail = error.Message,
                Status = StatusCodes.Status422UnprocessableEntity,
                Instance = HttpContext.Request.Path
            })
        };
    }
}
