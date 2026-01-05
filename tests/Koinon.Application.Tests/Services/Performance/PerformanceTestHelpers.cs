using System.Diagnostics;
using System.Text;
using FluentAssertions;

namespace Koinon.Application.Tests.Services.Performance;

/// <summary>
/// Generates CSV data for performance testing.
/// </summary>
public static class CsvGenerator
{
    /// <summary>
    /// Generates a CSV MemoryStream with the specified number of rows and headers.
    /// </summary>
    /// <param name="rowCount">Number of data rows to generate.</param>
    /// <param name="headers">Array of header column names.</param>
    /// <returns>MemoryStream containing CSV data.</returns>
    public static MemoryStream GenerateCsv(int rowCount, string[] headers)
    {
        var sb = new StringBuilder();

        // Add headers
        sb.AppendLine(string.Join(",", headers));

        // Add rows with dummy data
        for (var i = 1; i <= rowCount; i++)
        {
            var rowData = headers.Select((_, index) => $"Value{i}_{index}");
            sb.AppendLine(string.Join(",", rowData));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var stream = new MemoryStream(bytes);
        return stream;
    }

    /// <summary>
    /// Generates a CSV MemoryStream with person import data.
    /// </summary>
    /// <param name="rowCount">Number of person records to generate.</param>
    /// <returns>MemoryStream containing person CSV data.</returns>
    public static MemoryStream GeneratePersonCsv(int rowCount)
    {
        var headers = new[]
        {
            "FirstName",
            "LastName",
            "Email",
            "Phone",
            "BirthDate",
            "Gender",
            "Street1",
            "City",
            "State",
            "PostalCode"
        };

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers));

        for (var i = 1; i <= rowCount; i++)
        {
            var birthYear = 1950 + (i % 70);
            var month = (i % 12) + 1;
            var day = (i % 28) + 1;
            var gender = i % 2 == 0 ? "Male" : "Female";
            var state = i % 2 == 0 ? "CA" : "NY";

            sb.AppendLine($"FirstName{i},LastName{i},person{i}@example.com,555-{i:D3}-{(i * 7):D4},{birthYear:D4}-{month:D2}-{day:D2},{gender},{i} Main St,City{i},{state},{10000 + i:D5}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var stream = new MemoryStream(bytes);
        return stream;
    }

    /// <summary>
    /// Generates a CSV MemoryStream with family import data.
    /// </summary>
    /// <param name="rowCount">Number of family records to generate.</param>
    /// <returns>MemoryStream containing family CSV data.</returns>
    public static MemoryStream GenerateFamilyCsv(int rowCount)
    {
        var headers = new[]
        {
            "FamilyName",
            "Street1",
            "Street2",
            "City",
            "State",
            "PostalCode",
            "Country"
        };

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers));

        for (var i = 1; i <= rowCount; i++)
        {
            var state = i % 3 == 0 ? "CA" : i % 3 == 1 ? "NY" : "TX";
            var street2 = i % 5 == 0 ? $"Apt {i % 100}" : "";

            sb.AppendLine($"Family{i},{i} Family Lane,{street2},City{i},{state},{10000 + i:D5},USA");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var stream = new MemoryStream(bytes);
        return stream;
    }
}

/// <summary>
/// Captures performance metrics for a test operation.
/// </summary>
/// <param name="ElapsedMs">Elapsed time in milliseconds.</param>
/// <param name="MemoryUsedBytes">Memory used in bytes.</param>
/// <param name="RecordCount">Number of records processed.</param>
public record PerformanceMetrics(
    long ElapsedMs,
    long MemoryUsedBytes,
    int RecordCount
);

/// <summary>
/// Measures performance metrics for async operations.
/// </summary>
public static class PerformanceMeasurer
{
    /// <summary>
    /// Measures timing and memory usage of an async operation.
    /// </summary>
    /// <typeparam name="T">Return type of the operation.</typeparam>
    /// <param name="action">The async operation to measure.</param>
    /// <param name="recordCount">Number of records being processed (for metrics).</param>
    /// <returns>Tuple containing the result and performance metrics.</returns>
    public static async Task<(T Result, PerformanceMetrics Metrics)> MeasureAsync<T>(
        Func<Task<T>> action,
        int recordCount)
    {
        // Force garbage collection before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);
        var stopwatch = Stopwatch.StartNew();

        var result = await action();

        stopwatch.Stop();
        var memoryAfter = GC.GetTotalMemory(forceFullCollection: true);

        var metrics = new PerformanceMetrics(
            ElapsedMs: stopwatch.ElapsedMilliseconds,
            MemoryUsedBytes: Math.Max(0, memoryAfter - memoryBefore),
            RecordCount: recordCount
        );

        return (result, metrics);
    }
}

/// <summary>
/// Provides FluentAssertions-based assertions for performance metrics.
/// </summary>
public static class PerformanceAssertions
{
    /// <summary>
    /// Asserts that the actual elapsed time is within the expected time.
    /// </summary>
    /// <param name="expected">Expected maximum duration.</param>
    /// <param name="actual">Actual duration.</param>
    public static void AssertCompletedWithin(TimeSpan expected, TimeSpan actual)
    {
        actual.Should().BeLessThanOrEqualTo(expected,
            $"operation should complete within {expected.TotalMilliseconds}ms but took {actual.TotalMilliseconds}ms");
    }

    /// <summary>
    /// Asserts that the actual memory usage is less than the maximum allowed.
    /// </summary>
    /// <param name="maxBytes">Maximum allowed memory usage in bytes.</param>
    /// <param name="actualBytes">Actual memory usage in bytes.</param>
    public static void AssertMemoryUsageLessThan(long maxBytes, long actualBytes)
    {
        actualBytes.Should().BeLessThan(maxBytes,
            $"operation should use less than {maxBytes / 1024.0 / 1024.0:F2}MB but used {actualBytes / 1024.0 / 1024.0:F2}MB");
    }
}
