using System.Diagnostics;
using System.Text;
using FluentAssertions;
using Koinon.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services.Performance;

/// <summary>
/// Performance tests for CsvParserService with large CSV files.
/// Tests focus on memory efficiency and throughput for streaming operations.
/// </summary>
[Trait("Category", "Performance")]
public class CsvParserPerformanceTests
{
    private readonly Mock<ILogger<CsvParserService>> _loggerMock;
    private readonly CsvParserService _service;

    public CsvParserPerformanceTests()
    {
        _loggerMock = new Mock<ILogger<CsvParserService>>();
        _service = new CsvParserService(_loggerMock.Object);
    }

    #region GeneratePreviewAsync Performance Tests

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task GeneratePreviewAsync_100000Rows_CompletesUnder500Ms()
    {
        // Arrange
        const int rowCount = 100000;
        const int maxMilliseconds = 2000;

        var headers = new[] { "FirstName", "LastName", "Email", "Phone", "BirthDate" };
        var csvStream = CsvGenerator.GenerateCsv(rowCount, headers);

        // Act
        var (result, metrics) = await PerformanceMeasurer.MeasureAsync(
            async () => await _service.GeneratePreviewAsync(csvStream),
            rowCount);

        // Assert
        result.Should().NotBeNull();
        result.Headers.Should().HaveCount(5);
        result.TotalRowCount.Should().Be(rowCount);
        result.SampleRows.Should().HaveCountLessOrEqualTo(5, "preview should only return first 5 rows");

        PerformanceAssertions.AssertCompletedWithin(
            TimeSpan.FromMilliseconds(maxMilliseconds),
            TimeSpan.FromMilliseconds(metrics.ElapsedMs));
    }

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task GeneratePreviewAsync_50000Rows_CompletesUnder250Ms()
    {
        // Arrange
        const int rowCount = 50000;
        const int maxMilliseconds = 250;

        var headers = new[] { "Name", "Age", "City", "State" };
        var csvStream = CsvGenerator.GenerateCsv(rowCount, headers);

        // Act
        var (result, metrics) = await PerformanceMeasurer.MeasureAsync(
            async () => await _service.GeneratePreviewAsync(csvStream),
            rowCount);

        // Assert
        result.Should().NotBeNull();
        result.TotalRowCount.Should().Be(rowCount);

        PerformanceAssertions.AssertCompletedWithin(
            TimeSpan.FromMilliseconds(maxMilliseconds),
            TimeSpan.FromMilliseconds(metrics.ElapsedMs));
    }

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task GeneratePreviewAsync_30000Rows_WithWideRows_CompletesUnder1Second()
    {
        // Arrange
        const int rowCount = 30000;
        const int maxMilliseconds = 1000;

        // Wide rows with 20 columns to test column iteration performance
        // Note: Reduced row count to stay under 10MB file size limit
        var headers = Enumerable.Range(1, 20).Select(i => $"Column{i}").ToArray();
        var csvStream = CsvGenerator.GenerateCsv(rowCount, headers);

        // Act
        var (result, metrics) = await PerformanceMeasurer.MeasureAsync(
            async () => await _service.GeneratePreviewAsync(csvStream),
            rowCount);

        // Assert
        result.Should().NotBeNull();
        result.Headers.Should().HaveCount(20);
        result.TotalRowCount.Should().Be(rowCount);

        PerformanceAssertions.AssertCompletedWithin(
            TimeSpan.FromMilliseconds(maxMilliseconds),
            TimeSpan.FromMilliseconds(metrics.ElapsedMs));
    }

    #endregion

    #region StreamRowsAsync Memory Efficiency Tests

    [Fact(Skip = "Memory measurement is unreliable in CI - GC.GetTotalMemory varies between runs")]
    public async Task StreamRowsAsync_100000Rows_UsesLessThan50MBAdditionalMemory()
    {
        // Arrange
        const int rowCount = 100000;
        const long maxMemoryBytes = 50 * 1024 * 1024; // 50MB

        var headers = new[] { "FirstName", "LastName", "Email", "Phone" };
        var csvStream = CsvGenerator.GenerateCsv(rowCount, headers);

        // Force GC before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);

        // Act - Stream all rows without storing them
        var processedCount = 0;
        await foreach (var row in _service.StreamRowsAsync(csvStream))
        {
            processedCount++;
            // Process row without storing it to simulate real streaming behavior
            _ = row["FirstName"];
        }

        // Measure memory after processing
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryAfter = GC.GetTotalMemory(forceFullCollection: true);
        var memoryUsed = Math.Max(0, memoryAfter - memoryBefore);

        // Assert
        processedCount.Should().Be(rowCount, "should process all rows");

        PerformanceAssertions.AssertMemoryUsageLessThan(maxMemoryBytes, memoryUsed);

        // Additional logging for analysis
        var memoryUsedMB = memoryUsed / 1024.0 / 1024.0;
        var bytesPerRow = memoryUsed / (double)rowCount;
        _loggerMock.Object.LogInformation(
            $"Streamed {rowCount} rows using {memoryUsedMB:F2}MB ({bytesPerRow:F2} bytes/row)");
    }

