using Microsoft.AspNetCore.Mvc;

namespace Koinon.PrintBridge;

/// <summary>
/// API controller for diagnostics, health checks, and printing operations.
/// </summary>
[ApiController]
[Route("api")]
public class DiagnosticsController : ControllerBase
{
    private readonly PrinterDiscoveryService _printerDiscovery;
    private readonly ZplPrintService _printService;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(
        PrinterDiscoveryService printerDiscovery,
        ZplPrintService printService,
        ILogger<DiagnosticsController> logger)
    {
        _printerDiscovery = printerDiscovery;
        _printService = printService;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint.
    /// </summary>
    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            version = "1.0.0",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Gets all available printers on the system.
    /// </summary>
    [HttpGet("printers")]
    public async Task<IActionResult> GetPrinters()
    {
        try
        {
            var printers = await _printerDiscovery.GetAvailablePrintersAsync();

            return Ok(new
            {
                printers = printers.Select(p => new
                {
                    name = p.Name,
                    status = p.Status,
                    isDefault = p.IsDefault,
                    isZebraPrinter = p.IsZebraPrinter,
                    driverName = p.DriverName,
                    portName = p.PortName
                }),
                count = printers.Count,
                zebraCount = printers.Count(p => p.IsZebraPrinter)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve printers");
            return StatusCode(500, new
            {
                error = "Failed to retrieve printers",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Refreshes the printer cache.
    /// </summary>
    [HttpPost("printers/refresh")]
    public async Task<IActionResult> RefreshPrinters()
    {
        try
        {
            await _printerDiscovery.RefreshPrintersAsync();
            var printers = await _printerDiscovery.GetAvailablePrintersAsync();

            return Ok(new
            {
                message = "Printer cache refreshed",
                count = printers.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh printers");
            return StatusCode(500, new
            {
                error = "Failed to refresh printers",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Prints a test label to verify printer functionality.
    /// </summary>
    [HttpPost("print/test")]
    public async Task<IActionResult> PrintTestLabel([FromBody] TestPrintRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PrinterName))
        {
            return BadRequest(new { error = "Printer name is required" });
        }

        try
        {
            // Generate test label
            var testZpl = @"^XA
^FO50,50^A0N,50,50^FDTest Label^FS
^FO50,120^A0N,30,30^FD" + DateTime.Now.ToString("g") + @"^FS
^FO50,160^A0N,25,25^FDKoinon Print Bridge^FS
^XZ";

            await _printService.PrintZplAsync(request.PrinterName, testZpl);

            _logger.LogInformation("Test label printed successfully to {PrinterName}", request.PrinterName);

            return Ok(new
            {
                success = true,
                message = $"Test label sent to {request.PrinterName}",
                printerName = request.PrinterName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to print test label to {PrinterName}", request.PrinterName);
            return StatusCode(500, new
            {
                error = "Failed to print test label",
                message = ex.Message,
                printerName = request.PrinterName
            });
        }
    }

    /// <summary>
    /// Prints ZPL content to a specified printer.
    /// </summary>
    [HttpPost("print")]
    public async Task<IActionResult> Print([FromBody] PrintRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PrinterName))
        {
            return BadRequest(new { error = "Printer name is required" });
        }

        if (string.IsNullOrWhiteSpace(request.ZplContent))
        {
            return BadRequest(new { error = "ZPL content is required" });
        }

        // Validate ZPL content with comprehensive security checks
        var validationResult = _printService.ValidateZplWithSecurity(request.ZplContent);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                error = "Invalid or potentially dangerous ZPL content",
                message = validationResult.ErrorMessage
            });
        }

        try
        {
            await _printService.PrintZplAsync(request.PrinterName, request.ZplContent);

            _logger.LogInformation("Label printed successfully to {PrinterName}", request.PrinterName);

            return Ok(new
            {
                success = true,
                message = $"Label sent to {request.PrinterName}",
                printerName = request.PrinterName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to print label to {PrinterName}", request.PrinterName);
            return StatusCode(500, new
            {
                error = "Failed to print label",
                message = ex.Message,
                printerName = request.PrinterName
            });
        }
    }

    // Maximum batch size to prevent DoS attacks
    private const int MaxBatchSize = 50;

    /// <summary>
    /// Prints multiple ZPL labels in batch.
    /// </summary>
    [HttpPost("print/batch")]
    public async Task<IActionResult> PrintBatch([FromBody] BatchPrintRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PrinterName))
        {
            return BadRequest(new { error = "Printer name is required" });
        }

        if (request.ZplContents == null || request.ZplContents.Count == 0)
        {
            return BadRequest(new { error = "At least one ZPL content is required" });
        }

        // Rate limiting: max 50 labels per batch to prevent DoS
        if (request.ZplContents.Count > MaxBatchSize)
        {
            return BadRequest(new
            {
                error = "Batch size exceeded",
                message = $"Maximum {MaxBatchSize} labels per batch. Received {request.ZplContents.Count}."
            });
        }

        // Validate all ZPL content with security checks
        var invalidLabels = new List<string>();
        for (int i = 0; i < request.ZplContents.Count; i++)
        {
            var validationResult = _printService.ValidateZplWithSecurity(request.ZplContents[i]);
            if (!validationResult.IsValid)
            {
                invalidLabels.Add($"Label {i}: {validationResult.ErrorMessage}");
            }
        }

        if (invalidLabels.Count > 0)
        {
            return BadRequest(new
            {
                error = "Invalid or potentially dangerous ZPL content",
                message = string.Join("; ", invalidLabels)
            });
        }

        try
        {
            await _printService.PrintBatchZplAsync(request.PrinterName, request.ZplContents);

            _logger.LogInformation("Batch of {Count} labels printed successfully to {PrinterName}",
                request.ZplContents.Count, request.PrinterName);

            return Ok(new
            {
                success = true,
                message = $"{request.ZplContents.Count} labels sent to {request.PrinterName}",
                printerName = request.PrinterName,
                labelCount = request.ZplContents.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to print batch of labels to {PrinterName}", request.PrinterName);
            return StatusCode(500, new
            {
                error = "Failed to print batch",
                message = ex.Message,
                printerName = request.PrinterName
            });
        }
    }
}

/// <summary>
/// Request model for test print.
/// </summary>
public class TestPrintRequest
{
    public required string PrinterName { get; init; }
}

/// <summary>
/// Request model for printing a single label.
/// </summary>
public class PrintRequest
{
    public required string PrinterName { get; init; }
    public required string ZplContent { get; init; }
}

/// <summary>
/// Request model for batch printing.
/// </summary>
public class BatchPrintRequest
{
    public required string PrinterName { get; init; }
    public required List<string> ZplContents { get; init; }
}
