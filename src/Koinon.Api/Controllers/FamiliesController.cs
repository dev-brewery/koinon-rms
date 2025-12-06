using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for family (household) management operations.
/// Provides endpoints for searching, creating, updating, and managing family members.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class FamiliesController(
    IFamilyService familyService,
    ILogger<FamiliesController> logger) : ControllerBase
{
    /// <summary>
    /// Searches for families with optional filters and pagination.
    /// </summary>
    /// <param name="query">Full-text search query (family name or member names)</param>
    /// <param name="campusId">Filter by campus IdKey</param>
    /// <param name="includeInactive">Include inactive families</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 25, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of families</returns>
    /// <response code="200">Returns paginated list of families</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FamilySummaryDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> Search(
        [FromQuery] string? query,
        [FromQuery] string? campusId,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var parameters = new FamilySearchParameters
        {
            Query = query,
            CampusId = campusId,
            IncludeInactive = includeInactive,
            Page = page,
            PageSize = pageSize
        };

        parameters.ValidatePageSize();

        // Note: This requires IFamilyService.SearchAsync method to be implemented
        // For now, returning a placeholder response
        logger.LogWarning("FamilyService.SearchAsync not yet implemented");

        var result = new PagedResult<FamilySummaryDto>(
            items: new List<FamilySummaryDto>(),
            totalCount: 0,
            page: parameters.Page,
            pageSize: parameters.PageSize);

        logger.LogInformation(
            "Families search completed: Query={Query}, Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            query, result.Page, result.PageSize, result.TotalCount);

        return Task.FromResult<IActionResult>(Ok(result));
    }

    /// <summary>
    /// Gets a family by their IdKey with full details including members.
    /// </summary>
    /// <param name="idKey">The family's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Family details with members</returns>
    /// <response code="200">Returns family details</response>
    /// <response code="404">Family not found</response>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(FamilyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdKey(string idKey, CancellationToken ct = default)
    {
        var family = await familyService.GetByIdKeyAsync(idKey, ct);

        if (family == null)
        {
            logger.LogWarning("Family not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Family not found",
                Detail = $"No family found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogInformation("Family retrieved: IdKey={IdKey}, Name={Name}", idKey, family.Name);

        return Ok(family);
    }

    /// <summary>
    /// Gets family members for a specific family.
    /// </summary>
    /// <param name="idKey">The family's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of family members</returns>
    /// <response code="200">Returns list of family members</response>
    /// <response code="404">Family not found</response>
    [HttpGet("{idKey}/members")]
    [ProducesResponseType(typeof(IReadOnlyList<FamilyMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMembers(string idKey, CancellationToken ct = default)
    {
        var family = await familyService.GetByIdKeyAsync(idKey, ct);

        if (family == null)
        {
            logger.LogWarning("Family not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Family not found",
                Detail = $"No family found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogInformation(
            "Family members retrieved: IdKey={IdKey}, MemberCount={MemberCount}",
            idKey, family.Members.Count);

        return Ok(family.Members);
    }

    /// <summary>
    /// Creates a new family.
    /// </summary>
    /// <param name="request">Family creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created family details</returns>
    /// <response code="201">Family created successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost]
    [ProducesResponseType(typeof(FamilyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateFamilyRequest request, CancellationToken ct = default)
    {
        var result = await familyService.CreateFamilyAsync(request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to create family: Code={Code}, Message={Message}",
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

        var family = result.Value!;

        logger.LogInformation(
            "Family created successfully: IdKey={IdKey}, Name={Name}",
            family.IdKey, family.Name);

        return CreatedAtAction(
            nameof(GetByIdKey),
            new { idKey = family.IdKey },
            family);
    }

    /// <summary>
    /// Updates an existing family's basic details.
    /// </summary>
    /// <param name="idKey">The family's IdKey</param>
    /// <param name="request">Family update details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated family details</returns>
    /// <response code="200">Family updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Family not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpPut("{idKey}")]
    [ProducesResponseType(typeof(FamilyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public Task<IActionResult> Update(
        string idKey,
        [FromBody] UpdateFamilyRequest request,
        CancellationToken ct = default)
    {
        // Note: This requires IFamilyService.UpdateAsync method to be implemented
        logger.LogWarning("FamilyService.UpdateAsync not yet implemented");

        return Task.FromResult<IActionResult>(NotFound(new ProblemDetails
        {
            Title = "Not implemented",
            Detail = "Family update endpoint requires IFamilyService.UpdateAsync to be implemented",
            Status = StatusCodes.Status404NotFound,
            Instance = HttpContext.Request.Path
        }));
    }

    /// <summary>
    /// Adds a member to a family.
    /// </summary>
    /// <param name="idKey">The family's IdKey</param>
    /// <param name="request">Add member request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Added family member details</returns>
    /// <response code="201">Member added successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Family or person not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost("{idKey}/members")]
    [ProducesResponseType(typeof(FamilyMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddMember(
        string idKey,
        [FromBody] AddFamilyMemberRequest request,
        CancellationToken ct = default)
    {
        var result = await familyService.AddFamilyMemberAsync(idKey, request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to add family member: FamilyIdKey={FamilyIdKey}, Code={Code}, Message={Message}",
                idKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Resource not found",
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

        var member = result.Value!;

        logger.LogInformation(
            "Family member added successfully: FamilyIdKey={FamilyIdKey}, PersonIdKey={PersonIdKey}",
            idKey, member.Person.IdKey);

        return CreatedAtAction(
            nameof(GetMembers),
            new { idKey },
            member);
    }

    /// <summary>
    /// Removes a member from a family.
    /// </summary>
    /// <param name="idKey">The family's IdKey</param>
    /// <param name="personIdKey">The person's IdKey to remove</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Member removed successfully</response>
    /// <response code="404">Family or person not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpDelete("{idKey}/members/{personIdKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemoveMember(
        string idKey,
        string personIdKey,
        CancellationToken ct = default)
    {
        var result = await familyService.RemoveFamilyMemberAsync(idKey, personIdKey, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to remove family member: FamilyIdKey={FamilyIdKey}, PersonIdKey={PersonIdKey}, Code={Code}, Message={Message}",
                idKey, personIdKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Resource not found",
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

        logger.LogInformation(
            "Family member removed successfully: FamilyIdKey={FamilyIdKey}, PersonIdKey={PersonIdKey}",
            idKey, personIdKey);

        return NoContent();
    }
}