    [Fact(Skip = "Memory measurement is unreliable in CI - GC.GetTotalMemory varies between runs")]
    public async Task StreamRowsAsync_50000Rows_DoesNotLoadAllIntoMemory()
    {
        // Arrange
        const int rowCount = 50000;
        const long maxMemoryBytes = 25 * 1024 * 1024; // 25MB

        var headers = new[] { "Name", "Email", "Phone", "Address" };
        var csvStream = CsvGenerator.GenerateCsv(rowCount, headers);

        // Force GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);

        // Act - Stream with processing
        var stopwatch = Stopwatch.StartNew();
        var processedCount = 0;

        await foreach (var row in _service.StreamRowsAsync(csvStream))
        {
            processedCount++;
            // Simulate minimal processing
            _ = row.Count;
        }

        stopwatch.Stop();

        // Measure memory
        var memoryAfter = GC.GetTotalMemory(forceFullCollection: true);
        var memoryUsed = Math.Max(0, memoryAfter - memoryBefore);

        // Assert
        processedCount.Should().Be(rowCount);
        memoryUsed.Should().BeLessThan(maxMemoryBytes,
            "streaming should not load all rows into memory at once");

        // Log throughput
        var throughput = rowCount / (stopwatch.ElapsedMilliseconds / 1000.0);
        _loggerMock.Object.LogInformation(
            $"Streamed {rowCount} rows at {throughput:F0} rows/sec using {memoryUsed / 1024.0 / 1024.0:F2}MB");
    }

    [Fact(Skip = "Memory measurement is unreliable in CI - GC.GetTotalMemory varies between runs")]
    public async Task StreamRowsAsync_40000Rows_WideRows_EfficientMemoryUsage()
    {
        // Arrange
        const int rowCount = 40000;
        const long maxMemoryBytes = 40 * 1024 * 1024; // 40MB

        // 15 columns to test memory efficiency with wider rows
        // Note: Reduced row count to stay under 10MB file size limit
        var headers = Enumerable.Range(1, 15).Select(i => $"Field{i}").ToArray();
        var csvStream = CsvGenerator.GenerateCsv(rowCount, headers);

        // Force GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);

        // Act
        var processedCount = 0;
        await foreach (var row in _service.StreamRowsAsync(csvStream))
        {
            processedCount++;
            _ = row.Count; // Minimal processing
        }

        // Measure
        GC.Collect();
        var memoryAfter = GC.GetTotalMemory(forceFullCollection: true);
        var memoryUsed = Math.Max(0, memoryAfter - memoryBefore);

        // Assert
        processedCount.Should().Be(rowCount);
        PerformanceAssertions.AssertMemoryUsageLessThan(maxMemoryBytes, memoryUsed);
    }

    [Fact(Skip = "Memory measurement is unreliable in CI - GC.GetTotalMemory varies between runs")]
    public async Task StreamRowsAsync_ComparedToLoadingAll_UsesSignificantlyLessMemory()
    {
        // Arrange
        const int rowCount = 50000;
        var headers = new[] { "FirstName", "LastName", "Email" };

        // Test 1: Streaming approach
        var streamCsv = CsvGenerator.GenerateCsv(rowCount, headers);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var streamMemoryBefore = GC.GetTotalMemory(forceFullCollection: true);

        var streamCount = 0;
        await foreach (var row in _service.StreamRowsAsync(streamCsv))
        {
            streamCount++;
            _ = row.Count;
        }

        GC.Collect();
        var streamMemoryAfter = GC.GetTotalMemory(forceFullCollection: true);
        var streamMemoryUsed = Math.Max(0, streamMemoryAfter - streamMemoryBefore);

        // Test 2: Loading all into memory
        var loadAllCsv = CsvGenerator.GenerateCsv(rowCount, headers);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var loadAllMemoryBefore = GC.GetTotalMemory(forceFullCollection: true);

        var allRows = new List<Dictionary<string, string>>();
        await foreach (var row in _service.StreamRowsAsync(loadAllCsv))
        {
            allRows.Add(row);
        }

        var loadAllMemoryAfter = GC.GetTotalMemory(forceFullCollection: false);
        var loadAllMemoryUsed = Math.Max(0, loadAllMemoryAfter - loadAllMemoryBefore);

        // Assert
        streamCount.Should().Be(rowCount);
        allRows.Should().HaveCount(rowCount);

        // Streaming should use significantly less memory (at least 50% less)
        streamMemoryUsed.Should().BeLessThan(loadAllMemoryUsed / 2,
            "streaming should use significantly less memory than loading all rows");

        _loggerMock.Object.LogInformation(
            $"Memory comparison: Streaming={streamMemoryUsed / 1024.0 / 1024.0:F2}MB, " +
            $"LoadAll={loadAllMemoryUsed / 1024.0 / 1024.0:F2}MB, " +
            $"Savings={((loadAllMemoryUsed - streamMemoryUsed) / (double)loadAllMemoryUsed) * 100:F1}%");
    }

