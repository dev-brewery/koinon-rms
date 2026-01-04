using System.Net;
using Koinon.Application.Interfaces;
using Koinon.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio.Security;

namespace Koinon.Infrastructure.Services;

/// <summary>
/// Service for validating Twilio webhook request signatures.
/// Implements signature validation using HMAC-SHA1 and optional IP allowlist checking.
/// </summary>
public class TwilioSignatureValidator : ITwilioSignatureValidator
{
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioSignatureValidator> _logger;
    private readonly RequestValidator _requestValidator;

    /// <summary>
    /// Initializes a new instance of the TwilioSignatureValidator.
    /// </summary>
    /// <param name="options">Twilio configuration options containing AuthToken and validation settings.</param>
    /// <param name="logger">Logger for validation events and security warnings.</param>
    public TwilioSignatureValidator(
        IOptions<TwilioOptions> options,
        ILogger<TwilioSignatureValidator> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Initialize Twilio's RequestValidator with the auth token
        _requestValidator = new RequestValidator(_options.AuthToken ?? string.Empty);
    }

    /// <summary>
    /// Validates the signature of an incoming Twilio webhook request.
    /// </summary>
    /// <param name="url">The full URL of the webhook endpoint (including query string).</param>
    /// <param name="parameters">Form parameters from the POST body (or query parameters for GET).</param>
    /// <param name="signature">The X-Twilio-Signature header value.</param>
    /// <param name="sourceIp">Optional source IP address for additional validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the request signature is valid and IP check passes (if configured), false otherwise.</returns>
    public Task<bool> ValidateSignatureAsync(
        string url,
        IDictionary<string, string> parameters,
        string signature,
        string? sourceIp = null,
        CancellationToken cancellationToken = default)
    {
        // Check if validation is disabled (e.g., development environment)
        if (!_options.EnableWebhookValidation)
        {
            _logger.LogWarning(
                "Twilio webhook validation is DISABLED. Request from {SourceIp} to {Url} was not validated",
                sourceIp ?? "unknown",
                url);
            return Task.FromResult(true);
        }

        // Validate IP allowlist if configured
        if (!string.IsNullOrWhiteSpace(sourceIp) &&
            _options.AllowedIpRanges is { Length: > 0 })
        {
            if (!IsIpInAllowedRanges(sourceIp, _options.AllowedIpRanges))
            {
                _logger.LogWarning(
                    "Twilio webhook request rejected: Source IP {SourceIp} not in allowed ranges for URL {Url}",
                    sourceIp,
                    url);
                return Task.FromResult(false);
            }

            _logger.LogDebug(
                "Source IP {SourceIp} validated against allowed ranges for URL {Url}",
                sourceIp,
                url);
        }

        // Validate the signature using Twilio's RequestValidator
        bool isValid = _requestValidator.Validate(url, parameters, signature);

        if (!isValid)
        {
            _logger.LogWarning(
                "Twilio webhook signature validation FAILED for URL {Url} from IP {SourceIp}. " +
                "This may indicate request tampering or configuration mismatch",
                url,
                sourceIp ?? "unknown");
        }
        else
        {
            _logger.LogDebug(
                "Twilio webhook signature validated successfully for URL {Url}",
                url);
        }

        return Task.FromResult(isValid);
    }

    /// <summary>
    /// Checks if an IP address is within any of the allowed CIDR ranges.
    /// </summary>
    /// <param name="ipAddress">The IP address to check (IPv4 or IPv6).</param>
    /// <param name="allowedRanges">Array of CIDR notation ranges (e.g., "54.172.60.0/23").</param>
    /// <returns>True if the IP is in any of the allowed ranges, false otherwise.</returns>
    private bool IsIpInAllowedRanges(string ipAddress, string[] allowedRanges)
    {
        if (!IPAddress.TryParse(ipAddress, out var ip))
        {
            _logger.LogWarning(
                "Invalid IP address format: {IpAddress}",
                ipAddress);
            return false;
        }

        foreach (var range in allowedRanges)
        {
            if (IsIpInCidrRange(ip, range))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if an IP address is within a CIDR range.
    /// </summary>
    /// <param name="ipAddress">The IP address to check.</param>
    /// <param name="cidrRange">CIDR notation range (e.g., "54.172.60.0/23").</param>
    /// <returns>True if the IP is in the range, false otherwise.</returns>
    private bool IsIpInCidrRange(IPAddress ipAddress, string cidrRange)
    {
        try
        {
            var parts = cidrRange.Split('/');
            if (parts.Length != 2)
            {
                _logger.LogWarning(
                    "Invalid CIDR range format: {CidrRange}",
                    cidrRange);
                return false;
            }

            if (!IPAddress.TryParse(parts[0], out var rangeAddress) ||
                !int.TryParse(parts[1], out var prefixLength))
            {
                _logger.LogWarning(
                    "Invalid CIDR range components: {CidrRange}",
                    cidrRange);
                return false;
            }

            // Ensure both IPs are the same address family
            if (ipAddress.AddressFamily != rangeAddress.AddressFamily)
            {
                return false;
            }

            var ipBytes = ipAddress.GetAddressBytes();
            var rangeBytes = rangeAddress.GetAddressBytes();

            // Calculate the number of bits to check
            int bytesToCheck = prefixLength / 8;
            int remainingBits = prefixLength % 8;

            // Check full bytes
            for (int i = 0; i < bytesToCheck; i++)
            {
                if (ipBytes[i] != rangeBytes[i])
                {
                    return false;
                }
            }

            // Check remaining bits if any
            if (remainingBits > 0)
            {
                byte mask = (byte)(0xFF << (8 - remainingBits));
                if ((ipBytes[bytesToCheck] & mask) != (rangeBytes[bytesToCheck] & mask))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error validating IP {IpAddress} against CIDR range {CidrRange}",
                ipAddress,
                cidrRange);
            return false;
        }
    }
}
