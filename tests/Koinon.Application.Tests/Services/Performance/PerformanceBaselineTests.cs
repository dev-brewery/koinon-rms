using FluentAssertions;
using Xunit;

namespace Koinon.Application.Tests.Services.Performance;

/// <summary>
/// Defines and validates performance baselines for the Export and Import modules.
/// This class serves as the canonical reference for all performance expectations.
///
/// <para><b>Environment Assumptions:</b></para>
/// <list type="bullet">
///   <item>Development/CI hardware: ~4 CPU cores, 8GB+ RAM</item>
///   <item>In-memory database for testing (EntityFrameworkCore InMemory provider)</item>
///   <item>No network I/O (mocked external dependencies)</item>
///   <item>Tests run in parallel isolation (separate database per test)</item>
/// </list>
///
/// <para><b>Adjustment Guidelines:</b></para>
/// <list type="bullet">
///   <item>For slower hardware: Multiply time baselines by 1.5-2x</item>
///   <item>For faster hardware: Can reduce time baselines by 0.5-0.75x</item>
///   <item>For production database: Add 20-50% overhead for real I/O</item>
///   <item>Memory limits are environment-agnostic (algorithm efficiency, not hardware)</item>
/// </list>
/// </summary>
[Trait("Category", "Performance")]
public class PerformanceBaselineTests
{
    #region Export Baseline Tests

    [Fact]
    public void ExportBaselines_TimeThresholds_AreReasonable()
    {
        // Arrange & Assert - Verify export time baselines are within reasonable ranges
        PerformanceBaselines.Export.Time1KRecordsMs.Should().BeInRange(500, 2000,
            "1K export should complete between 0.5-2 seconds on typical hardware");

        PerformanceBaselines.Export.Time10KRecordsMs.Should().BeInRange(2000, 10000,
            "10K export should complete between 2-10 seconds on typical hardware");

        PerformanceBaselines.Export.Time100KRecordsMs.Should().BeInRange(10000, 60000,
            "100K export should complete between 10-60 seconds on typical hardware");

        // Verify linear scaling (within reasonable tolerance)
        var ratio10Kto1K = (double)PerformanceBaselines.Export.Time10KRecordsMs /
                           PerformanceBaselines.Export.Time1KRecordsMs;
        ratio10Kto1K.Should().BeInRange(3, 15,
            "10K export should take 3-15x longer than 1K (allows for overhead)");

        var ratio100Kto10K = (double)PerformanceBaselines.Export.Time100KRecordsMs /
                             PerformanceBaselines.Export.Time10KRecordsMs;
        ratio100Kto10K.Should().BeInRange(3, 15,
            "100K export should take 3-15x longer than 10K (allows for overhead)");
    }

    [Fact]
    public void ExportBaselines_MemoryLimits_ScaleAppropriately()
    {
        // Arrange & Assert - Verify memory limits scale linearly with data size
        PerformanceBaselines.Export.MaxMemory1KRecordsMb.Should().BeInRange(5, 50,
            "1K export should use 5-50 MB (includes framework overhead)");

        PerformanceBaselines.Export.MaxMemory10KRecordsMb.Should().BeInRange(20, 200,
            "10K export should use 20-200 MB");

        PerformanceBaselines.Export.MaxMemory100KRecordsMb.Should().BeInRange(100, 1000,
            "100K export should use 100-1000 MB (streaming should limit growth)");

        // Verify memory growth is sub-linear (streaming efficiency)
        var memoryRatio10Kto1K = PerformanceBaselines.Export.MaxMemory10KRecordsMb /
                                 PerformanceBaselines.Export.MaxMemory1KRecordsMb;
        memoryRatio10Kto1K.Should().BeLessThan(10,
            "memory growth should be sub-linear due to streaming (10K should use <10x memory of 1K)");

        var memoryRatio100Kto10K = PerformanceBaselines.Export.MaxMemory100KRecordsMb /
                                   PerformanceBaselines.Export.MaxMemory10KRecordsMb;
        memoryRatio100Kto10K.Should().BeLessThan(10,
            "memory growth should be sub-linear due to streaming (100K should use <10x memory of 10K)");
    }

