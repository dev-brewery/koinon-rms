using Koinon.Application.DTOs.Communications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for receiving Twilio webhook callbacks.
/// Handles delivery status updates and other notifications from Twilio.
/// </summary>
[ApiController]
[Route("api/v1/webhooks/twilio")]
[AllowAnonymous] // Twilio cannot provide JWT tokens
public class TwilioWebhookController(ILogger<TwilioWebhookController> logger) : ControllerBase
{
    /// <summary>
    /// Receives SMS delivery status callback from Twilio.
    /// </summary>
    /// <param name="payload">Twilio webhook payload (form-urlencoded)</param>
    /// <returns>204 NoContent on success</returns>
    /// <response code="204">Status callback received successfully</response>
    /// <remarks>
    /// Twilio sends status updates as application/x-www-form-urlencoded POST requests.
    /// For security in production, we should validate the X-Twilio-Signature header.
    /// See: https://www.twilio.com/docs/usage/webhooks/webhooks-security
    /// </remarks>
    [HttpPost("status")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ReceiveStatusCallback([FromForm] TwilioWebhookDto payload)
    {
        // Log the status update for monitoring and debugging
        logger.LogInformation(
            "Twilio status callback: MessageSid={MessageSid}, Status={Status}, To={To}, ErrorCode={ErrorCode}",
            payload.MessageSid,
            payload.MessageStatus,
            payload.To,
            payload.ErrorCode);

        // Log error details if delivery failed
        if (payload.ErrorCode.HasValue)
        {
            logger.LogWarning(
                "SMS delivery failed: MessageSid={MessageSid}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}",
                payload.MessageSid,
                payload.ErrorCode,
                payload.ErrorMessage);
        }

        // TODO(#400): Validate X-Twilio-Signature header to ensure request authenticity
        // TODO(#401): Persist status to CommunicationRecipient when integrated with communications feature

        // Twilio expects 2xx response to confirm receipt
        return NoContent();
    }
}
