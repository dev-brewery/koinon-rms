using Koinon.Api.Helpers;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for person management operations.
/// Provides endpoints for searching, creating, updating, and retrieving people.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PeopleController(
    IPersonService personService,
    ILogger<PeopleController> logger) : ControllerBase
{
    /// <summary>
    /// Searches for people with optional filters and pagination.
    /// </summary>
    /// <param name="query">Full-text search query</param>
    /// <param name="campusId">Filter by campus IdKey</param>
    /// <param name="recordStatusId">Filter by record status IdKey</param>
    /// <param name="connectionStatusId">Filter by connection status IdKey</param>
    /// <param name="includeInactive">Include inactive records</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 25, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of people</returns>
    /// <response code="200">Returns paginated list of people</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PersonSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? query,
        [FromQuery] string? campusId,
        [FromQuery] string? recordStatusId,
        [FromQuery] string? connectionStatusId,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        // Validate optional IdKey parameters
        if (!string.IsNullOrWhiteSpace(campusId) && !IdKeyValidator.IsValid(campusId))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("campusId"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        if (!string.IsNullOrWhiteSpace(recordStatusId) && !IdKeyValidator.IsValid(recordStatusId))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("recordStatusId"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        if (!string.IsNullOrWhiteSpace(connectionStatusId) && !IdKeyValidator.IsValid(connectionStatusId))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("connectionStatusId"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var parameters = new PersonSearchParameters
        {
            Query = query,
            CampusId = campusId,
            RecordStatusId = recordStatusId,
            ConnectionStatusId = connectionStatusId,
            IncludeInactive = includeInactive,
            Page = page,
            PageSize = pageSize
        };

        parameters.ValidatePageSize();

        var result = await personService.SearchAsync(parameters, ct);

        logger.LogInformation(
            "People search completed: Query={Query}, Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}",
            query, result.Page, result.PageSize, result.TotalCount);

        return Ok(result);
    }

    /// <summary>
    /// Gets a person by their IdKey with full details.
    /// </summary>
    /// <param name="idKey">The person's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Person details</returns>
    /// <response code="200">Returns person details</response>
    /// <response code="404">Person not found</response>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdKey(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyValidator.IsValid(idKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("idKey"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var person = await personService.GetByIdKeyAsync(idKey, ct);

        if (person == null)
        {
            logger.LogWarning("Person not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Person not found",
                Detail = $"No person found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogInformation("Person retrieved: IdKey={IdKey}, Name={Name}", idKey, person.FullName);

        return Ok(person);
    }

    /// <summary>
    /// Creates a new person.
    /// </summary>
    /// <param name="request">Person creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created person details</returns>
    /// <response code="201">Person created successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreatePersonRequest request, CancellationToken ct = default)
    {
        var result = await personService.CreateAsync(request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to create person: Code={Code}, Message={Message}",
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

        var person = result.Value!;

        logger.LogInformation(
            "Person created successfully: IdKey={IdKey}, Name={Name}",
            person.IdKey, person.FullName);

        return CreatedAtAction(
            nameof(GetByIdKey),
            new { idKey = person.IdKey },
            person);
    }

    /// <summary>
    /// Updates an existing person.
    /// </summary>
    /// <param name="idKey">The person's IdKey</param>
    /// <param name="request">Person update details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated person details</returns>
    /// <response code="200">Person updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Person not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpPut("{idKey}")]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        string idKey,
        [FromBody] UpdatePersonRequest request,
        CancellationToken ct = default)
    {
        if (!IdKeyValidator.IsValid(idKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("idKey"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await personService.UpdateAsync(idKey, request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to update person: IdKey={IdKey}, Code={Code}, Message={Message}",
                idKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Person not found",
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

        var person = result.Value!;

        logger.LogInformation(
            "Person updated successfully: IdKey={IdKey}, Name={Name}",
            person.IdKey, person.FullName);

        return Ok(person);
    }

    /// <summary>
    /// Soft-deletes a person (sets record status to inactive).
    /// </summary>
    /// <param name="idKey">The person's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Person deleted successfully</response>
    /// <response code="404">Person not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpDelete("{idKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyValidator.IsValid(idKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("idKey"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var result = await personService.DeleteAsync(idKey, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to delete person: IdKey={IdKey}, Code={Code}, Message={Message}",
                idKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Person not found",
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

        logger.LogInformation("Person soft-deleted successfully: IdKey={IdKey}", idKey);

        return NoContent();
    }

    /// <summary>
    /// Gets a person's family with all members.
    /// </summary>
    /// <param name="idKey">The person's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Family details with members</returns>
    /// <response code="200">Returns family details</response>
    /// <response code="404">Person not found or has no family</response>
    [HttpGet("{idKey}/family")]
    [ProducesResponseType(typeof(FamilySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFamily(string idKey, CancellationToken ct = default)
    {
        if (!IdKeyValidator.IsValid(idKey))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid IdKey format",
                Detail = IdKeyValidator.GetErrorMessage("idKey"),
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var family = await personService.GetFamilyAsync(idKey, ct);

        if (family == null)
        {
            logger.LogWarning("Family not found for person: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Family not found",
                Detail = $"No family found for person with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogInformation(
            "Family retrieved for person: IdKey={IdKey}, FamilyName={FamilyName}",
            idKey, family.Name);

        return Ok(family);
    }
}
