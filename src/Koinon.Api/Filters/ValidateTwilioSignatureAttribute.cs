using Koinon.Application.Interfaces;
using Koinon.Infrastructure.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Koinon.Api.Filters;

/// <summary>
/// Authorization filter for Twilio webhook endpoints.
/// Validates the X-Twilio-Signature header to ensure requests are legitimately from Twilio
/// and haven't been tampered with.
/// </summary>
/// <remarks>
/// This filter:
/// 1. Enables request body buffering so form data can be read multiple times
/// 2. Extracts the full URL, form parameters, signature header, and client IP
/// 3. Validates the signature using ITwilioSignatureValidator
/// 4. Returns 403 Forbidden if validation fails
/// 
/// See: https://www.twilio.com/docs/usage/webhooks/webhooks-security
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ValidateTwilioSignatureAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Get required services from DI container
        var validator = context.HttpContext.RequestServices
            .GetRequiredService<ITwilioSignatureValidator>();

        var options = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<TwilioOptions>>();

        // If webhook validation is disabled in configuration, allow the request through
        if (!options.Value.EnableWebhookValidation)
        {
            return;
        }

        // Enable buffering so the request body can be read multiple times
        context.HttpContext.Request.EnableBuffering();

        // Extract the X-Twilio-Signature header
        if (!context.HttpContext.Request.Headers.TryGetValue("X-Twilio-Signature", out var signature) ||
            string.IsNullOrWhiteSpace(signature))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Title = "Missing Twilio signature",
                Detail = "X-Twilio-Signature header is required for webhook validation",
                Status = StatusCodes.Status403Forbidden,
                Instance = context.HttpContext.Request.Path
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        // Build the full URL (scheme + host + path + query string)
        var request = context.HttpContext.Request;
        var url = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";

        // Extract form parameters from the request body
        var parameters = new Dictionary<string, string>();

        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync(context.HttpContext.RequestAborted);
            foreach (var key in form.Keys)
            {
                parameters[key] = form[key].ToString();
            }
        }

        // Extract client IP address for optional IP range validation
        var sourceIp = context.HttpContext.Connection.RemoteIpAddress?.ToString();

        // Validate the signature
        var isValid = await validator.ValidateSignatureAsync(
            url,
            parameters,
            signature!,
            sourceIp,
            context.HttpContext.RequestAborted);

        if (!isValid)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Title = "Invalid Twilio signature",
                Detail = "The X-Twilio-Signature header validation failed. This request may not be from Twilio or may have been tampered with.",
                Status = StatusCodes.Status403Forbidden,
                Instance = context.HttpContext.Request.Path
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        // If valid, allow the request to continue to the controller action
    }
}
