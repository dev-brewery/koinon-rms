namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for validating Twilio webhook request signatures.
/// Ensures that incoming webhook requests are legitimately from Twilio
/// and haven't been tampered with.
/// </summary>
public interface ITwilioSignatureValidator
{
    /// <summary>
    /// Validates the signature of an incoming Twilio webhook request.
    /// </summary>
    /// <param name="url">The full URL of the webhook endpoint (including query string)</param>
    /// <param name="parameters">Form parameters from the POST body (or query parameters for GET)</param>
    /// <param name="signature">The X-Twilio-Signature header value</param>
    /// <param name="sourceIp">Optional source IP address for additional validation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the request signature is valid, false otherwise</returns>
    /// <remarks>
    /// Validation process:
    /// 1. Verifies signature matches expected HMAC-SHA1 hash
    /// 2. Optionally validates source IP against allowed ranges
    /// 3. Returns true only if all checks pass
    /// 
    /// See: https://www.twilio.com/docs/usage/webhooks/webhooks-security
    /// </remarks>
    Task<bool> ValidateSignatureAsync(
        string url,
        IDictionary<string, string> parameters,
        string signature,
        string? sourceIp = null,
        CancellationToken cancellationToken = default);
}
