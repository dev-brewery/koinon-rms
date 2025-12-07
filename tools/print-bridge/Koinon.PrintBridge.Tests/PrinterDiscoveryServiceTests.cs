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
}
