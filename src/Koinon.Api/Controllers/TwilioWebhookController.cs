using Koinon.Api.Filters;
using Koinon.Application.DTOs.Communications;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koinon.Api.Controllers;

/// <summary>
/// Controller for receiving Twilio webhook callbacks.
/// Handles delivery status updates and other notifications from Twilio.
/// </summary>
/// <remarks>
/// This controller uses [AllowAnonymous] because Twilio cannot provide JWT tokens.
/// Security is enforced via [ValidateTwilioSignature] which validates the
/// X-Twilio-Signature header to ensure requests are legitimately from Twilio.
/// </remarks>
[ApiController]
[Route("api/v1/webhooks/twilio")]
[AllowAnonymous] // Twilio cannot provide JWT tokens
[ValidateTwilioSignature] // Validates X-Twilio-Signature header
public class TwilioWebhookController(
    ILogger<TwilioWebhookController> logger,
    ISmsDeliveryStatusService smsDeliveryStatusService) : ControllerBase
{
    /// <summary>
    /// Receives SMS delivery status callback from Twilio.
    /// </summary>
    /// <param name="payload">Twilio webhook payload (form-urlencoded)</param>
    /// <returns>204 NoContent on success</returns>
    /// <response code="204">Status callback received successfully</response>
    /// <remarks>
    /// Twilio sends status updates as application/x-www-form-urlencoded POST requests.
    /// The X-Twilio-Signature header is validated by the ValidateTwilioSignature filter
    /// to ensure request authenticity.
    /// See: https://www.twilio.com/docs/usage/webhooks/webhooks-security
    /// </remarks>
    [HttpPost("status")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReceiveStatusCallback(
        [FromForm] TwilioWebhookDto payload,
        CancellationToken cancellationToken)
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

        // Persist status to CommunicationRecipient
        if (!string.IsNullOrEmpty(payload.MessageSid) && !string.IsNullOrEmpty(payload.MessageStatus))
        {
            await smsDeliveryStatusService.UpdateDeliveryStatusAsync(
                payload.MessageSid,
                payload.MessageStatus,
                payload.ErrorCode,
                payload.ErrorMessage,
                cancellationToken);
        }
        else
        {
            logger.LogWarning(
                "Received Twilio webhook with missing required fields: MessageSid={MessageSid}, MessageStatus={MessageStatus}",
                payload.MessageSid,
                payload.MessageStatus);
        }

        // Twilio expects 2xx response to confirm receipt
        return NoContent();
    }
}
