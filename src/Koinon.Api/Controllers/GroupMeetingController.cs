using FluentValidation;
using Koinon.Application.DTOs.GroupMeeting;
using Koinon.Application.Interfaces;
using Koinon.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// API controller for managing group meeting RSVPs.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1")]
public class GroupMeetingController(
    GroupMeetingService groupMeetingService,
    IUserContext userContext,
    IValidator<UpdateRsvpRequest> updateRsvpValidator) : ControllerBase
{
    /// <summary>
    /// Send RSVP requests to all active members for a specific meeting.
    /// Requires GroupLeader role or staff permissions.
    /// </summary>
    [HttpPost("groups/{idKey}/meetings/{date}/request-rsvp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RequestRsvps(string idKey, string date, CancellationToken ct)
    {
        // Verify user is a leader of this group
        if (!userContext.CurrentPersonId.HasValue)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        // Check if user is a group leader
        var isLeader = await groupMeetingService.IsGroupLeaderAsync(
            userContext.CurrentPersonId.Value, idKey, ct);

        if (!isLeader)
        {
            return Forbid();
        }

        if (!DateOnly.TryParse(date, out DateOnly meetingDate))
        {
            return BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });
        }

        try
        {
            var count = await groupMeetingService.SendRsvpRequestsAsync(idKey, meetingDate, ct);
            return Ok(new { count, message = $"{count} RSVP requests created" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get RSVP summary and responses for a specific meeting.
    /// </summary>
    [HttpGet("groups/{idKey}/meetings/{date}/rsvps")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMeetingRsvps(string idKey, string date, CancellationToken ct)
    {
        if (!DateOnly.TryParse(date, out DateOnly meetingDate))
        {
            return BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });
        }

        var summary = await groupMeetingService.GetRsvpsAsync(idKey, meetingDate, ct);

        if (summary == null)
        {
            return NotFound(new { error = "Group not found" });
        }

        return Ok(summary);
    }

    /// <summary>
    /// Update the current user's RSVP for a specific meeting.
    /// </summary>
    [HttpPut("my-groups/{groupIdKey}/rsvp/{date}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyRsvp(
        string groupIdKey,
        string date,
        [FromBody] UpdateRsvpRequest request,
        CancellationToken ct)
    {
        // Validate request
        var validationResult = await updateRsvpValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        if (!userContext.CurrentPersonId.HasValue)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        if (!DateOnly.TryParse(date, out DateOnly meetingDate))
        {
            return BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });
        }

        var success = await groupMeetingService.UpdateRsvpAsync(
            userContext.CurrentPersonId.Value,
            groupIdKey,
            meetingDate,
            request.Status,
            request.Note,
            ct);

        if (!success)
        {
            return NotFound(new { error = "Group not found or you are not a member" });
        }

        return Ok(new { message = "RSVP updated successfully" });
    }

    /// <summary>
    /// Get the current user's pending and upcoming RSVP requests.
    /// </summary>
    [HttpGet("my-rsvps")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyRsvps(
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        CancellationToken ct = default)
    {
        if (!userContext.CurrentPersonId.HasValue)
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        DateOnly? parsedStartDate = null;
        if (!string.IsNullOrEmpty(startDate) && DateOnly.TryParse(startDate, out DateOnly sd))
        {
            parsedStartDate = sd;
        }

        DateOnly? parsedEndDate = null;
        if (!string.IsNullOrEmpty(endDate) && DateOnly.TryParse(endDate, out DateOnly ed))
        {
            parsedEndDate = ed;
        }

        var rsvps = await groupMeetingService.GetMyRsvpsAsync(
            userContext.CurrentPersonId.Value,
            parsedStartDate,
            parsedEndDate,
            ct);

        return Ok(rsvps);
    }
}
