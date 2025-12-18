using System.Text;
using Koinon.Application.DTOs.Import;
using Koinon.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

public class CsvParserServiceTests
{
    private readonly Mock<ILogger<CsvParserService>> _loggerMock;
    private readonly CsvParserService _service;

    public CsvParserServiceTests()
    {
        _loggerMock = new Mock<ILogger<CsvParserService>>();
        _service = new CsvParserService(_loggerMock.Object);
    }

    #region GeneratePreviewAsync Tests

    [Fact]
    public async Task GeneratePreviewAsync_ValidCsv_ReturnsPreview()
    {
        // Arrange
        var csv = "FirstName,LastName,Email\nJohn,Doe,john@example.com\nJane,Smith,jane@example.com";
        var stream = CreateStream(csv);

        // Act
        var result = await _service.GeneratePreviewAsync(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Headers.Count);
        Assert.Contains("FirstName", result.Headers);
        Assert.Contains("LastName", result.Headers);
        Assert.Contains("Email", result.Headers);
        Assert.Equal(2, result.TotalRowCount);
        Assert.Equal(2, result.SampleRows.Count);
        Assert.Equal("Comma", result.DetectedDelimiter);
        Assert.Equal("UTF-8", result.DetectedEncoding);
    }

    [Fact]
    public async Task GeneratePreviewAsync_SemicolonDelimiter_DetectsCorrectly()
    {
        // Arrange
        var csv = "Name;Age;City\nJohn;30;NYC\nJane;25;LA";
        var stream = CreateStream(csv);

        // Act
        var result = await _service.GeneratePreviewAsync(stream);

        // Assert
        Assert.Equal("Semicolon", result.DetectedDelimiter);
        Assert.Equal(3, result.Headers.Count);
        Assert.Equal(2, result.TotalRowCount);
    }

    [Fact]
    public async Task GeneratePreviewAsync_TabDelimiter_DetectsCorrectly()
    {
        // Arrange
        var csv = "Name\tAge\tCity\nJohn\t30\tNYC\nJane\t25\tLA";
        var stream = CreateStream(csv);

        // Act
        var result = await _service.GeneratePreviewAsync(stream);

        // Assert
        Assert.Equal("Tab", result.DetectedDelimiter);
        Assert.Equal(3, result.Headers.Count);
    }

    [Fact]
    public async Task GeneratePreviewAsync_Utf8WithBom_DetectsEncoding()
    {
        // Arrange
        var csv = "Name,Age\nJohn,30";
        var utf8WithBom = new UTF8Encoding(true);
        var stream = CreateStream(csv, utf8WithBom);

        // Act
        var result = await _service.GeneratePreviewAsync(stream);

        // Assert
        Assert.Contains("UTF-8", result.DetectedEncoding);
    }

    [Fact]
    public async Task GeneratePreviewAsync_LargeCsv_ReturnsOnlyFirstFiveRowsInSample()
    {
        // Arrange
        var lines = new List<string> { "Name,Age" };
        for (var i = 1; i <= 20; i++)
        {
            lines.Add($"Person{i},{20 + i}");
        }
        var csv = string.Join("\n", lines);
        var stream = CreateStream(csv);

        // Act
        var result = await _service.GeneratePreviewAsync(stream);

        // Assert
        Assert.Equal(20, result.TotalRowCount);
        Assert.Equal(5, result.SampleRows.Count); // Only first 5 as sample
    }

    [Fact]
    public async Task GeneratePreviewAsync_DuplicateHeaders_ThrowsException()
    {
        // Arrange
        var csv = "Name,Age,Name\nJohn,30,Doe";
        var stream = CreateStream(csv);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GeneratePreviewAsync(stream));
        Assert.Contains("duplicate headers", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GeneratePreviewAsync_FileTooLarge_ThrowsException()
    {
        // Arrange
        var largeData = new string('a', 11 * 1024 * 1024); // 11MB
        var stream = CreateStream(largeData);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GeneratePreviewAsync(stream));
        Assert.Contains("exceeds maximum", ex.Message);
    }

