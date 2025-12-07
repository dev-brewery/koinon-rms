using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;

namespace Koinon.PrintBridge;

/// <summary>
/// Service for printing images and graphics to Windows printers using GDI.
/// Supports all Windows printers including Dymo, generic desktop printers, etc.
/// </summary>
public class WindowsPrintService
{
    private readonly ILogger<WindowsPrintService> _logger;

    // Common label sizes in inches (width x height)
    private static readonly Dictionary<string, (float width, float height)> LabelSizes = new()
    {
        { "default", (2.25f, 1.25f) },      // Standard address label
        { "small", (1.125f, 3.5f) },        // File folder label
        { "medium", (2.25f, 1.25f) },       // Standard address label
        { "large", (4.0f, 6.0f) },          // Shipping label
        { "badge", (3.0f, 4.0f) }           // Name badge
    };

    public WindowsPrintService(ILogger<WindowsPrintService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Prints a label from base64-encoded image data.
    /// </summary>
    /// <param name="printerName">Name of the printer</param>
    /// <param name="base64Image">Base64-encoded image data (PNG, JPEG, etc.)</param>
    /// <param name="labelSize">Label size preset (default, small, medium, large, badge)</param>
    public async Task PrintLabelAsync(string printerName, string base64Image, string labelSize = "default")
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            throw new ArgumentException("Printer name cannot be empty", nameof(printerName));
        }

        if (string.IsNullOrWhiteSpace(base64Image))
        {
            throw new ArgumentException("Image data cannot be empty", nameof(base64Image));
        }

        _logger.LogInformation("Printing image label to printer: {PrinterName} (size: {LabelSize})",
            printerName, labelSize);

        await Task.Run(() =>
        {
            try
            {
                // Convert base64 to image
                var imageBytes = Convert.FromBase64String(base64Image);
                using var ms = new MemoryStream(imageBytes);
                using var sourceImage = Image.FromStream(ms);

                // Get label dimensions
                if (!LabelSizes.TryGetValue(labelSize.ToLowerInvariant(), out var dimensions))
                {
                    _logger.LogWarning("Unknown label size '{LabelSize}', using default", labelSize);
                    dimensions = LabelSizes["default"];
                }

                // Clone the image so it can be used in the PrintPage event handler
                // which executes asynchronously after this using block exits
                using var imageToPrint = (Image)sourceImage.Clone();

                // Print the image
                PrintImage(printerName, imageToPrint, dimensions.width, dimensions.height);

                _logger.LogInformation("Successfully printed image label to printer: {PrinterName}", printerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to print image label to printer: {PrinterName}", printerName);
                throw;
            }
        });
    }

