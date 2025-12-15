using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// API controller for self-service profile management.
/// Allows authenticated users to view and update their own profile and family information.
/// </summary>
[Authorize]
[ApiController]
public class MyProfileController(IMyProfileService myProfileService) : ControllerBase
{
    /// <summary>
    /// Gets the current user's profile details.
    /// </summary>
    [HttpGet("api/v1/my-profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var result = await myProfileService.GetMyProfileAsync(ct);

        if (!result.IsSuccess)
        {
            return result.Error!.Code switch
            {
                "NOT_FOUND" => NotFound(result.Error),
                "FORBIDDEN" => Unauthorized(result.Error),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Updates the current user's profile with restricted fields.
    /// </summary>
    [HttpPut("api/v1/my-profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateMyProfileRequest request,
        CancellationToken ct)
    {
        var result = await myProfileService.UpdateMyProfileAsync(request, ct);

        if (!result.IsSuccess)
        {
            return result.Error!.Code switch
            {
                "NOT_FOUND" => NotFound(result.Error),
                "FORBIDDEN" => Unauthorized(result.Error),
                "VALIDATION_ERROR" => BadRequest(result.Error),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Gets the current user's family members.
    /// </summary>
    [HttpGet("api/v1/my-family")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyFamily(CancellationToken ct)
    {
        var result = await myProfileService.GetMyFamilyAsync(ct);

        if (!result.IsSuccess)
        {
            return result.Error!.Code switch
            {
                "FORBIDDEN" => Unauthorized(result.Error),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Updates a family member's information (limited fields).
    /// Only allowed if current user is an adult in the family AND target person is a child.
    /// </summary>
    [HttpPut("api/v1/my-family/members/{personIdKey}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFamilyMember(
        string personIdKey,
        [FromBody] UpdateFamilyMemberRequest request,
        CancellationToken ct)
    {
        var result = await myProfileService.UpdateFamilyMemberAsync(personIdKey, request, ct);

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
    /// Gets the current user's involvement (groups and attendance summary).
    /// </summary>
    [HttpGet("api/v1/my-involvement")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyInvolvement(CancellationToken ct)
    {
        var result = await myProfileService.GetMyInvolvementAsync(ct);

        if (!result.IsSuccess)
        {
            return result.Error!.Code switch
            {
                "FORBIDDEN" => Unauthorized(result.Error),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(new { data = result.Value });
    }
}