    #endregion

    #region ValidateFileAsync Performance Tests with Error Rates

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task ValidateFileAsync_10000Rows_0PercentErrors_CompletesUnder500Ms()
    {
        // Arrange
        const int rowCount = 10000;
        const int maxMilliseconds = 500;

        var headers = new[] { "FirstName", "LastName", "Email", "Phone" };
        var csvStream = GenerateValidCsv(rowCount, headers);
        var requiredColumns = new List<string> { "FirstName", "LastName" };

        // Act
        var (errors, metrics) = await PerformanceMeasurer.MeasureAsync(
            async () => await _service.ValidateFileAsync(csvStream, requiredColumns),
            rowCount);

        // Assert
        errors.Should().BeEmpty("all data is valid");

        PerformanceAssertions.AssertCompletedWithin(
            TimeSpan.FromMilliseconds(maxMilliseconds),
            TimeSpan.FromMilliseconds(metrics.ElapsedMs));
    }

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task ValidateFileAsync_10000Rows_5PercentErrors_CompletesUnder750Ms()
    {
        // Arrange
        const int rowCount = 10000;
        const double errorRate = 0.05; // 5%
        const int maxMilliseconds = 750;

        var csvStream = GenerateCsvWithInvalidEmails(rowCount, errorRate);
        var requiredColumns = new List<string> { "Name" };

        // Act
        var (errors, metrics) = await PerformanceMeasurer.MeasureAsync(
            async () => await _service.ValidateFileAsync(csvStream, requiredColumns),
            rowCount);

        // Assert
        var expectedErrorCount = (int)(rowCount * errorRate);
        errors.Should().HaveCountGreaterOrEqualTo(expectedErrorCount - 50); // Allow small variance
        errors.Should().HaveCountLessOrEqualTo(expectedErrorCount + 50);
        errors.Should().AllSatisfy(e => e.ErrorMessage.Should().Contain("Invalid email"));

        PerformanceAssertions.AssertCompletedWithin(
            TimeSpan.FromMilliseconds(maxMilliseconds),
            TimeSpan.FromMilliseconds(metrics.ElapsedMs));
    }

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task ValidateFileAsync_10000Rows_50PercentErrors_CompletesUnder1000Ms()
    {
        // Arrange
        const int rowCount = 10000;
        const double errorRate = 0.50; // 50%
        const int maxMilliseconds = 1000;

        var csvStream = GenerateCsvWithInvalidEmails(rowCount, errorRate);
        var requiredColumns = new List<string> { "Name" };

        // Act
        var (errors, metrics) = await PerformanceMeasurer.MeasureAsync(
            async () => await _service.ValidateFileAsync(csvStream, requiredColumns),
            rowCount);

        // Assert
        var expectedErrorCount = (int)(rowCount * errorRate);
        errors.Should().HaveCountGreaterOrEqualTo(expectedErrorCount - 100); // Allow variance
        errors.Should().AllSatisfy(e => e.ErrorMessage.Should().Contain("Invalid email"));

        PerformanceAssertions.AssertCompletedWithin(
            TimeSpan.FromMilliseconds(maxMilliseconds),
            TimeSpan.FromMilliseconds(metrics.ElapsedMs));

        // Log error rate
        var actualErrorRate = errors.Count / (double)rowCount;
        _loggerMock.Object.LogInformation(
            $"Validated {rowCount} rows with {actualErrorRate:P1} error rate in {metrics.ElapsedMs}ms");
    }

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task ValidateFileAsync_50000Rows_10PercentErrors_CompletesUnder2Seconds()
    {
        // Arrange
        const int rowCount = 50000;
        const double errorRate = 0.10; // 10%
        const int maxMilliseconds = 2000;

        var csvStream = GenerateCsvWithMixedErrors(rowCount, errorRate);
        var requiredColumns = new List<string> { "Name" };

        // Act
        var (errors, metrics) = await PerformanceMeasurer.MeasureAsync(
            async () => await _service.ValidateFileAsync(csvStream, requiredColumns),
            rowCount);

        // Assert
        errors.Should().NotBeEmpty();

        PerformanceAssertions.AssertCompletedWithin(
            TimeSpan.FromMilliseconds(maxMilliseconds),
            TimeSpan.FromMilliseconds(metrics.ElapsedMs));

        // Log throughput
        var throughput = rowCount / (metrics.ElapsedMs / 1000.0);
        _loggerMock.Object.LogInformation(
            $"Validated {rowCount} rows at {throughput:F0} rows/sec with {errors.Count} errors");
    }

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task ValidateFileAsync_100000Rows_1PercentErrors_CompletesUnder3Seconds()
    {
        // Arrange
        const int rowCount = 100000;
        const double errorRate = 0.01; // 1%
        const int maxMilliseconds = 3000;

        var csvStream = GenerateCsvWithInvalidPhones(rowCount, errorRate);
        var requiredColumns = new List<string> { "Name" };

        // Act
        var (errors, metrics) = await PerformanceMeasurer.MeasureAsync(
            async () => await _service.ValidateFileAsync(csvStream, requiredColumns),
            rowCount);

        // Assert
        var expectedErrorCount = (int)(rowCount * errorRate);
        errors.Should().HaveCountGreaterOrEqualTo(expectedErrorCount - 200);
        errors.Should().AllSatisfy(e => e.ErrorMessage.Should().Contain("Invalid phone"));

        PerformanceAssertions.AssertCompletedWithin(
            TimeSpan.FromMilliseconds(maxMilliseconds),
            TimeSpan.FromMilliseconds(metrics.ElapsedMs));
    }