    [Fact]
    public void ExportBaselines_ConcurrentOperations_AreRealistic()
    {
        // Arrange & Assert - Verify concurrent operation limits are achievable
        PerformanceBaselines.Export.MaxConcurrentExports.Should().BeInRange(3, 20,
            "system should handle 3-20 concurrent exports on typical hardware");

        PerformanceBaselines.Export.ConcurrentOverheadMultiplier.Should().BeInRange(1.5, 5.0,
            "concurrent operations should complete within 1.5-5x single operation time");

        // Verify that concurrent multiplier is reasonable for the max concurrent count
        var worstCaseTime = PerformanceBaselines.Export.Time1KRecordsMs *
                           PerformanceBaselines.Export.ConcurrentOverheadMultiplier;
        worstCaseTime.Should().BeLessThan(10000,
            "worst-case concurrent 1K exports should still complete within 10 seconds");
    }

    #endregion

    #region Import Baseline Tests

    [Fact]
    public void ImportBaselines_TimeThresholds_AreReasonable()
    {
        // Arrange & Assert - Verify import time baselines
        PerformanceBaselines.Import.Time1KRowsMs.Should().BeInRange(1000, 5000,
            "1K import should complete between 1-5 seconds (includes validation)");

        PerformanceBaselines.Import.Time10KRowsMs.Should().BeInRange(5000, 20000,
            "10K import should complete between 5-20 seconds");

        PerformanceBaselines.Import.BackgroundJobThreshold.Should().BeInRange(5000, 100000,
            "background job threshold should be 5K-100K rows (balance responsiveness vs overhead)");

        // Verify scaling
        var ratio10Kto1K = (double)PerformanceBaselines.Import.Time10KRowsMs /
                           PerformanceBaselines.Import.Time1KRowsMs;
        ratio10Kto1K.Should().BeInRange(3, 15,
            "10K import should take 3-15x longer than 1K");
    }

    [Fact]
    public void ImportBaselines_ValidationTimes_AreFast()
    {
        // Arrange & Assert - Validation should be significantly faster than full import
        PerformanceBaselines.Import.ValidationTime1KRowsMs.Should().BeLessThan(
            PerformanceBaselines.Import.Time1KRowsMs / 2,
            "validation should be at least 2x faster than full import");

        PerformanceBaselines.Import.ValidationTime10KRowsMs.Should().BeLessThan(
            PerformanceBaselines.Import.Time10KRowsMs / 3,
            "validation should be at least 3x faster than full import for larger datasets");

        PerformanceBaselines.Import.ValidationTime1KRowsMs.Should().BeInRange(200, 1000,
            "1K validation should complete in 200ms-1s");

        PerformanceBaselines.Import.ValidationTime10KRowsMs.Should().BeInRange(1000, 5000,
            "10K validation should complete in 1-5s");
    }

    [Fact]
    public void ImportBaselines_MemoryLimits_AreEfficient()
    {
        // Arrange & Assert - Import should use reasonable memory (streaming)
        PerformanceBaselines.Import.MaxMemory1KRowsMb.Should().BeInRange(10, 100,
            "1K import should use 10-100 MB");

        PerformanceBaselines.Import.MaxMemory10KRowsMb.Should().BeInRange(30, 300,
            "10K import should use 30-300 MB");

        // Verify streaming efficiency
        var memoryRatio = PerformanceBaselines.Import.MaxMemory10KRowsMb /
                         PerformanceBaselines.Import.MaxMemory1KRowsMb;
        memoryRatio.Should().BeLessThan(5,
            "memory should grow sub-linearly with streaming (10K should use <5x memory of 1K)");
    }

    [Fact]
    public void ImportBaselines_ConcurrentOperations_AreRealistic()
    {
        // Arrange & Assert
        PerformanceBaselines.Import.MaxConcurrentImports.Should().BeInRange(2, 10,
            "system should handle 2-10 concurrent imports");

        PerformanceBaselines.Import.ConcurrentOverheadMultiplier.Should().BeInRange(1.5, 5.0,
            "concurrent imports should complete within 1.5-5x single import time");
    }

    #endregion

    #region CSV Parser Baseline Tests

    [Fact]
    public void CsvParserBaselines_PreviewTimes_AreReasonable()
    {
        // Arrange & Assert - Preview should be very fast (only reads header + first few rows)
        PerformanceBaselines.CsvParser.PreviewTime50KRowsMs.Should().BeInRange(100, 500,
            "50K preview should complete in 100-500ms (only samples first rows)");

        PerformanceBaselines.CsvParser.PreviewTime100KRowsMs.Should().BeInRange(200, 1000,
            "100K preview should complete in 200ms-1s");

        // Preview time should be relatively constant (doesn't read all rows)
        var ratio = (double)PerformanceBaselines.CsvParser.PreviewTime100KRowsMs /
                    PerformanceBaselines.CsvParser.PreviewTime50KRowsMs;
        ratio.Should().BeLessThan(3,
            "preview time should scale sub-linearly (only reads first N rows)");
    }

