using System.Diagnostics;
using AutoMapper;
using FluentAssertions;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Exports;
using Koinon.Application.Interfaces;
using Koinon.Application.Services;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services.Performance;

/// <summary>
/// Performance tests for DataExportService with large datasets.
/// </summary>
[Trait("Category", "Performance")]
public class DataExportPerformanceTests
{
    private readonly Mock<IBackgroundJobService> _backgroundJobServiceMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<DataExportService>> _loggerMock;

    public DataExportPerformanceTests()
    {
        _backgroundJobServiceMock = new Mock<IBackgroundJobService>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<DataExportService>>();
    }

    private IApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }

    private DataExportService CreateService(
        IApplicationDbContext context,
        IEnumerable<IExportDataProvider>? exportProviders = null,
        IEnumerable<IExportFormatGenerator>? formatGenerators = null)
    {
        exportProviders ??= new List<IExportDataProvider>();
        formatGenerators ??= new List<IExportFormatGenerator>();

        return new DataExportService(
            context,
            _mapperMock.Object,
            _backgroundJobServiceMock.Object,
            _fileStorageServiceMock.Object,
            exportProviders,
            formatGenerators,
            _loggerMock.Object);
    }

    private (Mock<IExportDataProvider>, Mock<IExportFormatGenerator>) CreateMockProvidersWithRecordCount(
        int recordCount,
        ExportType exportType = ExportType.People,
        ReportOutputFormat outputFormat = ReportOutputFormat.Csv)
    {
        var mockProvider = new Mock<IExportDataProvider>();
        mockProvider.Setup(p => p.ExportType).Returns(exportType);
        mockProvider.Setup(p => p.GetAvailableFields()).Returns(new List<ExportFieldDto>
        {
            new() { FieldName = "FirstName", DisplayName = "First Name", DataType = "string", IsDefaultField = true },
            new() { FieldName = "LastName", DisplayName = "Last Name", DataType = "string", IsDefaultField = true },
            new() { FieldName = "Email", DisplayName = "Email", DataType = "string", IsDefaultField = true }
        });

        // Generate large dataset
        var data = new List<Dictionary<string, object?>>();
        for (int i = 0; i < recordCount; i++)
        {
            data.Add(new Dictionary<string, object?>
            {
                ["FirstName"] = $"FirstName{i}",
                ["LastName"] = $"LastName{i}",
                ["Email"] = $"person{i}@example.com"
            });
        }

        mockProvider.Setup(p => p.GetDataAsync(
            It.IsAny<List<string>?>(),
            It.IsAny<Dictionary<string, string>?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        var mockGenerator = new Mock<IExportFormatGenerator>();
        mockGenerator.Setup(g => g.OutputFormat).Returns(outputFormat);
        mockGenerator.Setup(g => g.GetMimeType()).Returns("text/csv");
        mockGenerator.Setup(g => g.GetFileExtension()).Returns(".csv");
        mockGenerator.Setup(g => g.GenerateAsync(
            It.IsAny<IReadOnlyList<Dictionary<string, object?>>>(),
            It.IsAny<List<string>>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());

        return (mockProvider, mockGenerator);
    }

    #region Large Dataset Performance Tests

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task ProcessExportJobAsync_1000Records_CompletesUnder1Second()
    {
        // Arrange
        const int recordCount = 1000;
        const int maxMilliseconds = 1000;

        var context = CreateInMemoryContext();
        var (mockProvider, mockGenerator) = CreateMockProvidersWithRecordCount(recordCount);
        var service = CreateService(context,
            exportProviders: new[] { mockProvider.Object },
            formatGenerators: new[] { mockGenerator.Object });

        context.ExportJobs.Add(new ExportJob
        {
            Id = 1,
            ExportType = ExportType.People,
            Status = ReportStatus.Pending,
            OutputFormat = ReportOutputFormat.Csv,
            Parameters = "{}",
            CreatedDateTime = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        _fileStorageServiceMock.Setup(s => s.StoreFileAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("storage-key-1");

        // Act
        var (_, metrics) = await PerformanceMeasurer.MeasureAsync(
            async () =>
            {
                await service.ProcessExportJobAsync(1);
                return true;
            },
            recordCount);

        // Assert
        // SYNC OK: Test verification
        var job = await context.ExportJobs.FirstAsync(j => j.Id == 1);
        job.Status.Should().Be(ReportStatus.Completed);

        PerformanceAssertions.AssertCompletedWithin(
            TimeSpan.FromMilliseconds(maxMilliseconds),
            TimeSpan.FromMilliseconds(metrics.ElapsedMs));
    }

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task ProcessExportJobAsync_10000Records_CompletesUnder5Seconds()
    {
        // Arrange
        const int recordCount = 10000;
        const int maxMilliseconds = 5000;

        var context = CreateInMemoryContext();
        var (mockProvider, mockGenerator) = CreateMockProvidersWithRecordCount(recordCount);
        var service = CreateService(context,
            exportProviders: new[] { mockProvider.Object },
            formatGenerators: new[] { mockGenerator.Object });

        context.ExportJobs.Add(new ExportJob
        {
            Id = 1,
            ExportType = ExportType.People,
            Status = ReportStatus.Pending,
            OutputFormat = ReportOutputFormat.Csv,
            Parameters = "{}",
            CreatedDateTime = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        _fileStorageServiceMock.Setup(s => s.StoreFileAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("storage-key-1");

        // Act
        var (_, metrics) = await PerformanceMeasurer.MeasureAsync(
            async () =>
            {
                await service.ProcessExportJobAsync(1);
                return true;
            },
            recordCount);

        // Assert
        // SYNC OK: Test verification
        var job = await context.ExportJobs.FirstAsync(j => j.Id == 1);
        job.Status.Should().Be(ReportStatus.Completed);

        PerformanceAssertions.AssertCompletedWithin(
            TimeSpan.FromMilliseconds(maxMilliseconds),
            TimeSpan.FromMilliseconds(metrics.ElapsedMs));
    }

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task ProcessExportJobAsync_100000Records_CompletesWithinReasonableTime()
    {
        // Arrange
        const int recordCount = 100000;
        const int maxMilliseconds = 30000; // 30 seconds for very large dataset

        var context = CreateInMemoryContext();
        var (mockProvider, mockGenerator) = CreateMockProvidersWithRecordCount(recordCount);
        var service = CreateService(context,
            exportProviders: new[] { mockProvider.Object },
            formatGenerators: new[] { mockGenerator.Object });

        context.ExportJobs.Add(new ExportJob
        {
            Id = 1,
            ExportType = ExportType.People,
            Status = ReportStatus.Pending,
            OutputFormat = ReportOutputFormat.Csv,
            Parameters = "{}",
            CreatedDateTime = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        _fileStorageServiceMock.Setup(s => s.StoreFileAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("storage-key-1");

        // Act
        var (_, metrics) = await PerformanceMeasurer.MeasureAsync(
            async () =>
            {
                await service.ProcessExportJobAsync(1);
                return true;
            },
            recordCount);

        // Assert
        // SYNC OK: Test verification
        var job = await context.ExportJobs.FirstAsync(j => j.Id == 1);
        job.Status.Should().Be(ReportStatus.Completed);

        PerformanceAssertions.AssertCompletedWithin(
            TimeSpan.FromMilliseconds(maxMilliseconds),
            TimeSpan.FromMilliseconds(metrics.ElapsedMs));

        // Log metrics for analysis
        var throughput = recordCount / (metrics.ElapsedMs / 1000.0);
        _loggerMock.Object.LogInformation(
            $"Processed {recordCount} records in {metrics.ElapsedMs}ms ({throughput:F0} records/sec, {metrics.MemoryUsedBytes / 1024.0 / 1024.0:F2}MB)");
    }

    #endregion

    #region Concurrent Export Tests

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task ProcessExportJobAsync_5ConcurrentExports_CompletesWithinReasonableTime()
    {
        // Arrange
        const int concurrentCount = 5;
        const int recordsPerExport = 1000;
        const int maxMillisecondsMultiplier = 3; // Allow 3x single export time for parallel overhead

        // Create shared database name for concurrent access
        var databaseName = Guid.NewGuid().ToString();

        // Measure single export time first
        var singleExportTime = await MeasureSingleExport(databaseName, recordsPerExport);
        var maxConcurrentMs = Math.Max(singleExportTime * maxMillisecondsMultiplier, 5000); // At least 5 seconds

        // Setup concurrent exports
        var context = CreateInMemoryContextWithName(databaseName);
        for (int i = 2; i <= concurrentCount + 1; i++) // Start at 2 since job 1 was used for timing
        {
            context.ExportJobs.Add(new ExportJob
            {
                Id = i,
                ExportType = ExportType.People,
                Status = ReportStatus.Pending,
                OutputFormat = ReportOutputFormat.Csv,
                Parameters = "{}",
                CreatedDateTime = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        _fileStorageServiceMock.Setup(s => s.StoreFileAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream s, string fn, string mt, CancellationToken ct) => $"storage-key-{Guid.NewGuid()}");

        // Act - Run concurrent exports with separate contexts
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task>();
        for (int i = 2; i <= concurrentCount + 1; i++)
        {
            int jobId = i;
            tasks.Add(Task.Run(async () =>
            {
                // Each task gets its own context for thread safety
                var taskContext = CreateInMemoryContextWithName(databaseName);
                var (mockProvider, mockGenerator) = CreateMockProvidersWithRecordCount(recordsPerExport);
                var taskService = CreateService(taskContext,
                    exportProviders: new[] { mockProvider.Object },
                    formatGenerators: new[] { mockGenerator.Object });

                await taskService.ProcessExportJobAsync(jobId);
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var verifyContext = CreateInMemoryContextWithName(databaseName);
        var completedJobs = await verifyContext.ExportJobs
            .Where(j => j.Id >= 2 && j.Status == ReportStatus.Completed)
            .ToListAsync();
        completedJobs.Should().HaveCount(concurrentCount, "all concurrent exports should complete successfully");

        stopwatch.ElapsedMilliseconds.Should().BeLessThanOrEqualTo(maxConcurrentMs,
            $"concurrent exports should complete within {maxMillisecondsMultiplier}x single export time");
    }

    private async Task<long> MeasureSingleExport(string databaseName, int recordCount)
    {
        var context = CreateInMemoryContextWithName(databaseName);
        var (mockProvider, mockGenerator) = CreateMockProvidersWithRecordCount(recordCount);

        _fileStorageServiceMock.Setup(s => s.StoreFileAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream s, string fn, string mt, CancellationToken ct) => $"storage-key-{Guid.NewGuid()}");

        var service = CreateService(context,
            exportProviders: new[] { mockProvider.Object },
            formatGenerators: new[] { mockGenerator.Object });

        context.ExportJobs.Add(new ExportJob
        {
            Id = 1,
            ExportType = ExportType.People,
            Status = ReportStatus.Pending,
            OutputFormat = ReportOutputFormat.Csv,
            Parameters = "{}",
            CreatedDateTime = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var stopwatch = Stopwatch.StartNew();
        await service.ProcessExportJobAsync(1);
        stopwatch.Stop();

        return stopwatch.ElapsedMilliseconds;
    }

    private IApplicationDbContext CreateInMemoryContextWithName(string databaseName)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .EnableSensitiveDataLogging()
            .Options;
        return new TestDbContext(options);
    }

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task ProcessExportJobAsync_10ConcurrentExports_AllCompleteSuccessfully()
    {
        // Arrange
        const int concurrentCount = 10;
        const int recordsPerExport = 500;
        const int maxMilliseconds = 10000; // 10 seconds for 10 concurrent exports

        // Create shared database name for concurrent access
        var databaseName = Guid.NewGuid().ToString();
        var context = CreateInMemoryContextWithName(databaseName);

        // Create multiple export jobs
        for (int i = 1; i <= concurrentCount; i++)
        {
            context.ExportJobs.Add(new ExportJob
            {
                Id = i,
                ExportType = ExportType.People,
                Status = ReportStatus.Pending,
                OutputFormat = ReportOutputFormat.Csv,
                Parameters = "{}",
                CreatedDateTime = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        _fileStorageServiceMock.Setup(s => s.StoreFileAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream s, string fn, string mt, CancellationToken ct) => $"storage-key-{Guid.NewGuid()}");

        // Act - Run concurrent exports with separate contexts
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task>();
        for (int i = 1; i <= concurrentCount; i++)
        {
            int jobId = i;
            tasks.Add(Task.Run(async () =>
            {
                // Each task gets its own context for thread safety
                var taskContext = CreateInMemoryContextWithName(databaseName);
                var (mockProvider, mockGenerator) = CreateMockProvidersWithRecordCount(recordsPerExport);
                var taskService = CreateService(taskContext,
                    exportProviders: new[] { mockProvider.Object },
                    formatGenerators: new[] { mockGenerator.Object });

                await taskService.ProcessExportJobAsync(jobId);
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var verifyContext = CreateInMemoryContextWithName(databaseName);
        var completedJobs = await verifyContext.ExportJobs
            .Where(j => j.Status == ReportStatus.Completed)
            .ToListAsync();
        completedJobs.Should().HaveCount(concurrentCount, "all concurrent exports should complete successfully");

        stopwatch.ElapsedMilliseconds.Should().BeLessThanOrEqualTo(maxMilliseconds,
            $"10 concurrent exports should complete within {maxMilliseconds}ms");

        // Verify no data corruption - each job should have unique output file
        var outputFileIds = completedJobs.Select(j => j.OutputFileId).ToList();
        outputFileIds.Should().OnlyHaveUniqueItems("each export should have its own output file");
    }

    #endregion

    #region Memory Usage Tests

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task ProcessExportJobAsync_1000Records_UsesReasonableMemory()
    {
        // Arrange
        const int recordCount = 1000;
        const long maxMemoryBytes = 10 * 1024 * 1024; // 10 MB

        var context = CreateInMemoryContext();
        var (mockProvider, mockGenerator) = CreateMockProvidersWithRecordCount(recordCount);
        var service = CreateService(context,
            exportProviders: new[] { mockProvider.Object },
            formatGenerators: new[] { mockGenerator.Object });

        context.ExportJobs.Add(new ExportJob
        {
            Id = 1,
            ExportType = ExportType.People,
            Status = ReportStatus.Pending,
            OutputFormat = ReportOutputFormat.Csv,
            Parameters = "{}",
            CreatedDateTime = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        _fileStorageServiceMock.Setup(s => s.StoreFileAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("storage-key-1");

        // Act
        var (_, metrics) = await PerformanceMeasurer.MeasureAsync(
            async () =>
            {
                await service.ProcessExportJobAsync(1);
                return true;
            },
            recordCount);

        // Assert
        PerformanceAssertions.AssertMemoryUsageLessThan(maxMemoryBytes, metrics.MemoryUsedBytes);
    }

    [Fact(Skip = "Memory measurement is unreliable in CI - GC.GetTotalMemory varies between runs")]
    public async Task ProcessExportJobAsync_10000Records_MemoryGrowthIsLinear()
    {
        // Arrange - Test that memory growth is approximately linear with record count
        const int smallRecordCount = 1000;
        const int largeRecordCount = 10000;

        var context1 = CreateInMemoryContext();
        var (mockProvider1, mockGenerator1) = CreateMockProvidersWithRecordCount(smallRecordCount);
        var service1 = CreateService(context1,
            exportProviders: new[] { mockProvider1.Object },
            formatGenerators: new[] { mockGenerator1.Object });

        context1.ExportJobs.Add(new ExportJob
        {
            Id = 1,
            ExportType = ExportType.People,
            Status = ReportStatus.Pending,
            OutputFormat = ReportOutputFormat.Csv,
            Parameters = "{}",
            CreatedDateTime = DateTime.UtcNow
        });
        await context1.SaveChangesAsync();

        _fileStorageServiceMock.Setup(s => s.StoreFileAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("storage-key-1");

        // Measure small dataset
        var (_, smallMetrics) = await PerformanceMeasurer.MeasureAsync(
            async () =>
            {
                await service1.ProcessExportJobAsync(1);
                return true;
            },
            smallRecordCount);

        // Setup large dataset test
        var context2 = CreateInMemoryContext();
        var (mockProvider2, mockGenerator2) = CreateMockProvidersWithRecordCount(largeRecordCount);
        var service2 = CreateService(context2,
            exportProviders: new[] { mockProvider2.Object },
            formatGenerators: new[] { mockGenerator2.Object });

        context2.ExportJobs.Add(new ExportJob
        {
            Id = 1,
            ExportType = ExportType.People,
            Status = ReportStatus.Pending,
            OutputFormat = ReportOutputFormat.Csv,
            Parameters = "{}",
            CreatedDateTime = DateTime.UtcNow
        });
        await context2.SaveChangesAsync();

        // Measure large dataset
        var (_, largeMetrics) = await PerformanceMeasurer.MeasureAsync(
            async () =>
            {
                await service2.ProcessExportJobAsync(1);
                return true;
            },
            largeRecordCount);

        // Assert - Memory growth should be roughly proportional (within 3x tolerance for overhead)
        var recordRatio = (double)largeRecordCount / smallRecordCount;
        var memoryRatio = (double)largeMetrics.MemoryUsedBytes / Math.Max(1, smallMetrics.MemoryUsedBytes);

        memoryRatio.Should().BeLessThan(recordRatio * 3,
            "memory growth should be roughly linear with record count");
    }

    [Fact(Skip = "Memory measurement is unreliable in CI - GC.GetTotalMemory varies between runs")]
    public async Task ProcessExportJobAsync_MultipleSequentialExports_NoMemoryLeak()
    {
        // Arrange - Run multiple exports sequentially and verify memory doesn't grow unbounded
        const int exportCount = 5;
        const int recordsPerExport = 1000;

        var memoryReadings = new List<long>();

        for (int i = 0; i < exportCount; i++)
        {
            var context = CreateInMemoryContext();
            var (mockProvider, mockGenerator) = CreateMockProvidersWithRecordCount(recordsPerExport);
            var service = CreateService(context,
                exportProviders: new[] { mockProvider.Object },
                formatGenerators: new[] { mockGenerator.Object });

            context.ExportJobs.Add(new ExportJob
            {
                Id = 1,
                ExportType = ExportType.People,
                Status = ReportStatus.Pending,
                OutputFormat = ReportOutputFormat.Csv,
                Parameters = "{}",
                CreatedDateTime = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            _fileStorageServiceMock.Setup(s => s.StoreFileAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync($"storage-key-{i}");

            // Act
            var (_, metrics) = await PerformanceMeasurer.MeasureAsync(
                async () =>
                {
                    await service.ProcessExportJobAsync(1);
                    return true;
                },
                recordsPerExport);

            memoryReadings.Add(metrics.MemoryUsedBytes);

            // Dispose context explicitly
            if (context is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        // Assert - Memory usage should not grow significantly across exports
        var firstMemory = memoryReadings.First();
        var lastMemory = memoryReadings.Last();

        // Allow 50% growth tolerance for GC variations, but no unbounded growth
        lastMemory.Should().BeLessThan((long)(firstMemory * 1.5),
            "sequential exports should not cause unbounded memory growth (potential memory leak)");
    }

    #endregion

    #region Test DbContext

    private class TestDbContext : DbContext, IApplicationDbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<Person> People { get; set; } = null!;
        public DbSet<PersonAlias> PersonAliases { get; set; } = null!;
        public DbSet<PhoneNumber> PhoneNumbers { get; set; } = null!;
        public DbSet<Group> Groups { get; set; } = null!;
        public DbSet<GroupType> GroupTypes { get; set; } = null!;
        public DbSet<GroupTypeRole> GroupTypeRoles { get; set; } = null!;
        public DbSet<GroupMember> GroupMembers { get; set; } = null!;
        public DbSet<GroupMemberRequest> GroupMemberRequests { get; set; } = null!;
        public DbSet<GroupSchedule> GroupSchedules { get; set; } = null!;
        public DbSet<Family> Families { get; set; } = null!;
        public DbSet<FamilyMember> FamilyMembers { get; set; } = null!;
        public DbSet<Campus> Campuses { get; set; } = null!;
        public DbSet<Location> Locations { get; set; } = null!;
        public DbSet<DefinedType> DefinedTypes { get; set; } = null!;
        public DbSet<DefinedValue> DefinedValues { get; set; } = null!;
        public DbSet<Schedule> Schedules { get; set; } = null!;
        public DbSet<Attendance> Attendances { get; set; } = null!;
        public DbSet<AttendanceOccurrence> AttendanceOccurrences { get; set; } = null!;
        public DbSet<AttendanceCode> AttendanceCodes { get; set; } = null!;
        public DbSet<Device> Devices { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<UserPreference> UserPreferences { get; set; } = null!;
        public DbSet<UserSession> UserSessions { get; set; } = null!;
        public DbSet<TwoFactorConfig> TwoFactorConfigs { get; set; } = null!;
        public DbSet<SupervisorSession> SupervisorSessions { get; set; } = null!;
        public DbSet<SupervisorAuditLog> SupervisorAuditLogs { get; set; } = null!;
        public DbSet<FollowUp> FollowUps { get; set; } = null!;
        public DbSet<PagerAssignment> PagerAssignments { get; set; } = null!;
        public DbSet<PagerMessage> PagerMessages { get; set; } = null!;
        public DbSet<AuthorizedPickup> AuthorizedPickups { get; set; } = null!;
        public DbSet<PickupLog> PickupLogs { get; set; } = null!;
        public DbSet<Communication> Communications { get; set; } = null!;
        public DbSet<CommunicationRecipient> CommunicationRecipients { get; set; } = null!;
        public DbSet<CommunicationTemplate> CommunicationTemplates { get; set; } = null!;
        public DbSet<CommunicationPreference> CommunicationPreferences { get; set; } = null!;
        public DbSet<BinaryFile> BinaryFiles { get; set; } = null!;
        public DbSet<ImportTemplate> ImportTemplates { get; set; } = null!;
        public DbSet<ImportJob> ImportJobs { get; set; } = null!;
        public DbSet<ReportDefinition> ReportDefinitions { get; set; } = null!;
        public DbSet<ReportRun> ReportRuns { get; set; } = null!;
        public DbSet<ReportSchedule> ReportSchedules { get; set; } = null!;
        public DbSet<ExportJob> ExportJobs { get; set; } = null!;
        public DbSet<SecurityRole> SecurityRoles { get; set; } = null!;
        public DbSet<SecurityClaim> SecurityClaims { get; set; } = null!;
        public DbSet<PersonSecurityRole> PersonSecurityRoles { get; set; } = null!;
        public DbSet<RoleSecurityClaim> RoleSecurityClaims { get; set; } = null!;
        public DbSet<Fund> Funds { get; set; } = null!;
        public DbSet<ContributionBatch> ContributionBatches { get; set; } = null!;
        public DbSet<Contribution> Contributions { get; set; } = null!;
        public DbSet<ContributionDetail> ContributionDetails { get; set; } = null!;
        public DbSet<ContributionStatement> ContributionStatements { get; set; } = null!;
        public DbSet<FinancialAuditLog> FinancialAuditLogs { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<PersonMergeHistory> PersonMergeHistories { get; set; } = null!;
        public DbSet<PersonDuplicateIgnore> PersonDuplicateIgnores { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<NotificationPreference> NotificationPreferences { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Location>()
                .HasOne(l => l.ParentLocation)
                .WithMany(l => l.ChildLocations)
                .HasForeignKey(l => l.ParentLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Location>()
                .HasOne(l => l.OverflowLocation)
                .WithMany()
                .HasForeignKey(l => l.OverflowLocationId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    #endregion
}
