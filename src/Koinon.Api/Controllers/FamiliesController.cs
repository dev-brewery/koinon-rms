using Koinon.Api.Filters;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for family (household) management operations.
/// Provides endpoints for creating and managing family members.
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
    /// <param name="searchTerm">Optional search term to filter by family name</param>
    /// <param name="campusIdKey">Optional campus IdKey to filter by</param>
    /// <param name="includeInactive">Include inactive families (default: false)</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 25, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of families</returns>
    /// <response code="200">Returns paginated list of families</response>
    [HttpGet]
    [ValidateIdKey]
    [ProducesResponseType(typeof(PagedResult<FamilySummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] string? campusIdKey,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        // Validate pagination parameters
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1 || pageSize > 100)
        {
            pageSize = 25;
        }

        var result = await familyService.SearchAsync(
            searchTerm,
            campusIdKey,
            includeInactive,
            page,
            pageSize,
            ct);

        logger.LogInformation(
            "Family search completed: SearchTerm={SearchTerm}, CampusIdKey={CampusIdKey}, Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            searchTerm, campusIdKey, result.Page, result.PageSize, result.TotalCount);

        return Ok(new
        {
            data = result.Items,
            meta = new
            {
                page = result.Page,
                pageSize = result.PageSize,
                totalCount = result.TotalCount,
                totalPages = (int)Math.Ceiling(result.TotalCount / (double)result.PageSize)
            }
        });
    }

    /// <summary>
    /// Gets a family by their IdKey with full details including members.
    /// </summary>
    /// <param name="idKey">The family's unique IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Family details with members</returns>
    /// <response code="200">Returns the family details</response>
    /// <response code="403">Not authorized to access this family</response>
    /// <response code="404">Family not found</response>
    [HttpGet("{idKey}")]
    [ValidateIdKey]
    [ProducesResponseType(typeof(FamilyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdKey(string idKey, CancellationToken ct = default)
    {
        try
        {
            var family = await familyService.GetByIdKeyAsync(idKey, ct);

            if (family == null)
            {
                logger.LogDebug("Family not found: IdKey={IdKey}", idKey);
                return NotFound(new ProblemDetails
                {
                    Title = "Family not found",
                    Detail = $"No family found with IdKey '{idKey}'",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            logger.LogDebug("Family retrieved: IdKey={IdKey}, Name={Name}", idKey, family.Name);
            return Ok(new { data = family });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogDebug(ex, "Unauthorized access attempt to family: IdKey={IdKey}", idKey);
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Access denied",
                Detail = "You are not authorized to access this family",
                Status = StatusCodes.Status403Forbidden,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Gets all members of a family.
    /// </summary>
    /// <param name="idKey">The family's unique IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of family members</returns>
    /// <response code="200">Returns the family members</response>
    /// <response code="403">Not authorized to access this family</response>
    /// <response code="404">Family not found</response>
    [HttpGet("{idKey}/members")]
    [ValidateIdKey]
    [ProducesResponseType(typeof(List<FamilyMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMembers(string idKey, CancellationToken ct = default)
    {
        try
        {
            var family = await familyService.GetByIdKeyAsync(idKey, ct);

            if (family == null)
            {
                logger.LogDebug("Family not found: IdKey={IdKey}", idKey);
                return NotFound(new ProblemDetails
                {
                    Title = "Family not found",
                    Detail = $"No family found with IdKey '{idKey}'",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            logger.LogDebug(
                "Family members retrieved: IdKey={IdKey}, MemberCount={MemberCount}",
                idKey, family.Members.Count);

            return Ok(new { data = family.Members });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogDebug(ex, "Unauthorized access attempt to family members: IdKey={IdKey}", idKey);
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Access denied",
                Detail = "You are not authorized to access this family",
                Status = StatusCodes.Status403Forbidden,
                Instance = HttpContext.Request.Path
            });
        }
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
            new { data = family });
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
    [ValidateIdKey]
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
            logger.LogDebug(
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
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Access denied",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status403Forbidden,
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

        var member = result.Value!;

        logger.LogInformation(
            "Family member added successfully: FamilyIdKey={FamilyIdKey}, PersonIdKey={PersonIdKey}",
            idKey, member.Person.IdKey);

        return CreatedAtAction(
            nameof(GetMembers),
            new { idKey },
            new { data = member });
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
    [ValidateIdKey]
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
            logger.LogDebug(
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
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Access denied",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status403Forbidden,
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
