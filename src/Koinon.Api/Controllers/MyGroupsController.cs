using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// API controller for managing groups where the current user is a leader.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/my-groups")]
public class MyGroupsController(IMyGroupsService myGroupsService) : ControllerBase
{
    /// <summary>
    /// Gets all groups where the current user is a leader.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyGroups(CancellationToken ct)
    {
        var groups = await myGroupsService.GetMyGroupsAsync(ct);
        return Ok(new { data = groups });
    }

    /// <summary>
    /// Gets detailed member information including contact details for a group.
    /// Only accessible to group leaders and staff.
    /// </summary>
    [HttpGet("{groupIdKey}/members")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupMembersWithContactInfo(string groupIdKey, CancellationToken ct)
    {
        var result = await myGroupsService.GetGroupMembersWithContactInfoAsync(groupIdKey, ct);

        if (!result.IsSuccess)
        {
            return result.Error!.Code switch
            {
                "NOT_FOUND" => NotFound(result.Error),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result.Error),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Updates a group member's role or status.
    /// Only accessible to group leaders and staff.
    /// </summary>
    [HttpPut("{groupIdKey}/members/{memberIdKey}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGroupMember(
        string groupIdKey,
        string memberIdKey,
        [FromBody] UpdateGroupMemberRequest request,
        CancellationToken ct)
    {
        var result = await myGroupsService.UpdateGroupMemberAsync(groupIdKey, memberIdKey, request, ct);

        if (!result.IsSuccess)
        {
            return result.Error!.Code switch
            {
                "NOT_FOUND" => NotFound(result.Error),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result.Error),
                "VALIDATION_ERROR" => BadRequest(result.Error),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Removes a member from a group.
    /// Only accessible to group leaders and staff.
    /// </summary>
    [HttpDelete("{groupIdKey}/members/{memberIdKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveGroupMember(
        string groupIdKey,
        string memberIdKey,
        CancellationToken ct)
    {
        var result = await myGroupsService.RemoveGroupMemberAsync(groupIdKey, memberIdKey, ct);

        if (!result.IsSuccess)
        {
            return result.Error!.Code switch
            {
                "NOT_FOUND" => NotFound(result.Error),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result.Error),
                _ => BadRequest(result.Error)
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Records attendance for a group meeting.
    /// Only accessible to group leaders and staff.
    /// </summary>
    [HttpPost("{groupIdKey}/attendance")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordAttendance(
        string groupIdKey,
        [FromBody] RecordAttendanceRequest request,
        CancellationToken ct)
    {
        var result = await myGroupsService.RecordAttendanceAsync(groupIdKey, request, ct);

        if (!result.IsSuccess)
        {
            return result.Error!.Code switch
            {
                "NOT_FOUND" => NotFound(result.Error),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result.Error),
                "VALIDATION_ERROR" => BadRequest(result.Error),
                _ => BadRequest(result.Error)
            };
        }

        return StatusCode(StatusCodes.Status201Created);
    }
}