    [Fact]
    public void CsvParserBaselines_StreamingMemory_IsEfficient()
    {
        // Arrange & Assert - Streaming should use minimal memory
        PerformanceBaselines.CsvParser.MaxStreamingMemory50KRowsMb.Should().BeInRange(10, 50,
            "50K streaming should use 10-50 MB (row-by-row processing)");

        PerformanceBaselines.CsvParser.MaxStreamingMemory100KRowsMb.Should().BeInRange(20, 100,
            "100K streaming should use 20-100 MB");

        // Verify streaming efficiency (memory should NOT double when rows double)
        var memoryRatio = PerformanceBaselines.CsvParser.MaxStreamingMemory100KRowsMb /
                         PerformanceBaselines.CsvParser.MaxStreamingMemory50KRowsMb;
        memoryRatio.Should().BeLessThan(3,
            "streaming memory should grow sub-linearly (100K should use <3x memory of 50K)");

        // Absolute limit check
        PerformanceBaselines.CsvParser.MaxStreamingMemory100KRowsMb.Should().BeLessThan(100,
            "streaming 100K rows should never exceed 100 MB");
    }

    [Fact]
    public void CsvParserBaselines_ValidationWithErrors_DegracesGracefully()
    {
        // Arrange & Assert - Validation time should not explode with error rate
        var timeRatio = (double)PerformanceBaselines.CsvParser.ValidationTime10KRows50PctErrorsMs /
                        PerformanceBaselines.CsvParser.ValidationTime10KRows0PctErrorsMs;

        timeRatio.Should().BeLessThan(3,
            "50% error rate should not cause more than 3x slowdown");

        PerformanceBaselines.CsvParser.ValidationTime10KRows0PctErrorsMs.Should().BeInRange(200, 1000,
            "10K validation with 0% errors should complete in 200ms-1s");

        PerformanceBaselines.CsvParser.ValidationTime10KRows5PctErrorsMs.Should().BeInRange(300, 1500,
            "10K validation with 5% errors should complete in 300ms-1.5s");

        PerformanceBaselines.CsvParser.ValidationTime10KRows50PctErrorsMs.Should().BeInRange(500, 2000,
            "10K validation with 50% errors should complete in 500ms-2s");
    }

    #endregion

    #region Baseline Consistency Tests

    [Fact]
    public void Baselines_CrossModule_AreConsistent()
    {
        // Arrange & Assert - Verify that import time > export time (import does more work)
        PerformanceBaselines.Import.Time1KRowsMs.Should().BeGreaterThan(
            PerformanceBaselines.Export.Time1KRecordsMs,
            "import should take longer than export (validation + transformation)");

        // CSV parser should be fastest (minimal processing)
        PerformanceBaselines.CsvParser.PreviewTime100KRowsMs.Should().BeLessThan(
            PerformanceBaselines.Export.Time1KRecordsMs,
            "CSV preview should be faster than any full operation");

        // Memory consistency: streaming should use less than loading all
        PerformanceBaselines.CsvParser.MaxStreamingMemory100KRowsMb.Should().BeLessThan(
            PerformanceBaselines.Export.MaxMemory100KRecordsMb,
            "CSV streaming should use less memory than export (no business logic)");
    }