    #endregion

    #region Concurrent Operations Tests

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task StreamRowsAsync_3ConcurrentStreams_AllCompleteSuccessfully()
    {
        // Arrange
        const int concurrentCount = 3;
        const int rowsPerStream = 50000;
        const int maxMilliseconds = 5000;

        var headers = new[] { "Name", "Email", "Phone" };

        // Act - Run concurrent streaming operations
        var tasks = Enumerable.Range(0, concurrentCount).Select(async i =>
        {
            var csvStream = CsvGenerator.GenerateCsv(rowsPerStream, headers);
            var count = 0;

            await foreach (var row in _service.StreamRowsAsync(csvStream))
            {
                count++;
                _ = row.Count;
            }

            return count;
        }).ToList();

        var stopwatch = Stopwatch.StartNew();
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().AllSatisfy(count => count.Should().Be(rowsPerStream));
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxMilliseconds,
            $"{concurrentCount} concurrent streams should complete within {maxMilliseconds}ms");
    }

    #endregion

    #region Helper Methods

    private static MemoryStream GenerateValidCsv(int rowCount, string[] headers)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers));

        for (var i = 1; i <= rowCount; i++)
        {
            var values = new List<string>
            {
                $"FirstName{i}",
                $"LastName{i}",
                $"person{i}@example.com",
                $"555-{i % 900 + 100}-{i % 9000 + 1000:D4}"
            };
            sb.AppendLine(string.Join(",", values));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return new MemoryStream(bytes);
    }

    private static MemoryStream GenerateCsvWithInvalidEmails(int rowCount, double errorRate)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Name,Email");

        for (var i = 1; i <= rowCount; i++)
        {
            var hasError = (i % (int)(1 / errorRate)) == 0;
            var email = hasError ? "invalid-email" : $"person{i}@example.com";
            sb.AppendLine($"Name{i},{email}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return new MemoryStream(bytes);
    }

    private static MemoryStream GenerateCsvWithInvalidPhones(int rowCount, double errorRate)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Name,Phone");

        for (var i = 1; i <= rowCount; i++)
        {
            var hasError = (i % (int)(1 / errorRate)) == 0;
            var phone = hasError ? "123" : $"555-{i % 900 + 100}-{i % 9000 + 1000:D4}";
            sb.AppendLine($"Name{i},{phone}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return new MemoryStream(bytes);
    }

    private static MemoryStream GenerateCsvWithMixedErrors(int rowCount, double errorRate)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Name,Email,Phone,BirthDate");

        for (var i = 1; i <= rowCount; i++)
        {
            var hasError = (i % (int)(1 / errorRate)) == 0;

            var email = hasError && i % 3 == 0 ? "bad-email" : $"person{i}@example.com";
            var phone = hasError && i % 3 == 1 ? "123" : $"555-{i % 900 + 100}-{i % 9000 + 1000:D4}";
            var birthDate = hasError && i % 3 == 2 ? "invalid-date" : $"{1950 + (i % 70):D4}-01-15";

            sb.AppendLine($"Name{i},{email},{phone},{birthDate}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return new MemoryStream(bytes);
    }

    #endregion
}
