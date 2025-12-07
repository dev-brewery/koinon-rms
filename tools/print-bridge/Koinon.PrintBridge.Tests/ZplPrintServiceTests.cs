using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.PrintBridge.Tests;

public class ZplPrintServiceTests
{
    private readonly Mock<ILogger<ZplPrintService>> _mockLogger;
    private readonly ZplPrintService _service;

    public ZplPrintServiceTests()
    {
        _mockLogger = new Mock<ILogger<ZplPrintService>>();
        _service = new ZplPrintService(_mockLogger.Object);
    }

    [Fact]
    public void ValidateZpl_WithValidZpl_ReturnsTrue()
    {
        // Arrange
        var validZpl = @"^XA
^FO50,50^A0N,50,50^FDTest Label^FS
^XZ";

        // Act
        var result = _service.ValidateZpl(validZpl);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateZpl_WithInvalidZpl_ReturnsFalse()
    {
        // Arrange
        var invalidZpl = "This is not ZPL";

        // Act
        var result = _service.ValidateZpl(invalidZpl);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateZpl_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        var emptyZpl = "";

        // Act
        var result = _service.ValidateZpl(emptyZpl);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateZpl_WithWhitespace_ReturnsFalse()
    {
        // Arrange
        var whitespaceZpl = "   ";

        // Act
        var result = _service.ValidateZpl(whitespaceZpl);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateZpl_WithMissingStartCommand_ReturnsFalse()
    {
        // Arrange
        var invalidZpl = @"^FO50,50^A0N,50,50^FDTest Label^FS
^XZ";

        // Act
        var result = _service.ValidateZpl(invalidZpl);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateZpl_WithMissingEndCommand_ReturnsFalse()
    {
        // Arrange
        var invalidZpl = @"^XA
^FO50,50^A0N,50,50^FDTest Label^FS";

        // Act
        var result = _service.ValidateZpl(invalidZpl);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateZpl_WithExtraWhitespace_ReturnsTrue()
    {
        // Arrange
        var validZpl = @"

        ^XA
^FO50,50^A0N,50,50^FDTest Label^FS
^XZ

        ";

        // Act
        var result = _service.ValidateZpl(validZpl);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateZpl_WithCaseInsensitiveCommands_ReturnsTrue()
    {
        // Arrange
        var validZpl = @"^xa
^FO50,50^A0N,50,50^FDTest Label^FS
^xz";

        // Act
        var result = _service.ValidateZpl(validZpl);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PrintZplAsync_WithEmptyPrinterName_ThrowsArgumentException()
    {
        // Arrange
        var validZpl = "^XA^FO50,50^A0N,50,50^FDTest^FS^XZ";

        // Act
        Func<Task> act = async () => await _service.PrintZplAsync("", validZpl);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Printer name cannot be empty*");
    }

    [Fact]
    public async Task PrintZplAsync_WithEmptyZpl_ThrowsArgumentException()
    {
        // Arrange
        var printerName = "TestPrinter";

        // Act
        Func<Task> act = async () => await _service.PrintZplAsync(printerName, "");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*ZPL content cannot be empty*");
    }
}