    [Fact]
    public void Baselines_AllValues_ArePositive()
    {
        // Arrange & Assert - Sanity check: all baselines should be positive numbers
        PerformanceBaselines.Export.Time1KRecordsMs.Should().BePositive();
        PerformanceBaselines.Export.Time10KRecordsMs.Should().BePositive();
        PerformanceBaselines.Export.Time100KRecordsMs.Should().BePositive();
        PerformanceBaselines.Export.MaxMemory1KRecordsMb.Should().BePositive();
        PerformanceBaselines.Export.MaxMemory10KRecordsMb.Should().BePositive();
        PerformanceBaselines.Export.MaxMemory100KRecordsMb.Should().BePositive();
        PerformanceBaselines.Export.MaxConcurrentExports.Should().BePositive();
        PerformanceBaselines.Export.ConcurrentOverheadMultiplier.Should().BePositive();

        PerformanceBaselines.Import.Time1KRowsMs.Should().BePositive();
        PerformanceBaselines.Import.Time10KRowsMs.Should().BePositive();
        PerformanceBaselines.Import.BackgroundJobThreshold.Should().BePositive();
        PerformanceBaselines.Import.ValidationTime1KRowsMs.Should().BePositive();
        PerformanceBaselines.Import.ValidationTime10KRowsMs.Should().BePositive();
        PerformanceBaselines.Import.MaxMemory1KRowsMb.Should().BePositive();
        PerformanceBaselines.Import.MaxMemory10KRowsMb.Should().BePositive();
        PerformanceBaselines.Import.MaxConcurrentImports.Should().BePositive();
        PerformanceBaselines.Import.ConcurrentOverheadMultiplier.Should().BePositive();

        PerformanceBaselines.CsvParser.PreviewTime50KRowsMs.Should().BePositive();
        PerformanceBaselines.CsvParser.PreviewTime100KRowsMs.Should().BePositive();
        PerformanceBaselines.CsvParser.MaxStreamingMemory50KRowsMb.Should().BePositive();
        PerformanceBaselines.CsvParser.MaxStreamingMemory100KRowsMb.Should().BePositive();
        PerformanceBaselines.CsvParser.ValidationTime10KRows0PctErrorsMs.Should().BePositive();
        PerformanceBaselines.CsvParser.ValidationTime10KRows5PctErrorsMs.Should().BePositive();
        PerformanceBaselines.CsvParser.ValidationTime10KRows50PctErrorsMs.Should().BePositive();
    }

    #endregion
}

/// <summary>
/// Canonical performance baselines for Export, Import, and CSV parsing operations.
///
/// <para><b>Purpose:</b></para>
/// <list type="bullet">
///   <item>Single source of truth for performance expectations</item>
///   <item>Referenced by all performance tests across the codebase</item>
///   <item>Documents performance characteristics for developers</item>
///   <item>Enables regression detection in CI/CD pipelines</item>
/// </list>
///
/// <para><b>Baseline Selection Rationale:</b></para>
///
/// <para><b>Export Time Baselines:</b></para>
/// <list type="bullet">
///   <item><b>1K = 1000ms:</b> Balances responsiveness with database query overhead</item>
///   <item><b>10K = 5000ms:</b> Allows for query complexity and CSV generation</item>
///   <item><b>100K = 30000ms:</b> Large dataset handling with streaming</item>
/// </list>
///
/// <para><b>Import Time Baselines:</b></para>
/// <list type="bullet">
///   <item><b>1K = 2000ms:</b> Includes parsing, validation, and entity creation</item>
///   <item><b>10K = 10000ms:</b> Accounts for batch processing overhead</item>
///   <item><b>Background threshold = 10000:</b> Keeps UI responsive for large imports</item>
/// </list>
///
/// <para><b>Memory Baselines:</b></para>
/// <list type="bullet">
///   <item>Export: Allows for query results + CSV generation buffer</item>
///   <item>Import: Accounts for streaming parser + entity materialization</item>
///   <item>CSV Parser: Pure streaming efficiency (minimal buffering)</item>
/// </list>
///
/// <para><b>Concurrent Operation Limits:</b></para>
/// <list type="bullet">
///   <item>Export: 10 concurrent (typically background jobs)</item>
///   <item>Import: 3 concurrent (more resource-intensive than export)</item>
///   <item>Overhead: 3x multiplier accounts for thread contention and I/O blocking</item>
/// </list>
/// </summary>
public static class PerformanceBaselines
{
    /// <summary>
    /// Performance baselines for data export operations.
    /// </summary>
    public static class Export
    {
        /// <summary>
        /// Maximum time to export 1,000 records (in milliseconds).
        /// Chosen to ensure responsive UI for small exports.
        /// </summary>
        public const int Time1KRecordsMs = 1000;

        /// <summary>
        /// Maximum time to export 10,000 records (in milliseconds).
        /// Balances user wait time with realistic database query performance.
        /// </summary>
        public const int Time10KRecordsMs = 5000;

        /// <summary>
        /// Maximum time to export 100,000 records (in milliseconds).
        /// Large datasets may be processed as background jobs.
        /// Allows for streaming and chunked processing.
        /// </summary>
        public const int Time100KRecordsMs = 30000;

        /// <summary>
        /// Maximum memory usage for exporting 1,000 records (in MB).
        /// Includes query results + CSV generation overhead.
        /// </summary>
        public const int MaxMemory1KRecordsMb = 10;

        /// <summary>
        /// Maximum memory usage for exporting 10,000 records (in MB).
        /// Should scale sub-linearly due to streaming.
        /// </summary>
        public const int MaxMemory10KRecordsMb = 50;

