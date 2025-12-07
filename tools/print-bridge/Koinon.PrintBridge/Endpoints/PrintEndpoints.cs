using System.Runtime.Versioning;
using Koinon.PrintBridge.Services;

namespace Koinon.PrintBridge.Endpoints;

/// <summary>
/// Endpoints for print operations.
/// </summary>
[SupportedOSPlatform("windows")]
public static class PrintEndpoints
{
    /// <summary>
    /// Maps print-related endpoints to the application.
    /// </summary>
    public static void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/print")
            .WithName("Print");

        group.MapPost("/print", HandlePrint)
            .WithName("Print")
            .Produces<PrintJobResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapGet("/printers", HandleGetPrinters)
            .WithName("GetPrinters")
            .Produces<PrintersResponse>();

        group.MapGet("/health", HandleHealth)
            .WithName("Health")
            .Produces<HealthResponse>();

        group.MapPost("/test", HandleTestPrint)
            .WithName("TestPrint")
            .Produces<PrintJobResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// POST /api/v1/print/print - Send ZPL content to a printer
    /// </summary>
    private static IResult HandlePrint(
        PrintRequest request,
        ZplPrintService printService)
    {
        var result = printService.SendZplToPrinter(
            request.PrinterName,
            request.ZplContent,
            request.Copies ?? 1);

        if (!result.Success)
        {
            return Results.BadRequest(new ErrorResponse { Message = result.Message });
        }

        return Results.Ok(new PrintJobResponse
        {
            Success = true,
            Message = result.Message,
            PrinterName = result.PrinterName
        });
    }

    /// <summary>
    /// GET /api/v1/print/printers - List available printers
    /// </summary>
    private static IResult HandleGetPrinters(PrinterDiscoveryService printerService)
    {
        var printers = printerService.GetAvailablePrinters();

        return Results.Ok(new PrintersResponse
        {
            Printers = printers.Select(p => new PrinterResponse
            {
                Name = p.Name,
                Type = p.Type,
                Status = p.Status,
                IsDefault = p.IsDefault
            }).ToList()
        });
    }

    /// <summary>
    /// GET /api/v1/print/health - Check PrintBridge health status
    /// </summary>
    private static IResult HandleHealth(PrinterDiscoveryService printerService)
    {
        var defaultPrinter = printerService.GetDefaultPrinter();

        return Results.Ok(new HealthResponse
        {
            Status = "healthy",
            Version = "1.0.0",
            DefaultPrinter = defaultPrinter?.Name,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// POST /api/v1/print/test - Print a test label
    /// </summary>
    private static IResult HandleTestPrint(
        TestPrintRequest? request,
        ZplPrintService printService)
    {
        var result = printService.SendTestLabel(request?.PrinterName);

        if (!result.Success)
        {
            return Results.BadRequest(new ErrorResponse { Message = result.Message });
        }

        return Results.Ok(new PrintJobResponse
        {
            Success = true,
            Message = result.Message,
            PrinterName = result.PrinterName
        });
    }
}

/// <summary>
/// Request to print ZPL content.
/// </summary>
public record PrintRequest
{
    public string? PrinterName { get; init; }
    public required string ZplContent { get; init; }
    public int? Copies { get; init; }
}

/// <summary>
/// Request to print a test label.
/// </summary>
public record TestPrintRequest
{
    public string? PrinterName { get; init; }
}

/// <summary>
/// Response from a print operation.
/// </summary>
public record PrintJobResponse
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public string? PrinterName { get; init; }
}

/// <summary>
/// List of available printers.
/// </summary>
public record PrintersResponse
{
    public required List<PrinterResponse> Printers { get; init; }
}

/// <summary>
/// Information about a printer.
/// </summary>
public record PrinterResponse
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string Status { get; init; }
    public required bool IsDefault { get; init; }
}

/// <summary>
/// Health status of PrintBridge.
/// </summary>
public record HealthResponse
{
    public required string Status { get; init; }
    public required string Version { get; init; }
    public string? DefaultPrinter { get; init; }
    public required DateTime Timestamp { get; init; }
}

/// <summary>
/// Error response.
/// </summary>
public record ErrorResponse
{
    public required string Message { get; init; }
}
