using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for email tracking operations (opens and clicks).
/// These endpoints are public and used in email communications.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class TrackingController(
    ICommunicationAnalyticsService analyticsService,
    ILogger<TrackingController> logger) : ControllerBase
{
    // 1x1 transparent GIF as base64
    private static readonly byte[] TransparentPixel = Convert.FromBase64String(
        "R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");

    /// <summary>
    /// Tracking pixel endpoint. Returns a 1x1 transparent GIF and records an email open.
    /// </summary>
    /// <param name="recipientIdKey">The recipient's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>1x1 transparent GIF</returns>
    /// <response code="200">Returns tracking pixel</response>
    [HttpGet("pixel/{recipientIdKey}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TrackOpen(string recipientIdKey, CancellationToken ct = default)
    {
        // Record the open synchronously - the operation is fast and the 1x1 pixel response is tiny
        try
        {
            await analyticsService.RecordOpenAsync(recipientIdKey, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recording email open for recipient {IdKey}", recipientIdKey);
        }

        // Return the tracking pixel
        return File(TransparentPixel, "image/gif");
    }

    /// <summary>
    /// Click tracking endpoint. Records a click and redirects to the target URL.
    /// </summary>
    /// <param name="recipientIdKey">The recipient's IdKey</param>
    /// <param name="url">The target URL to redirect to</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Redirect to target URL</returns>
    /// <response code="302">Redirects to target URL</response>
    /// <response code="400">Invalid or missing URL</response>
    [HttpGet("click/{recipientIdKey}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TrackClick(
        string recipientIdKey,
        [FromQuery] string? url,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogWarning("Click tracking called without URL for recipient {IdKey}", recipientIdKey);

            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "URL parameter is required",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        // Validate the URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var targetUri))
        {
            logger.LogWarning(
                "Invalid URL provided for click tracking: Recipient={IdKey}, URL={URL}",
                recipientIdKey, url);

            return BadRequest(new ProblemDetails
            {
                Title = "Invalid URL",
                Detail = "The provided URL is not valid",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        // Security: Only allow relative URLs or same-host URLs to prevent open redirect attacks
        // For MVP, reject external redirects
        var requestHost = HttpContext.Request.Host.Host;
        if (targetUri.Host != requestHost)
        {
            logger.LogWarning(
                "External redirect blocked for security: Recipient={IdKey}, URL={URL}",
                recipientIdKey, url);

            return BadRequest(new ProblemDetails
            {
                Title = "Invalid URL",
                Detail = "External redirects are not allowed for security reasons",
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }

        // Record the click synchronously before redirecting
        try
        {
            await analyticsService.RecordClickAsync(recipientIdKey, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recording click for recipient {IdKey}", recipientIdKey);
        }

        logger.LogInformation(
            "Click tracked for recipient {IdKey}, redirecting to {URL}",
            recipientIdKey, targetUri);

        // Redirect to the target URL
        return Redirect(targetUri.ToString());
    }
}
