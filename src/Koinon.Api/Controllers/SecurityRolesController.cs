using Koinon.Api.Authorization;
using Koinon.Application.Common;
using Koinon.Application.DTOs.Security;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Administrative controller for managing security roles, their claims, and their members.
/// All endpoints require the "security:manage" claim.
/// </summary>
[ApiController]
[Route("api/v1/admin/security-roles")]
[Authorize]
[RequiresClaim("security", "manage")]
public class SecurityRolesController(
    ISecurityRoleService service,
    ILogger<SecurityRolesController> logger) : ControllerBase
{
    /// <summary>
    /// Lists all security roles with summary counts of claims and members.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SecurityRoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken ct = default)
    {
        var result = await service.GetAllAsync(ct);
        if (result.IsFailure)
        {
            return MapError(result.Error!);
        }

        logger.LogInformation("Retrieved {Count} security roles", result.Value!.Count);
        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Gets the full detail of a security role, including its claims and members.
    /// </summary>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(SecurityRoleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdKeyAsync(string idKey, CancellationToken ct = default)
    {
        var result = await service.GetByIdKeyAsync(idKey, ct);
        if (result.IsFailure)
        {
            return MapError(result.Error!);
        }

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Creates a new security role.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SecurityRoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateSecurityRoleRequest request,
        CancellationToken ct = default)
    {
        var result = await service.CreateAsync(request, ct);
        if (result.IsFailure)
        {
            return MapError(result.Error!);
        }

        var role = result.Value!;
        logger.LogInformation("Security role created: IdKey={IdKey}", role.IdKey);

        return Created($"/api/v1/admin/security-roles/{role.IdKey}", new { data = role });
    }

    /// <summary>
    /// Updates an existing security role.
    /// </summary>
    [HttpPut("{idKey}")]
    [ProducesResponseType(typeof(SecurityRoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(
        string idKey,
        [FromBody] UpdateSecurityRoleRequest request,
        CancellationToken ct = default)
    {
        var result = await service.UpdateAsync(idKey, request, ct);
        if (result.IsFailure)
        {
            return MapError(result.Error!);
        }

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Deletes a security role. System roles cannot be deleted.
    /// </summary>
    [HttpDelete("{idKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteAsync(string idKey, CancellationToken ct = default)
    {
        var result = await service.DeleteAsync(idKey, ct);
        if (result.IsFailure)
        {
            return MapError(result.Error!);
        }

        return NoContent();
    }

    /// <summary>
    /// Attaches a claim to a role with an Allow or Deny decision.
    /// </summary>
    [HttpPost("{idKey}/claims")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignClaimAsync(
        string idKey,
        [FromBody] AssignClaimRequest request,
        CancellationToken ct = default)
    {
        var result = await service.AssignClaimAsync(idKey, request, ct);
        if (result.IsFailure)
        {
            return MapError(result.Error!);
        }

        return NoContent();
    }

    /// <summary>
    /// Removes a claim from a role.
    /// </summary>
    [HttpDelete("{idKey}/claims/{claimIdKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveClaimAsync(
        string idKey,
        string claimIdKey,
        CancellationToken ct = default)
    {
        var result = await service.RemoveClaimAsync(idKey, claimIdKey, ct);
        if (result.IsFailure)
        {
            return MapError(result.Error!);
        }

        return NoContent();
    }

    /// <summary>
    /// Adds a person to a role, optionally with an expiration date.
    /// </summary>
    [HttpPost("{idKey}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMemberAsync(
        string idKey,
        [FromBody] AssignPersonRequest request,
        CancellationToken ct = default)
    {
        var result = await service.AddMemberAsync(idKey, request, ct);
        if (result.IsFailure)
        {
            return MapError(result.Error!);
        }

        return NoContent();
    }

    /// <summary>
    /// Removes a person from a role.
    /// </summary>
    [HttpDelete("{idKey}/members/{personIdKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMemberAsync(
        string idKey,
        string personIdKey,
        CancellationToken ct = default)
    {
        var result = await service.RemoveMemberAsync(idKey, personIdKey, ct);
        if (result.IsFailure)
        {
            return MapError(result.Error!);
        }

        return NoContent();
    }

    private IActionResult MapError(Error error)
    {
        logger.LogWarning(
            "SecurityRolesController error: Code={Code}, Message={Message}",
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
            "CONFLICT" => Conflict(new ProblemDetails
            {
                Title = "Conflict",
                Detail = error.Message,
                Status = StatusCodes.Status409Conflict,
                Instance = HttpContext.Request.Path
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
