using Koinon.Api.Filters;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for communication template management operations.
/// Provides endpoints for creating, managing, and organizing reusable communication templates.
/// </summary>
[ApiController]
[Route("api/v1/communication-templates")]
[Authorize]
[ValidateIdKey]
public class CommunicationTemplatesController(
    ICommunicationTemplateService communicationTemplateService,
    ILogger<CommunicationTemplatesController> logger) : ControllerBase
{
    /// <summary>
    /// Searches for communication templates with optional filters and pagination.
    /// </summary>
    /// <param name="type">Filter by communication type (Email, SMS)</param>
    /// <param name="isActive">Filter by active status (true/false)</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 25, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of communication templates</returns>
    /// <response code="200">Returns paginated list of templates</response>
    [HttpGet]
    [ProducesResponseType(typeof(Application.Common.PagedResult<CommunicationTemplateSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? type = null,
        [FromQuery] bool? isActive = null,
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

        var result = await communicationTemplateService.SearchAsync(type, isActive, page, pageSize, ct);

        logger.LogInformation(
            "Communication template search completed: Page={Page}, PageSize={PageSize}, Type={Type}, IsActive={IsActive}, TotalCount={TotalCount}",
            result.Page, result.PageSize, type ?? "all", isActive?.ToString() ?? "all", result.TotalCount);

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
    /// Gets a communication template by its IdKey with full details.
    /// </summary>
    /// <param name="idKey">The template's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Template details</returns>
    /// <response code="200">Returns template details</response>
    /// <response code="404">Template not found</response>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(CommunicationTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdKey(string idKey, CancellationToken ct = default)
    {
        var template = await communicationTemplateService.GetByIdKeyAsync(idKey, ct);

        if (template == null)
        {
            logger.LogDebug("Communication template not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "Communication template not found",
                Detail = $"No communication template found with IdKey '{idKey}'",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug("Communication template retrieved: IdKey={IdKey}, Name={Name}", idKey, template.Name);

        return Ok(new { data = template });
    }

    /// <summary>
    /// Creates a new communication template.
    /// </summary>
    /// <param name="dto">Template creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created template details</returns>
    /// <response code="201">Template created successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost]
    [ProducesResponseType(typeof(CommunicationTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateCommunicationTemplateDto dto, CancellationToken ct = default)
    {
        var result = await communicationTemplateService.CreateAsync(dto, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to create communication template: Code={Code}, Message={Message}",
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

        var template = result.Value!;

        logger.LogInformation(
            "Communication template created: IdKey={IdKey}, Name={Name}, Type={Type}",
            template.IdKey,
            template.Name,
            template.CommunicationType);

        return CreatedAtAction(
            nameof(GetByIdKey),
            new { idKey = template.IdKey },
            new { data = template });
    }

    /// <summary>
    /// Updates an existing communication template.
    /// </summary>
    /// <param name="idKey">The template's IdKey</param>
    /// <param name="dto">Updated template details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated template details</returns>
    /// <response code="200">Template updated successfully</response>
    /// <response code="404">Template not found</response>
    /// <response code="400">Validation failed</response>
    /// <response code="422">Business rule violation</response>
    [HttpPut("{idKey}")]
    [ProducesResponseType(typeof(CommunicationTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        string idKey,
        [FromBody] UpdateCommunicationTemplateDto dto,
        CancellationToken ct = default)
    {
        var result = await communicationTemplateService.UpdateAsync(idKey, dto, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to update communication template {IdKey}: Code={Code}, Message={Message}",
                idKey,
                result.Error!.Code,
                result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Communication template not found",
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

        logger.LogInformation("Communication template updated: IdKey={IdKey}", idKey);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Deletes a communication template.
    /// </summary>
    /// <param name="idKey">The template's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Template deleted successfully</response>
    /// <response code="404">Template not found</response>
    /// <response code="422">Business rule violation (e.g., template in use)</response>
    [HttpDelete("{idKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(string idKey, CancellationToken ct = default)
    {
        var result = await communicationTemplateService.DeleteAsync(idKey, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to delete communication template {IdKey}: Code={Code}, Message={Message}",
                idKey,
                result.Error!.Code,
                result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "Communication template not found",
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

        logger.LogInformation("Communication template deleted: IdKey={IdKey}", idKey);

        return NoContent();
    }
}