    [Fact]
    public async Task GeneratePreviewAsync_EmptyCsv_ReturnsZeroRows()
    {
        // Arrange
        var csv = "Name,Age";
        var stream = CreateStream(csv);

        // Act
        var result = await _service.GeneratePreviewAsync(stream);

        // Assert
        Assert.Equal(2, result.Headers.Count);
        Assert.Empty(result.SampleRows);
        Assert.Equal(0, result.TotalRowCount);
    }

    #endregion

    #region ValidateFileAsync Tests

    [Fact]
    public async Task ValidateFileAsync_ValidFile_ReturnsNoErrors()
    {
        // Arrange
        var csv = "FirstName,LastName,Email\nJohn,Doe,john@example.com";
        var stream = CreateStream(csv);
        var requiredColumns = new List<string> { "FirstName", "LastName" };

        // Act
        var errors = await _service.ValidateFileAsync(stream, requiredColumns);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateFileAsync_MissingRequiredColumns_ReturnsError()
    {
        // Arrange
        var csv = "FirstName,Email\nJohn,john@example.com";
        var stream = CreateStream(csv);
        var requiredColumns = new List<string> { "FirstName", "LastName", "Age" };

        // Act
        var errors = await _service.ValidateFileAsync(stream, requiredColumns);

        // Assert
        Assert.Single(errors);
        Assert.Equal(0, errors[0].RowNumber);
        Assert.Contains("Required columns missing", errors[0].ErrorMessage);
        Assert.Contains("LastName", errors[0].ColumnName);
        Assert.Contains("Age", errors[0].ColumnName);
    }

    [Fact]
    public async Task ValidateFileAsync_EmptyRequiredField_ReturnsError()
    {
        // Arrange
        var csv = "FirstName,LastName,Email\nJohn,,john@example.com\n,Doe,jane@example.com";
        var stream = CreateStream(csv);
        var requiredColumns = new List<string> { "FirstName", "LastName" };

        // Act
        var errors = await _service.ValidateFileAsync(stream, requiredColumns);

        // Assert
        Assert.Equal(2, errors.Count);
        Assert.All(errors, e => Assert.Contains("Required field is empty", e.ErrorMessage));
    }

    [Fact]
    public async Task ValidateFileAsync_InvalidEmail_ReturnsError()
    {
        // Arrange
        var csv = "Name,Email\nJohn,invalid-email\nJane,jane@example.com";
        var stream = CreateStream(csv);
        var requiredColumns = new List<string> { "Name" };

        // Act
        var errors = await _service.ValidateFileAsync(stream, requiredColumns);

        // Assert
        Assert.Single(errors);
        Assert.Equal(2, errors[0].RowNumber);
        Assert.Equal("Email", errors[0].ColumnName);
        Assert.Contains("Invalid email format", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task ValidateFileAsync_InvalidPhone_ReturnsError()
    {
        // Arrange
        var csv = "Name,Phone\nJohn,123\nJane,555-123-4567";
        var stream = CreateStream(csv);
        var requiredColumns = new List<string> { "Name" };

        // Act
        var errors = await _service.ValidateFileAsync(stream, requiredColumns);

        // Assert
        Assert.Single(errors);
        Assert.Equal(2, errors[0].RowNumber);
        Assert.Contains("Invalid phone number", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task ValidateFileAsync_ValidPhone_WithFormatting_PassesValidation()
    {
        // Arrange
        var csv = "Name,Phone\nJohn,(555) 123-4567\nJane,15551234567";
        var stream = CreateStream(csv);
        var requiredColumns = new List<string> { "Name" };

        // Act
        var errors = await _service.ValidateFileAsync(stream, requiredColumns);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateFileAsync_InvalidDate_ReturnsError()
    {
        // Arrange
        var csv = "Name,BirthDate\nJohn,13/32/2020\nJane,2020-01-15";
        var stream = CreateStream(csv);
        var requiredColumns = new List<string> { "Name" };

        // Act
        var errors = await _service.ValidateFileAsync(stream, requiredColumns);

        // Assert
        Assert.Single(errors);
        Assert.Equal(2, errors[0].RowNumber);
        Assert.Contains("Invalid date format", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task ValidateFileAsync_ValidDateFormats_PassValidation()
    {
        // Arrange
        var csv = "Name,BirthDate\nJohn,2020-01-15\nJane,01/15/2020\nBob,15/01/2020";
        var stream = CreateStream(csv);
        var requiredColumns = new List<string> { "Name" };

        // Act
        var errors = await _service.ValidateFileAsync(stream, requiredColumns);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateFileAsync_CaseInsensitiveColumns_MatchesCorrectly()
    {
        // Arrange
        var csv = "firstname,LASTNAME,Email\nJohn,Doe,john@example.com";
        var stream = CreateStream(csv);
        var requiredColumns = new List<string> { "FirstName", "LastName" };

        // Act
        var errors = await _service.ValidateFileAsync(stream, requiredColumns);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateFileAsync_MultipleErrors_CollectsAll()
    {
        // Arrange
        var csv = "Name,Email,Phone\n,invalid-email,123\nJane,jane@example.com,5551234567";
        var stream = CreateStream(csv);
        var requiredColumns = new List<string> { "Name" };

        // Act
        var errors = await _service.ValidateFileAsync(stream, requiredColumns);

        // Assert
        Assert.Equal(3, errors.Count); // Empty name, invalid email, invalid phone
    }

    #endregion

    #region StreamRowsAsync Tests

    [Fact]
    public async Task StreamRowsAsync_ValidCsv_StreamsAllRows()
    {
        // Arrange
        var csv = "Name,Age\nJohn,30\nJane,25\nBob,35";
        var stream = CreateStream(csv);

        // Act
        var rows = new List<Dictionary<string, string>>();
        await foreach (var row in _service.StreamRowsAsync(stream))
        {
            rows.Add(row);
        }

        // Assert
        Assert.Equal(3, rows.Count);
        Assert.Equal("John", rows[0]["Name"]);
        Assert.Equal("30", rows[0]["Age"]);
        Assert.Equal("Jane", rows[1]["Name"]);
        Assert.Equal("25", rows[1]["Age"]);
    }

    [Fact]
    public async Task StreamRowsAsync_EmptyCsv_StreamsNoRows()
    {
        // Arrange
        var csv = "Name,Age";
        var stream = CreateStream(csv);

        // Act
        var rows = new List<Dictionary<string, string>>();
        await foreach (var row in _service.StreamRowsAsync(stream))
        {
            rows.Add(row);
        }

        // Assert
        Assert.Empty(rows);
    }

    [Fact]
    public async Task StreamRowsAsync_LargeCsv_StreamsEfficiently()
    {
        // Arrange
        var lines = new List<string> { "Name,Age" };
        for (var i = 1; i <= 1000; i++)
        {
            lines.Add($"Person{i},{20 + i}");
        }
        var csv = string.Join("\n", lines);
        var stream = CreateStream(csv);

        // Act
        var count = 0;
        await foreach (var row in _service.StreamRowsAsync(stream))
        {
            count++;
            Assert.Contains("Name", row.Keys);
            Assert.Contains("Age", row.Keys);
        }

        // Assert
        Assert.Equal(1000, count);
    }

    [Fact]
    public async Task StreamRowsAsync_WithCancellation_StopsStreaming()
    {
        // Arrange
        var lines = new List<string> { "Name,Age" };
        for (var i = 1; i <= 100; i++)
        {
            lines.Add($"Person{i},{20 + i}");
        }
        var csv = string.Join("\n", lines);
        var stream = CreateStream(csv);
        var cts = new CancellationTokenSource();

        // Act
        var count = 0;
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var row in _service.StreamRowsAsync(stream, cts.Token))
            {
                count++;
                if (count == 10)
                {
                    cts.Cancel();
                }
            }
        });

        // Assert
        Assert.Equal(10, count);
    }

    #endregion

    #region Helper Methods

    private static MemoryStream CreateStream(string content, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var bytes = encoding.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return stream;
    }

    #endregion
}
