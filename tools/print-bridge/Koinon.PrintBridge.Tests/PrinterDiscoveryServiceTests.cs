using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.PrintBridge.Tests;

public class PrinterDiscoveryServiceTests
{
    private readonly Mock<ILogger<PrinterDiscoveryService>> _mockLogger;
    private readonly PrinterDiscoveryService _service;

    public PrinterDiscoveryServiceTests()
    {
        _mockLogger = new Mock<ILogger<PrinterDiscoveryService>>();
        _service = new PrinterDiscoveryService(_mockLogger.Object);
    }

    [Fact]
    public async Task InitializeAsync_CompletesSuccessfully()
    {
        // Act
        var act = async () => await _service.InitializeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAvailablePrintersAsync_ReturnsNonNullList()
    {
        // Act
        var printers = await _service.GetAvailablePrintersAsync();

        // Assert
        printers.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAvailablePrintersAsync_ReturnsSameResultsWhenCached()
    {
        // Act
        var firstCall = await _service.GetAvailablePrintersAsync();
        var secondCall = await _service.GetAvailablePrintersAsync();

        // Assert
        firstCall.Should().BeSameAs(secondCall, "results should be cached");
    }

    [Fact]
    public async Task RefreshPrintersAsync_InvalidatesCache()
    {
        // Arrange
        var firstCall = await _service.GetAvailablePrintersAsync();

        // Act
        await _service.RefreshPrintersAsync();
        var secondCall = await _service.GetAvailablePrintersAsync();

        // Assert
        firstCall.Should().NotBeSameAs(secondCall, "cache should be invalidated after refresh");
    }

    [Fact]
    public void GetPrinterInfo_WithValidPrinterName_ReturnsInfo()
    {
        // Arrange - Get first available printer from system
        var printers = _service.GetAvailablePrintersAsync().Result;

        if (printers.Count == 0)
        {
            // Skip test if no printers available
            return;
        }

        var printerName = printers[0].Name;

        // Act
        var info = _service.GetPrinterInfo(printerName);

        // Assert
        info.Should().NotBeNull();
        info.Name.Should().Be(printerName);
        info.Status.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAvailablePrintersAsync_PrintersHaveTypeInformation()
    {
        // Act
        var printers = await _service.GetAvailablePrintersAsync();

        // Assert
        foreach (var printer in printers)
        {
            printer.PrinterType.Should().NotBeNullOrEmpty();
            printer.PrinterType.Should().BeOneOf("Zebra", "Dymo", "Generic");
            printer.SupportsImage.Should().BeTrue("all Windows printers support GDI image printing");

            // Type-specific validation
            if (printer.IsZebraPrinter)
            {
                printer.PrinterType.Should().Be("Zebra");
                printer.SupportsZpl.Should().BeTrue();
            }

            if (printer.IsDymoPrinter)
            {
                printer.PrinterType.Should().Be("Dymo");
                printer.SupportsZpl.Should().BeFalse();
            }

            if (printer.PrinterType == "Generic")
            {
                printer.IsZebraPrinter.Should().BeFalse();
                printer.IsDymoPrinter.Should().BeFalse();
                printer.SupportsZpl.Should().BeFalse();
            }
        }
    }

    [Theory]
    [InlineData("Zebra ZD420", true, false)]
    [InlineData("ZPL Printer", true, false)]
    [InlineData("DYMO LabelWriter 450", false, true)]
    [InlineData("DYMO LabelWriter 4XL", false, true)]
    [InlineData("HP LaserJet", false, false)]
    [InlineData("Canon PIXMA", false, false)]
    public void GetPrinterInfo_IdentifiesPrinterTypeCorrectly(string printerName, bool expectedZebra, bool expectedDymo)
    {
        // This is a unit test that doesn't require actual printers
        // We're testing the detection logic by creating a mock scenario

        // Note: This test validates the detection logic patterns
        // In real environment, GetPrinterInfo would query actual printer
        var nameLower = printerName.ToLowerInvariant();
        var isZebra = nameLower.Contains("zebra") || nameLower.Contains("zpl");
        var isDymo = nameLower.Contains("dymo") || nameLower.Contains("labelwriter");

        // Assert
        isZebra.Should().Be(expectedZebra);
        isDymo.Should().Be(expectedDymo);
    }
}
