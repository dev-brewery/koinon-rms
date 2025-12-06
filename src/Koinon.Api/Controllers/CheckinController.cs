using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for check-in operations.
/// MVP critical feature for Sunday morning kiosk operations.
/// Performance target: <200ms for individual check-in, <500ms for batch operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CheckinController(
    ICheckinConfigurationService configurationService,
    ICheckinSearchService searchService,
    ICheckinAttendanceService attendanceService,
    ILabelGenerationService labelService,
    ILogger<CheckinController> logger) : ControllerBase
{
    /// <summary>
    /// Gets active check-in areas for the current schedule.
    /// </summary>
    /// <param name="campusId">Campus IdKey (required)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of active check-in areas</returns>
    /// <response code="200">Returns list of active check-in areas</response>
    /// <response code="400">Invalid campus IdKey</response>
    [HttpGet("areas")]
    [AllowAnonymous] // Kiosk mode doesn't require authentication
    [ProducesResponseType(typeof(List<CheckinAreaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetActiveAreas(
        [FromQuery] string campusId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(campusId))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Campus IdKey is required",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var areas = await configurationService.GetActiveAreasAsync(campusId, null, ct);

        logger.LogInformation(
            "Active check-in areas retrieved: CampusId={CampusId}, AreaCount={AreaCount}",
            campusId, areas.Count);

        return Ok(areas);
    }

    /// <summary>
    /// Gets the complete check-in configuration for a campus or kiosk.
    /// </summary>
    /// <param name="campusId">Campus IdKey (optional if kioskId provided)</param>
    /// <param name="kioskId">Kiosk device IdKey (optional if campusId provided)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Check-in configuration</returns>
    /// <response code="200">Returns check-in configuration</response>
    /// <response code="400">Neither campusId nor kioskId provided</response>
    /// <response code="404">Campus or kiosk not found</response>
    [HttpGet("configuration")]
    [AllowAnonymous] // Kiosk mode doesn't require authentication
    [ProducesResponseType(typeof(CheckinConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfiguration(
        [FromQuery] string? campusId,
        [FromQuery] string? kioskId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(campusId) && string.IsNullOrWhiteSpace(kioskId))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Either campusId or kioskId must be provided",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        CheckinConfigurationDto? config = null;

        if (!string.IsNullOrWhiteSpace(kioskId))
        {
            config = await configurationService.GetConfigurationByKioskAsync(kioskId, ct);
        }
        else if (!string.IsNullOrWhiteSpace(campusId))
        {
            config = await configurationService.GetConfigurationByCampusAsync(campusId, ct);
        }

        if (config == null)
        {
            var identifier = !string.IsNullOrWhiteSpace(kioskId) ? $"kiosk '{kioskId}'" : $"campus '{campusId}'";
            logger.LogWarning("Check-in configuration not found: {Identifier}", identifier);

            return NotFound(new ProblemDetails
            {
                Title = "Configuration not found",
                Detail = $"No check-in configuration found for {identifier}",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogInformation(
            "Check-in configuration retrieved: Campus={Campus}, AreaCount={AreaCount}",
            config.Campus.Name, config.Areas.Count);

        return Ok(config);
    }

    /// <summary>
    /// Searches for families by phone number, name, or security code.
    /// </summary>
    /// <param name="query">Search query (phone, name, or security code)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of matching families</returns>
    /// <response code="200">Returns list of matching families</response>
    /// <response code="400">Invalid or missing search query</response>
    [HttpGet("search")]
    [AllowAnonymous] // Kiosk mode doesn't require authentication
    [ProducesResponseType(typeof(List<CheckinFamilySearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchFamilies(
        [FromQuery] string query,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Search query is required",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        // Auto-detect search type and route to appropriate method
        var families = await searchService.SearchAsync(query, ct);

        logger.LogInformation(
            "Family search completed: Query={Query}, ResultCount={ResultCount}",
            query, families.Count);

        return Ok(families);
    }

    /// <summary>
    /// Gets check-in opportunities for a family.
    /// Returns available locations and schedules for each family member.
    /// </summary>
    /// <param name="familyIdKey">Family IdKey</param>
    /// <param name="areaIdKey">Optional area IdKey to filter opportunities</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Check-in opportunities for family members</returns>
    /// <response code="200">Returns check-in opportunities</response>
    /// <response code="404">Family not found</response>
    [HttpGet("family/{familyIdKey}/opportunities")]
    [AllowAnonymous] // Kiosk mode doesn't require authentication
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetFamilyOpportunities(
        string familyIdKey,
        [FromQuery] string? areaIdKey,
        CancellationToken ct = default)
    {
        // Note: This endpoint needs a service method that doesn't exist yet in the interfaces.
        // For now, returning a placeholder indicating the service needs to be implemented.
        // The actual implementation would combine family search with area/location/schedule data.

        logger.LogWarning(
            "GetFamilyOpportunities called but service method not yet implemented: FamilyIdKey={FamilyIdKey}",
            familyIdKey);

        return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Title = "Not Implemented",
            Detail = "This endpoint requires additional service methods to be implemented",
            Status = StatusCodes.Status501NotImplemented,
            Instance = HttpContext.Request.Path
        }));
    }

    /// <summary>
    /// Records attendance (checks in) one or more people.
    /// </summary>
    /// <param name="request">Batch check-in request with person/location/schedule details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Check-in results with attendance records and security codes</returns>
    /// <response code="201">Check-in successful</response>
    /// <response code="400">Validation failed</response>
    /// <response code="422">Business rule violation (duplicate check-in, capacity, etc.)</response>
    [HttpPost("attendance")]
    [AllowAnonymous] // Kiosk mode doesn't require authentication
    [ProducesResponseType(typeof(BatchCheckinResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RecordAttendance(
        [FromBody] BatchCheckinRequestDto request,
        CancellationToken ct = default)
    {
        if (request.CheckIns == null || request.CheckIns.Count == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "At least one check-in is required",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await attendanceService.BatchCheckInAsync(request, ct);

        if (!result.AllSucceeded)
        {
            logger.LogWarning(
                "Batch check-in completed with errors: SuccessCount={SuccessCount}, FailureCount={FailureCount}",
                result.SuccessCount, result.FailureCount);

            // If all failed, return 422
            if (result.FailureCount == request.CheckIns.Count)
            {
                return UnprocessableEntity(new ProblemDetails
                {
                    Title = "Check-in failed",
                    Detail = string.Join("; ", result.Results
                        .Where(r => !r.Success)
                        .Select(r => r.ErrorMessage)),
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Instance = HttpContext.Request.Path
                });
            }
        }

        logger.LogInformation(
            "Batch check-in completed: TotalCount={TotalCount}, SuccessCount={SuccessCount}, FailureCount={FailureCount}",
            request.CheckIns.Count, result.SuccessCount, result.FailureCount);

        return CreatedAtAction(
            nameof(GetAttendanceLabels),
            new { attendanceIdKey = result.Results.FirstOrDefault(r => r.Success)?.AttendanceIdKey },
            result);
    }

    /// <summary>
    /// Checks out a person from a location.
    /// </summary>
    /// <param name="attendanceIdKey">Attendance record IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Check-out successful</response>
    /// <response code="404">Attendance record not found</response>
    [HttpPost("checkout/{attendanceIdKey}")]
    [AllowAnonymous] // Kiosk mode doesn't require authentication
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckOut(string attendanceIdKey, CancellationToken ct = default)
    {
        var success = await attendanceService.CheckOutAsync(attendanceIdKey, ct);

        if (!success)
        {
            logger.LogWarning("Check-out failed - attendance not found: AttendanceIdKey={AttendanceIdKey}", attendanceIdKey);

            return NotFound(new ProblemDetails
            {
                Title = "Attendance not found",
                Detail = $"No attendance record found with IdKey '{attendanceIdKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogInformation("Check-out successful: AttendanceIdKey={AttendanceIdKey}", attendanceIdKey);

        return NoContent();
    }

    /// <summary>
    /// Gets printable labels for an attendance record.
    /// </summary>
    /// <param name="attendanceIdKey">Attendance record IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Label data for printing</returns>
    /// <response code="200">Returns label data</response>
    /// <response code="404">Attendance record not found</response>
    [HttpGet("labels/{attendanceIdKey}")]
    [AllowAnonymous] // Kiosk mode doesn't require authentication
    [ProducesResponseType(typeof(LabelSetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAttendanceLabels(string attendanceIdKey, CancellationToken ct = default)
    {
        var request = new LabelRequestDto
        {
            AttendanceIdKey = attendanceIdKey
        };

        try
        {
            var labels = await labelService.GenerateLabelsAsync(request, ct);

            logger.LogInformation(
                "Labels generated: AttendanceIdKey={AttendanceIdKey}, LabelCount={LabelCount}",
                attendanceIdKey, labels.Labels.Count);

            return Ok(labels);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(
                ex,
                "Failed to generate labels - attendance not found: AttendanceIdKey={AttendanceIdKey}",
                attendanceIdKey);

            return NotFound(new ProblemDetails
            {
                Title = "Attendance not found",
                Detail = $"No attendance record found with IdKey '{attendanceIdKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Gets current attendance for a location.
    /// Shows who is currently checked in.
    /// </summary>
    /// <param name="locationIdKey">Location IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of current attendance records</returns>
    /// <response code="200">Returns current attendance</response>
    [HttpGet("locations/{locationIdKey}/attendance")]
    [ProducesResponseType(typeof(List<AttendanceSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocationAttendance(
        string locationIdKey,
        CancellationToken ct = default)
    {
        var attendance = await attendanceService.GetCurrentAttendanceAsync(locationIdKey, ct);

        logger.LogInformation(
            "Current attendance retrieved: LocationIdKey={LocationIdKey}, Count={Count}",
            locationIdKey, attendance.Count);

        return Ok(attendance);
    }
}
