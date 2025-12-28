using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// People controller for testing graph generation.
/// Follows project conventions: [Route] with /api/v1/, {idKey} routes, ProblemDetails, response envelope.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PeopleController(
    IPersonService personService,
    ILogger<PeopleController> logger) : ControllerBase
{
    /// <summary>
    /// Searches for people with pagination.
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of people</returns>
    /// <response code="200">Returns paginated list of people</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PersonSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var parameters = new PersonSearchParameters
        {
            Query = query,
            Page = page,
            PageSize = pageSize
        };

        var result = await personService.SearchAsync(parameters, ct);

        logger.LogInformation(
            "People search completed: Query={Query}, Page={Page}, TotalCount={TotalCount}",
            query, result.Page, result.TotalCount);

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
    /// Gets a person by their IdKey.
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
        var person = await personService.GetByIdKeyAsync(idKey, ct);

        if (person == null)
        {
            logger.LogDebug("Person not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Person not found",
                Detail = $"No person found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug("Person retrieved: IdKey={IdKey}, Name={Name}", idKey, person.FullName);

        return Ok(new { data = person });
    }

    /// <summary>
    /// Creates a new person.
    /// </summary>
    /// <param name="request">Person creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created person details</returns>
    /// <response code="201">Person created successfully</response>
    /// <response code="400">Validation failed</response>
    [HttpPost]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePersonRequest request, CancellationToken ct = default)
    {
        var result = await personService.CreateAsync(request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to create person: Code={Code}, Message={Message}",
                result.Error!.Code, result.Error.Message);

            return BadRequest(new ProblemDetails
            {
                Title = result.Error.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        var person = result.Value!;

        logger.LogInformation(
            "Person created successfully: IdKey={IdKey}, Name={Name}",
            person.IdKey, person.FullName);

        return CreatedAtAction(
            nameof(GetByIdKey),
            new { idKey = person.IdKey },
            new { data = person });
    }

    /// <summary>
    /// Updates an existing person.
    /// </summary>
    /// <param name="idKey">The person's IdKey</param>
    /// <param name="request">Person update details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated person details</returns>
    /// <response code="200">Person updated successfully</response>
    /// <response code="404">Person not found</response>
    [HttpPut("{idKey}")]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string idKey,
        [FromBody] UpdatePersonRequest request,
        CancellationToken ct = default)
    {
        var result = await personService.UpdateAsync(idKey, request, ct);

        if (result.IsFailure)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Person not found",
                Detail = result.Error!.Message,
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Deletes a person.
    /// </summary>
    /// <param name="idKey">The person's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Person deleted successfully</response>
    /// <response code="404">Person not found</response>
    [HttpDelete("{idKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string idKey, CancellationToken ct = default)
    {
        var result = await personService.DeleteAsync(idKey, ct);

        if (result.IsFailure)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Person not found",
                Detail = result.Error!.Message,
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogInformation("Person deleted successfully: IdKey={IdKey}", idKey);

        return NoContent();
    }
}