        /// <summary>
        /// Maximum memory usage for exporting 100,000 records (in MB).
        /// Streaming should prevent linear memory growth.
        /// </summary>
        public const int MaxMemory100KRecordsMb = 200;

        /// <summary>
        /// Maximum number of concurrent export operations supported.
        /// Based on typical background job concurrency limits.
        /// </summary>
        public const int MaxConcurrentExports = 10;

        /// <summary>
        /// Multiplier for concurrent operation overhead.
        /// Concurrent exports should complete within 3x single export time.
        /// Accounts for thread contention and database connection pooling.
        /// </summary>
        public const double ConcurrentOverheadMultiplier = 3.0;
    }

    /// <summary>
    /// Performance baselines for data import operations.
    /// </summary>
    public static class Import
    {
        /// <summary>
        /// Maximum time to import 1,000 rows (in milliseconds).
        /// Includes CSV parsing, validation, and entity creation.
        /// </summary>
        public const int Time1KRowsMs = 2000;

        /// <summary>
        /// Maximum time to import 10,000 rows (in milliseconds).
        /// Allows for batch processing and validation overhead.
        /// </summary>
        public const int Time10KRowsMs = 10000;

        /// <summary>
        /// Row count threshold for enqueuing import as background job.
        /// Chosen to keep UI responsive while avoiding excessive job overhead.
        /// </summary>
        public const int BackgroundJobThreshold = 10000;

        /// <summary>
        /// Maximum time to validate 1,000 rows without importing (in milliseconds).
        /// Validation should be significantly faster than full import.
        /// </summary>
        public const int ValidationTime1KRowsMs = 500;

        /// <summary>
        /// Maximum time to validate 10,000 rows without importing (in milliseconds).
        /// Dry-run validation for user feedback before committing.
        /// </summary>
        public const int ValidationTime10KRowsMs = 2000;

        /// <summary>
        /// Maximum memory usage for importing 1,000 rows (in MB).
        /// Includes parser buffers + entity materialization.
        /// </summary>
        public const int MaxMemory1KRowsMb = 20;

        /// <summary>
        /// Maximum memory usage for importing 10,000 rows (in MB).
        /// Streaming should prevent linear growth.
        /// </summary>
        public const int MaxMemory10KRowsMb = 80;

        /// <summary>
        /// Maximum number of concurrent import operations supported.
        /// Lower than exports due to higher resource intensity.
        /// </summary>
        public const int MaxConcurrentImports = 3;

        /// <summary>
        /// Multiplier for concurrent operation overhead.
        /// Concurrent imports should complete within 2.5x single import time.
        /// </summary>
        public const double ConcurrentOverheadMultiplier = 2.5;
    }

    /// <summary>
    /// Performance baselines for CSV parsing operations.
    /// </summary>
    public static class CsvParser
    {
        /// <summary>
        /// Maximum time to generate preview of 50,000 row CSV (in milliseconds).
        /// Preview only reads header + first 5 rows, should be very fast.
        /// </summary>
        public const int PreviewTime50KRowsMs = 250;

        /// <summary>
        /// Maximum time to generate preview of 100,000 row CSV (in milliseconds).
        /// Should scale sub-linearly (only samples beginning of file).
        /// </summary>
        public const int PreviewTime100KRowsMs = 500;

        /// <summary>
        /// Maximum memory usage for streaming 50,000 rows (in MB).
        /// Pure streaming efficiency - no buffering of all rows.
        /// </summary>
        public const int MaxStreamingMemory50KRowsMb = 25;

        /// <summary>
        /// Maximum memory usage for streaming 100,000 rows (in MB).
        /// Should grow sub-linearly due to row-by-row processing.
        /// </summary>
        public const int MaxStreamingMemory100KRowsMb = 50;

        /// <summary>
        /// Maximum time to validate 10,000 rows with 0% error rate (in milliseconds).
        /// Fast-path validation when data is clean.
        /// </summary>
        public const int ValidationTime10KRows0PctErrorsMs = 500;

        /// <summary>
        /// Maximum time to validate 10,000 rows with 5% error rate (in milliseconds).
        /// Small number of errors should not significantly impact performance.
        /// </summary>
        public const int ValidationTime10KRows5PctErrorsMs = 750;

        /// <summary>
        /// Maximum time to validate 10,000 rows with 50% error rate (in milliseconds).
        /// High error rate should still complete reasonably fast.
        /// Allows for error message generation overhead.
        /// </summary>
        public const int ValidationTime10KRows50PctErrorsMs = 1000;
    }
}