    /// <summary>
    /// Prints a simple text label using GDI rendering.
    /// </summary>
    /// <param name="printerName">Name of the printer</param>
    /// <param name="labelContent">Text content to print</param>
    /// <param name="labelSize">Label size preset</param>
    public async Task PrintTextLabelAsync(string printerName, string labelContent, string labelSize = "default")
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            throw new ArgumentException("Printer name cannot be empty", nameof(printerName));
        }

        if (string.IsNullOrWhiteSpace(labelContent))
        {
            throw new ArgumentException("Label content cannot be empty", nameof(labelContent));
        }

        _logger.LogInformation("Printing text label to printer: {PrinterName}", printerName);

        await Task.Run(() =>
        {
            try
            {
                // Get label dimensions
                if (!LabelSizes.TryGetValue(labelSize.ToLowerInvariant(), out var dimensions))
                {
                    _logger.LogWarning("Unknown label size '{LabelSize}', using default", labelSize);
                    dimensions = LabelSizes["default"];
                }

                // Create image with text
                using var image = CreateTextImage(labelContent, dimensions.width, dimensions.height);

                // Print the image
                PrintImage(printerName, image, dimensions.width, dimensions.height);

                _logger.LogInformation("Successfully printed text label to printer: {PrinterName}", printerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to print text label to printer: {PrinterName}", printerName);
                throw;
            }
        });
    }

    /// <summary>
    /// Validates base64-encoded image data.
    /// </summary>
    public (bool IsValid, string? ErrorMessage) ValidateImageData(string base64Image)
    {
        if (string.IsNullOrWhiteSpace(base64Image))
        {
            return (false, "Image data cannot be empty");
        }

        try
        {
            // Check if valid base64
            var imageBytes = Convert.FromBase64String(base64Image);

            // Check size (max 5MB)
            const int maxSizeBytes = 5 * 1024 * 1024;
            if (imageBytes.Length > maxSizeBytes)
            {
                return (false, $"Image size exceeds maximum of {maxSizeBytes / 1024 / 1024}MB");
            }

            // Try to load as image
            using var ms = new MemoryStream(imageBytes);
            using var image = Image.FromStream(ms);

            // Basic validation passed
            return (true, null);
        }
        catch (FormatException)
        {
            return (false, "Invalid base64 encoding");
        }
        catch (ArgumentException)
        {
            return (false, "Invalid image format");
        }
        catch (Exception ex)
        {
            return (false, $"Image validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Prints an image to a printer using GDI.
    /// </summary>
    private void PrintImage(string printerName, Image image, float widthInches, float heightInches)
    {
        var printDocument = new PrintDocument
        {
            PrinterSettings = { PrinterName = printerName }
        };

        // Verify printer exists
        if (!printDocument.PrinterSettings.IsValid)
        {
            throw new InvalidOperationException($"Printer '{printerName}' not found or not available");
        }

        // Set up custom paper size for label
        var paperSize = new PaperSize("Label",
            (int)(widthInches * 100),    // Convert to hundredths of inch
            (int)(heightInches * 100));
        printDocument.DefaultPageSettings.PaperSize = paperSize;

        // Disable margins for full-bleed printing
        printDocument.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

        // Handle the PrintPage event
        printDocument.PrintPage += (sender, e) =>
        {
            if (e.Graphics == null)
            {
                return;
            }

            // Calculate bounds to fit label size
            var bounds = new Rectangle(
                0, 0,
                (int)(widthInches * e.Graphics.DpiX),
                (int)(heightInches * e.Graphics.DpiY)
            );

            // Draw the image
            e.Graphics.DrawImage(image, bounds);

            // No more pages
            e.HasMorePages = false;
        };

        // Print the document
        printDocument.Print();
    }

    /// <summary>
    /// Creates an image with text content for simple label printing.
    /// </summary>
    private static Image CreateTextImage(string text, float widthInches, float heightInches)
    {
        // Use 300 DPI for good quality
        const int dpi = 300;
        var width = (int)(widthInches * dpi);
        var height = (int)(heightInches * dpi);

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        bitmap.SetResolution(dpi, dpi);

        using (var g = Graphics.FromImage(bitmap))
        {
            // High quality rendering
            g.Clear(Color.White);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Calculate font size based on label height
            var fontSize = (int)(heightInches * 30); // Rough estimate
            using var font = new Font("Arial", fontSize, FontStyle.Regular, GraphicsUnit.Point);

            // Measure text to ensure it fits
            var textSize = g.MeasureString(text, font);
            var scale = Math.Min(width / textSize.Width, height / textSize.Height);

            if (scale < 1.0f)
            {
                // Text too large, scale down font
                fontSize = (int)(fontSize * scale * 0.9f);
            }

            using var finalFont = new Font("Arial", fontSize, FontStyle.Regular, GraphicsUnit.Point);

            // Draw text centered
            var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            g.DrawString(text, finalFont, Brushes.Black,
                new RectangleF(0, 0, width, height), format);
        }

        return bitmap;
    }

    /// <summary>
    /// Gets available label size presets.
    /// </summary>
    public static Dictionary<string, (float width, float height)> GetAvailableLabelSizes()
    {
        return new Dictionary<string, (float width, float height)>(LabelSizes);
    }
}
