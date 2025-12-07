using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.PrintBridge.Tests;

public class WindowsPrintServiceTests
{
    private readonly Mock<ILogger<WindowsPrintService>> _mockLogger;
    private readonly WindowsPrintService _service;

    public WindowsPrintServiceTests()
    {
        _mockLogger = new Mock<ILogger<WindowsPrintService>>();
        _service = new WindowsPrintService(_mockLogger.Object);
    }

    [Fact]
    public void ValidateImageData_WithNullData_ReturnsFalse()
    {
        // Act
        var result = _service.ValidateImageData(null!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateImageData_WithEmptyData_ReturnsFalse()
    {
        // Act
        var result = _service.ValidateImageData("");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateImageData_WithInvalidBase64_ReturnsFalse()
    {
        // Arrange
        var invalidBase64 = "not-valid-base64!!!";

        // Act
        var result = _service.ValidateImageData(invalidBase64);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("base64");
    }

    [Fact]
    public void ValidateImageData_WithValidPngImage_ReturnsTrue()
    {
        // Arrange - Create a minimal 1x1 PNG image in base64
        var validPng = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        // Act
        var result = _service.ValidateImageData(validPng);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateImageData_WithOversizedImage_ReturnsFalse()
    {
        // Arrange - Create a base64 string larger than 5MB
        var oversizedData = new string('A', 7 * 1024 * 1024); // 7MB of 'A's

        // Act
        var result = _service.ValidateImageData(oversizedData);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("maximum size");
    }

    [Fact]
    public void GetAvailableLabelSizes_ReturnsMultipleSizes()
    {
        // Act
        var sizes = WindowsPrintService.GetAvailableLabelSizes();

        // Assert
        sizes.Should().NotBeEmpty();
        sizes.Should().ContainKey("default");
        sizes.Should().ContainKey("small");
        sizes.Should().ContainKey("medium");
        sizes.Should().ContainKey("large");
        sizes.Should().ContainKey("badge");
    }

    [Fact]
    public void GetAvailableLabelSizes_AllSizesHaveValidDimensions()
    {
        // Act
        var sizes = WindowsPrintService.GetAvailableLabelSizes();

        // Assert
        foreach (var size in sizes)
        {
            size.Value.width.Should().BeGreaterThan(0);
            size.Value.height.Should().BeGreaterThan(0);
            size.Value.width.Should().BeLessThan(20); // Reasonable max width
            size.Value.height.Should().BeLessThan(20); // Reasonable max height
        }
    }

    [Fact]
    public async Task PrintLabelAsync_WithNullPrinterName_ThrowsArgumentException()
    {
        // Arrange
        var validImage = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        // Act
        var act = async () => await _service.PrintLabelAsync(null!, validImage);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Printer name*");
    }

    [Fact]
    public async Task PrintLabelAsync_WithNullImage_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _service.PrintLabelAsync("TestPrinter", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Image data*");
    }

    [Fact]
    public async Task PrintTextLabelAsync_WithNullPrinterName_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _service.PrintTextLabelAsync(null!, "Test");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Printer name*");
    }

    [Fact]
    public async Task PrintTextLabelAsync_WithNullContent_ThrowsArgumentException()
    {
        // Act
        var act = async () => await _service.PrintTextLabelAsync("TestPrinter", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Label content*");
    }
}
