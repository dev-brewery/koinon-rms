using Koinon.Api.Filters;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for managing person communication preferences.
/// Handles opt-in/opt-out preferences for email and SMS communications.
/// </summary>
[ApiController]
[Route("api/v1/people/{personIdKey}/communication-preferences")]
[Authorize]
public class CommunicationPreferencesController(
    ICommunicationPreferenceService communicationPreferenceService,
    ILogger<CommunicationPreferencesController> logger) : ControllerBase
{
    /// <summary>
    /// Gets all communication preferences for a person.
    /// Returns preferences for all communication types (Email, SMS), using defaults for types without explicit preferences.
    /// </summary>
    /// <param name="personIdKey">The person's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of communication preferences</returns>
    /// <response code="200">Returns list of communication preferences</response>
    [HttpGet]
    [ValidateIdKey]
    [ProducesResponseType(typeof(List<CommunicationPreferenceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(string personIdKey, CancellationToken ct = default)
    {
        var preferences = await communicationPreferenceService.GetByPersonAsync(personIdKey, ct);

        logger.LogDebug(
            "Communication preferences retrieved: PersonIdKey={PersonIdKey}, Count={Count}",
            personIdKey, preferences.Count);

        return Ok(new { data = preferences });
    }

    /// <summary>
    /// Updates a person's preference for a specific communication type.
    /// Creates a new preference record if one doesn't exist.
    /// </summary>
    /// <param name="personIdKey">The person's IdKey</param>
    /// <param name="type">The communication type (Email or Sms)</param>
    /// <param name="request">The preference update data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated preference</returns>
    /// <response code="200">Preference updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Person not found</response>
    [HttpPut("{type}")]
    [ValidateIdKey]
    [ProducesResponseType(typeof(CommunicationPreferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSingle(
        string personIdKey,
        string type,
        [FromBody] UpdateCommunicationPreferenceDto request,
        CancellationToken ct = default)
    {
        var result = await communicationPreferenceService.UpdateAsync(personIdKey, type, request, ct);

        if (result.IsFailure)
        {
            logger.LogDebug(
                "Failed to update communication preference: PersonIdKey={PersonIdKey}, Type={Type}, Code={Code}, Message={Message}",
                personIdKey, type, result.Error!.Code, result.Error.Message);

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
                _ => BadRequest(new ProblemDetails
                {
                    Title = result.Error.Code,
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                })
            };
        }

        logger.LogInformation(
            "Communication preference updated: PersonIdKey={PersonIdKey}, Type={Type}, IsOptedOut={IsOptedOut}",
            personIdKey, type, result.Value!.IsOptedOut);

        return Ok(new { data = result.Value });
    }

    /// <summary>
    /// Updates multiple communication preferences for a person in a single operation.
    /// </summary>
    /// <param name="personIdKey">The person's IdKey</param>
    /// <param name="request">The bulk update data with list of preferences</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of updated preferences</returns>
    /// <response code="200">Preferences updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Person not found</response>
    [HttpPut]
    [ValidateIdKey]
    [ProducesResponseType(typeof(List<CommunicationPreferenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkUpdate(
        string personIdKey,
        [FromBody] BulkUpdatePreferencesDto request,
        CancellationToken ct = default)
    {
        var result = await communicationPreferenceService.BulkUpdateAsync(personIdKey, request, ct);

        if (result.IsFailure)
        {
            logger.LogDebug(
                "Failed to bulk update communication preferences: PersonIdKey={PersonIdKey}, Code={Code}, Message={Message}",
                personIdKey, result.Error!.Code, result.Error.Message);

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
                _ => BadRequest(new ProblemDetails
                {
                    Title = result.Error.Code,
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                })
            };
        }

        logger.LogInformation(
            "Communication preferences bulk updated: PersonIdKey={PersonIdKey}, Count={Count}",
            personIdKey, result.Value!.Count);

        return Ok(new { data = result.Value });
    }
}
