using System.Diagnostics;

namespace Koinon.Api.Middleware;

/// <summary>
/// Middleware that logs all HTTP requests with timing information.
/// Adds correlation IDs for request tracing across services.
/// </summary>
public class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Get or create correlation ID
        var correlationId = context.TraceIdentifier;
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.TraceIdentifier = correlationId;
        }

        // Add correlation ID to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-Id"] = correlationId;
            return Task.CompletedTask;
        });

        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        // Log request start
        logger.LogInformation(
            "HTTP {Method} {Path} started. CorrelationId: {CorrelationId}",
            requestMethod,
            requestPath,
            correlationId);

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Determine log level based on status code
            var statusCode = context.Response.StatusCode;
            var logLevel = statusCode switch
            {
                >= 500 => LogLevel.Error,
                >= 400 => LogLevel.Warning,
                _ => LogLevel.Information
            };

            logger.Log(
                logLevel,
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms. CorrelationId: {CorrelationId}",
                requestMethod,
                requestPath,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId);
        }
    }
}
