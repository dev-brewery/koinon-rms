using Koinon.Api.Filters;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for DefinedType and DefinedValue management.
/// DefinedTypes are system-level lookup tables; admins can manage their values.
/// </summary>
[ApiController]
[Route("api/v1/defined-types")]
[Authorize]
[ValidateIdKey]
public class DefinedTypesController(
    IDefinedTypeService definedTypeService,
    ILogger<DefinedTypesController> logger) : ControllerBase
{
    /// <summary>
    /// Gets all DefinedTypes ordered by Category then Name.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Summary list of all DefinedTypes</returns>
    /// <response code="200">Returns list of DefinedTypes</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DefinedTypeSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var types = await definedTypeService.GetAllTypesAsync(ct);

        logger.LogInformation("Retrieved {Count} defined types", types.Count);

        return Ok(new { data = types });
    }

    /// <summary>
    /// Gets a specific DefinedType with all its values.
    /// </summary>
    /// <param name="idKey">The DefinedType's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>DefinedType with values</returns>
    /// <response code="200">Returns DefinedType details</response>
    /// <response code="404">DefinedType not found</response>
    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(DefinedTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdKey(string idKey, CancellationToken ct = default)
    {
        var result = await definedTypeService.GetTypeByIdKeyAsync(idKey, ct);

        if (result.IsFailure)
        {
            logger.LogDebug("DefinedType not found: IdKey={IdKey}", idKey);

            return NotFound(new ProblemDetails
            {
                Title = "DefinedType not found",
                Detail = result.Error!.Message,
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        logger.LogDebug("DefinedType retrieved: IdKey={IdKey}, Name={Name}", idKey, result.Value!.Name);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Creates a new DefinedValue within the specified DefinedType.
    /// </summary>
    /// <param name="typeIdKey">The DefinedType's IdKey</param>
    /// <param name="request">Value creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created DefinedValue</returns>
    /// <response code="201">Value created successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">DefinedType not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost("{typeIdKey}/values")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DefinedValueManagementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateValue(
        string typeIdKey,
        [FromBody] CreateDefinedValueRequest request,
        CancellationToken ct = default)
    {
        var result = await definedTypeService.CreateValueAsync(typeIdKey, request, ct);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to create DefinedValue: TypeIdKey={TypeIdKey}, Code={Code}, Message={Message}",
                typeIdKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "DefinedType not found",
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

        var value = result.Value!;

        logger.LogInformation(
            "DefinedValue created: IdKey={IdKey}, Value={Value}",
            value.IdKey, value.Value);

        return CreatedAtAction(
            nameof(GetByIdKey),
            new { idKey = typeIdKey },
            new { data = value });
    }

    /// <summary>
    /// Updates an existing DefinedValue.
    /// </summary>
    /// <param name="valueIdKey">The DefinedValue's IdKey</param>
    /// <param name="request">Value update details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated DefinedValue</returns>
    /// <response code="200">Value updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">DefinedValue not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpPut("values/{valueIdKey}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DefinedValueManagementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateValue(
        string valueIdKey,
        [FromBody] UpdateDefinedValueRequest request,
        CancellationToken ct = default)
    {
        var result = await definedTypeService.UpdateValueAsync(valueIdKey, request, ct);

        if (result.IsFailure)
        {
            logger.LogDebug(
                "Failed to update DefinedValue: IdKey={IdKey}, Code={Code}, Message={Message}",
                valueIdKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "DefinedValue not found",
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

        var value = result.Value!;

        logger.LogInformation(
            "DefinedValue updated: IdKey={IdKey}, Value={Value}",
            value.IdKey, value.Value);

        return Ok(new { data = value });
    }

    /// <summary>
    /// Soft-deactivates a DefinedValue (sets IsActive = false).
    /// </summary>
    /// <param name="valueIdKey">The DefinedValue's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Value deactivated successfully</response>
    /// <response code="404">DefinedValue not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpDelete("values/{valueIdKey}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteValue(string valueIdKey, CancellationToken ct = default)
    {
        var result = await definedTypeService.DeleteValueAsync(valueIdKey, ct);

        if (result.IsFailure)
        {
            logger.LogDebug(
                "Failed to deactivate DefinedValue: IdKey={IdKey}, Code={Code}, Message={Message}",
                valueIdKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "DefinedValue not found",
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

        logger.LogInformation("DefinedValue deactivated: IdKey={IdKey}", valueIdKey);

        return NoContent();
    }

    /// <summary>
    /// Bulk-updates the Order of DefinedValues within a DefinedType.
    /// </summary>
    /// <param name="typeIdKey">The DefinedType's IdKey</param>
    /// <param name="request">Ordered list of value IdKeys with their desired positions</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Values reordered successfully</response>
    /// <response code="404">DefinedType not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost("{typeIdKey}/values/reorder")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReorderValues(
        string typeIdKey,
        [FromBody] ReorderDefinedValuesRequest request,
        CancellationToken ct = default)
    {
        var result = await definedTypeService.ReorderValuesAsync(typeIdKey, request, ct);

        if (result.IsFailure)
        {
            logger.LogDebug(
                "Failed to reorder DefinedValues: TypeIdKey={TypeIdKey}, Code={Code}, Message={Message}",
                typeIdKey, result.Error!.Code, result.Error.Message);

            return result.Error.Code switch
            {
                "NOT_FOUND" => NotFound(new ProblemDetails
                {
                    Title = "DefinedType not found",
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

        logger.LogInformation("DefinedValues reordered for type: TypeIdKey={TypeIdKey}", typeIdKey);

        return NoContent();
    }
}
