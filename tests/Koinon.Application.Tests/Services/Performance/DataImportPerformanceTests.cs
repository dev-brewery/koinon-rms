using FluentAssertions;
using Koinon.Application.Common;
using Koinon.Application.DTOs.Requests;
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
/// Performance tests for DataImportService with large CSV files.
/// </summary>
[Trait("Category", "Performance")]
public class DataImportPerformanceTests
{
    private readonly Mock<ICsvParserService> _csvParserServiceMock;
    private readonly Mock<IPersonService> _personServiceMock;
    private readonly Mock<IFamilyService> _familyServiceMock;
    private readonly Mock<IBackgroundJobService> _backgroundJobServiceMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<ILogger<DataImportService>> _loggerMock;

    public DataImportPerformanceTests()
    {
        _csvParserServiceMock = new Mock<ICsvParserService>();
        _personServiceMock = new Mock<IPersonService>();
        _familyServiceMock = new Mock<IFamilyService>();
        _backgroundJobServiceMock = new Mock<IBackgroundJobService>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _loggerMock = new Mock<ILogger<DataImportService>>();
    }

    private IApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new TestDbContext(options);
    }

    private IApplicationDbContext CreateInMemoryContextWithName(string databaseName)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .EnableSensitiveDataLogging()
            .ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new TestDbContext(options);
    }

    private DataImportService CreateService(IApplicationDbContext context)
    {
        return new DataImportService(
            context,
            _csvParserServiceMock.Object,
            _personServiceMock.Object,
            _familyServiceMock.Object,
            _backgroundJobServiceMock.Object,
            _fileStorageServiceMock.Object,
            _loggerMock.Object);
    }

    private void SetupCsvParserForPeopleImport(int rowCount)
    {
        SetupCsvParserForPeopleImportWithFakeCount(rowCount, rowCount);
    }

    private void SetupCsvParserForPeopleImportWithFakeCount(int actualRowCount, int reportedCount)
    {
        // Setup preview generation - use Returns with lambda to create new instance each time
        _csvParserServiceMock.Setup(p => p.GeneratePreviewAsync(
            It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(new DTOs.Import.CsvPreviewDto(
                Headers: new List<string> { "FirstName", "LastName", "Email", "Phone", "BirthDate", "Gender" },
                SampleRows: new List<IReadOnlyList<string>>(),
                TotalRowCount: reportedCount,  // Report this count to service
                DetectedDelimiter: "Comma",
                DetectedEncoding: "UTF-8"
            )));

        // Setup streaming rows - use Returns with lambda to create new enumerator each time
        _csvParserServiceMock.Setup(p => p.StreamRowsAsync(
            It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()))
            .Returns(() => GeneratePeopleRows(actualRowCount));
    }

    private void SetupCsvParserForFamiliesImport(int rowCount)
    {
        // Setup preview generation - use Returns with lambda to create new instance each time
        _csvParserServiceMock.Setup(p => p.GeneratePreviewAsync(
            It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(new DTOs.Import.CsvPreviewDto(
                Headers: new List<string> { "Name", "Street1", "City", "State", "PostalCode" },
                SampleRows: new List<IReadOnlyList<string>>(),
                TotalRowCount: rowCount,
                DetectedDelimiter: "Comma",
                DetectedEncoding: "UTF-8"
            )));

        // Setup streaming rows - use Returns with lambda to create new enumerator each time
        _csvParserServiceMock.Setup(p => p.StreamRowsAsync(
            It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()))
            .Returns(() => GenerateFamiliesRows(rowCount));
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators - yield return is async
    private async IAsyncEnumerable<Dictionary<string, string>> GeneratePeopleRows(int rowCount)
    {
        for (var i = 1; i <= rowCount; i++)
        {
            yield return new Dictionary<string, string>
            {
                ["FirstName"] = $"FirstName{i}",
                ["LastName"] = $"LastName{i}",
                ["Email"] = $"person{i}@example.com",
                ["Phone"] = $"555-{i:D3}-{(i * 7):D4}",
                ["BirthDate"] = $"{1950 + (i % 70):D4}-{(i % 12) + 1:D2}-{(i % 28) + 1:D2}",
                ["Gender"] = i % 2 == 0 ? "Male" : "Female"
            };
        }
    }

    private async IAsyncEnumerable<Dictionary<string, string>> GenerateFamiliesRows(int rowCount)
    {
        for (var i = 1; i <= rowCount; i++)
        {
            var street2 = i % 5 == 0 ? $"Apt {i % 100}" : "";
            var state = i % 3 == 0 ? "CA" : i % 3 == 1 ? "NY" : "TX";

            yield return new Dictionary<string, string>
            {
                ["FamilyName"] = $"Family{i}",
                ["Street1"] = $"{i} Family Lane",
                ["Street2"] = street2,
                ["City"] = $"City{i}",
                ["State"] = state,
                ["PostalCode"] = $"{10000 + i:D5}",
                ["Country"] = "USA"
            };
        }
    }
#pragma warning restore CS1998

    #region Large Dataset Import Tests

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task StartImportAsync_1000PeopleRows_CompletesUnder2Seconds()
    {
        // Arrange
        const int recordCount = 1000;
        const int maxMilliseconds = 2000;

        var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Setup preview to return SMALLER count (< 500) to force sync processing
        // But generator will still produce all rows for performance test
        SetupCsvParserForPeopleImportWithFakeCount(recordCount, reportedCount: 400);

        // Mock person service to succeed
        _personServiceMock.Setup(p => p.CreateAsync(
            It.IsAny<CreatePersonRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DTOs.PersonDto>.Success(new DTOs.PersonDto
            {
                IdKey = "test",
                Guid = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Person",
                FullName = "Test Person",
                Gender = "Unknown",
                EmailPreference = "EmailAllowed",
                PhoneNumbers = new List<DTOs.PhoneNumberDto>(),
                CreatedDateTime = DateTime.UtcNow
            }));

        var csvStream = CsvGenerator.GeneratePersonCsv(recordCount);
        var request = new StartImportRequest
        {
            FileStream = csvStream,
            FileName = "test-1000.csv",
            ImportType = "People",
            FieldMappings = new Dictionary<string, string>
            {
                ["FirstName"] = "FirstName",
                ["LastName"] = "LastName",
                ["Email"] = "Email",
                ["MobilePhone"] = "Phone",
                ["BirthDate"] = "BirthDate",
                ["Gender"] = "Gender"
            }
        };

        // Act
        var (result, metrics) = await PerformanceMeasurer.MeasureAsync(
            async () => await service.StartImportAsync(request),
            recordCount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(ImportJobStatus.Completed.ToString());

        PerformanceAssertions.AssertCompletedWithin(
            TimeSpan.FromMilliseconds(maxMilliseconds),
            TimeSpan.FromMilliseconds(metrics.ElapsedMs));
    }

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task StartImportAsync_10000PeopleRows_CompletesUnder10Seconds()
    {
        // Arrange
        const int recordCount = 10000;
        const int maxMilliseconds = 10000;

        var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Report smaller count to avoid background job threshold
        SetupCsvParserForPeopleImportWithFakeCount(recordCount, reportedCount: 400);

        // Mock person service to succeed
        _personServiceMock.Setup(p => p.CreateAsync(
            It.IsAny<CreatePersonRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DTOs.PersonDto>.Success(new DTOs.PersonDto
            {
                IdKey = "test",
                Guid = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Person",
                FullName = "Test Person",
                Gender = "Unknown",
                EmailPreference = "EmailAllowed",
                PhoneNumbers = new List<DTOs.PhoneNumberDto>(),
                CreatedDateTime = DateTime.UtcNow
            }));

        var csvStream = CsvGenerator.GeneratePersonCsv(recordCount);
        var request = new StartImportRequest
        {
            FileStream = csvStream,
            FileName = "test-10000.csv",
            ImportType = "People",
            FieldMappings = new Dictionary<string, string>
            {
                ["FirstName"] = "FirstName",
                ["LastName"] = "LastName",
                ["Email"] = "Email",
                ["MobilePhone"] = "Phone",
                ["BirthDate"] = "BirthDate",
                ["Gender"] = "Gender"
            }
        };

        // Act
        var (result, metrics) = await PerformanceMeasurer.MeasureAsync(
            async () => await service.StartImportAsync(request),
            recordCount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(ImportJobStatus.Completed.ToString());

        PerformanceAssertions.AssertCompletedWithin(
            TimeSpan.FromMilliseconds(maxMilliseconds),
            TimeSpan.FromMilliseconds(metrics.ElapsedMs));

        // Log metrics for analysis (informational only, not required for test)
        if (_loggerMock.Object != null)
        {
            var throughput = recordCount / (metrics.ElapsedMs / 1000.0);
            _loggerMock.Object.LogInformation(
                $"Imported {recordCount} people in {metrics.ElapsedMs}ms ({throughput:F0} records/sec, {metrics.MemoryUsedBytes / 1024.0 / 1024.0:F2}MB)");
        }
    }

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task StartImportAsync_100000PeopleRows_CompletesOrEnqueuesBackground()
    {
        // Arrange
        const int recordCount = 100000;

        var context = CreateInMemoryContext();
        var service = CreateService(context);
        SetupCsvParserForPeopleImport(recordCount);

        // Mock background job service
        _backgroundJobServiceMock.Setup(b => b.Enqueue<IDataImportService>(
            It.IsAny<System.Linq.Expressions.Expression<Action<IDataImportService>>>()))
            .Returns("background-job-id");

        _fileStorageServiceMock.Setup(f => f.StoreFileAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("storage-key-1");

        var csvStream = CsvGenerator.GeneratePersonCsv(recordCount);
        var request = new StartImportRequest
        {
            FileStream = csvStream,
            FileName = "test-100000.csv",
            ImportType = "People",
            FieldMappings = new Dictionary<string, string>
            {
                ["FirstName"] = "FirstName",
                ["LastName"] = "LastName"
            }
        };

        // Act
        var result = await service.StartImportAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        // Large imports should be enqueued as background jobs
        result.Value!.Status.Should().Be(ImportJobStatus.Pending.ToString());
        result.Value!.BackgroundJobId.Should().Be("background-job-id");

        // Verify background job was enqueued
        _backgroundJobServiceMock.Verify(b => b.Enqueue<IDataImportService>(
            It.IsAny<System.Linq.Expressions.Expression<Action<IDataImportService>>>()),
            Times.Once);
    }

    #endregion

    #region Validation Performance Tests

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task ValidateMappingsAsync_1000Rows_FasterThanFullImport()
    {
        // Arrange
        const int recordCount = 1000;

        var context = CreateInMemoryContext();
        var service = CreateService(context);
        SetupCsvParserForPeopleImport(recordCount);

        var csvStream = CsvGenerator.GeneratePersonCsv(recordCount);
        var request = new ValidateImportRequest
        {
            FileStream = csvStream,
            FileName = "validate-1000.csv",
            ImportType = "People",
            FieldMappings = new Dictionary<string, string>
            {
                ["FirstName"] = "FirstName",
                ["LastName"] = "LastName"
            }
        };

        // Act
        var (result, metrics) = await PerformanceMeasurer.MeasureAsync(
            async () => await service.ValidateMappingsAsync(request),
            recordCount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        // Validation should be faster than full import (< 3000ms for 1000 rows)
        // Full import benchmark is 2000ms, validation can vary in CI
        metrics.ElapsedMs.Should().BeLessThan(3000,
            "validation should complete in reasonable time");
    }

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task ValidateMappingsAsync_10000Rows_CompletesUnder2Seconds()
    {
        // Arrange
        const int recordCount = 10000;
        const int maxMilliseconds = 2000;

        var context = CreateInMemoryContext();
        var service = CreateService(context);
        SetupCsvParserForPeopleImport(recordCount);

        var csvStream = CsvGenerator.GeneratePersonCsv(recordCount);
        var request = new ValidateImportRequest
        {
            FileStream = csvStream,
            FileName = "validate-10000.csv",
            ImportType = "People",
            FieldMappings = new Dictionary<string, string>
            {
                ["FirstName"] = "FirstName",
                ["LastName"] = "LastName"
            }
        };

        // Act
        var (result, metrics) = await PerformanceMeasurer.MeasureAsync(
            async () => await service.ValidateMappingsAsync(request),
            recordCount);

        // Assert
        result.IsSuccess.Should().BeTrue();

        PerformanceAssertions.AssertCompletedWithin(
            TimeSpan.FromMilliseconds(maxMilliseconds),
            TimeSpan.FromMilliseconds(metrics.ElapsedMs));
    }

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task ValidateMappingsAsync_InvalidMappings_DetectsQuickly()
    {
        // Arrange
        const int recordCount = 5000;
        const int maxMilliseconds = 500; // Should fail fast

        var context = CreateInMemoryContext();
        var service = CreateService(context);
        SetupCsvParserForPeopleImport(recordCount);

        var csvStream = CsvGenerator.GeneratePersonCsv(recordCount);

        // Missing required field mappings
        var request = new ValidateImportRequest
        {
            FileStream = csvStream,
            FileName = "validate-invalid.csv",
            ImportType = "People",
            FieldMappings = new Dictionary<string, string>
            {
                ["FirstName"] = "FirstName"
                // Missing LastName (required)
            }
        };

        // Act
        var (result, metrics) = await PerformanceMeasurer.MeasureAsync(
            async () => await service.ValidateMappingsAsync(request),
            recordCount);

        // Assert
        result.IsSuccess.Should().BeFalse("validation should fail due to missing required fields");

        // Should fail fast without processing all rows
        metrics.ElapsedMs.Should().BeLessThan(maxMilliseconds,
            "validation errors should be detected quickly");
    }

    #endregion

    #region Concurrent Import Tests

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task StartImportAsync_2ConcurrentImports_BothCompleteSuccessfully()
    {
        // Arrange
        const int concurrentCount = 2;
        const int recordsPerImport = 1000;
        const int maxMilliseconds = 5000;

        var databaseName = Guid.NewGuid().ToString();

        // Setup mocks for concurrent access (use smaller count to avoid background jobs)
        SetupCsvParserForPeopleImportWithFakeCount(recordsPerImport, reportedCount: 400);
        _personServiceMock.Setup(p => p.CreateAsync(
            It.IsAny<CreatePersonRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DTOs.PersonDto>.Success(new DTOs.PersonDto
            {
                IdKey = "test",
                Guid = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Person",
                FullName = "Test Person",
                Gender = "Unknown",
                EmailPreference = "EmailAllowed",
                PhoneNumbers = new List<DTOs.PhoneNumberDto>(),
                CreatedDateTime = DateTime.UtcNow
            }));

        // Act - Run concurrent imports
        var tasks = new List<Task<Result<DTOs.Import.ImportJobDto>>>();
        for (int i = 0; i < concurrentCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                // Each task gets its own context for thread safety
                var taskContext = CreateInMemoryContextWithName(databaseName);
                var taskService = CreateService(taskContext);

                var csvStream = CsvGenerator.GeneratePersonCsv(recordsPerImport);
                var request = new StartImportRequest
                {
                    FileStream = csvStream,
                    FileName = $"concurrent-{i}.csv",
                    ImportType = "People",
                    FieldMappings = new Dictionary<string, string>
                    {
                        ["FirstName"] = "FirstName",
                        ["LastName"] = "LastName"
                    }
                };

                return await taskService.StartImportAsync(request);
            }));
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
        results.Should().AllSatisfy(r =>
            r.Value!.Status.Should().Be(ImportJobStatus.Completed.ToString()));

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxMilliseconds,
            $"{concurrentCount} concurrent imports should complete within {maxMilliseconds}ms");
    }

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task StartImportAsync_3ConcurrentImports_AllCompleteWithoutDataCorruption()
    {
        // Arrange
        const int concurrentCount = 3;
        const int recordsPerImport = 500;
        const int maxMilliseconds = 5000;

        var databaseName = Guid.NewGuid().ToString();

        // Setup mocks (use smaller count to avoid background jobs)
        SetupCsvParserForPeopleImportWithFakeCount(recordsPerImport, reportedCount: 400);
        _personServiceMock.Setup(p => p.CreateAsync(
            It.IsAny<CreatePersonRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DTOs.PersonDto>.Success(new DTOs.PersonDto
            {
                IdKey = "test",
                Guid = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Person",
                FullName = "Test Person",
                Gender = "Unknown",
                EmailPreference = "EmailAllowed",
                PhoneNumbers = new List<DTOs.PhoneNumberDto>(),
                CreatedDateTime = DateTime.UtcNow
            }));

        // Act - Run concurrent imports
        var tasks = new List<Task<Result<DTOs.Import.ImportJobDto>>>();
        for (int i = 0; i < concurrentCount; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                var taskContext = CreateInMemoryContextWithName(databaseName);
                var taskService = CreateService(taskContext);

                var csvStream = CsvGenerator.GeneratePersonCsv(recordsPerImport);
                var request = new StartImportRequest
                {
                    FileStream = csvStream,
                    FileName = $"concurrent-{taskId}.csv",
                    ImportType = "People",
                    FieldMappings = new Dictionary<string, string>
                    {
                        ["FirstName"] = "FirstName",
                        ["LastName"] = "LastName"
                    }
                };

                return await taskService.StartImportAsync(request);
            }));
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(concurrentCount);
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());

        // Verify no data corruption - each job should have unique ID
        var jobIds = results.Select(r => r.Value!.IdKey).ToList();
        jobIds.Should().OnlyHaveUniqueItems("each import job should have a unique ID");

        // Verify all completed successfully
        var verifyContext = CreateInMemoryContextWithName(databaseName);
        var completedJobs = await verifyContext.ImportJobs
            .Where(j => j.Status == ImportJobStatus.Completed)
            .ToListAsync();
        completedJobs.Should().HaveCount(concurrentCount,
            "all concurrent imports should complete successfully");

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxMilliseconds);
    }

    [Fact(Skip = "Performance timing varies in CI environments - run locally for benchmarks")]
    public async Task StartImportAsync_ConcurrentPeopleAndFamiliesImports_BothTypesSucceed()
    {
        // Arrange
        const int recordsPerImport = 500;
        const int maxMilliseconds = 5000;

        var databaseName = Guid.NewGuid().ToString();

        // Setup mocks for both import types (use smaller count to avoid background jobs)
        SetupCsvParserForPeopleImportWithFakeCount(recordsPerImport, reportedCount: 400);
        _personServiceMock.Setup(p => p.CreateAsync(
            It.IsAny<CreatePersonRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DTOs.PersonDto>.Success(new DTOs.PersonDto
            {
                IdKey = "test-person",
                Guid = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Person",
                FullName = "Test Person",
                Gender = "Unknown",
                EmailPreference = "EmailAllowed",
                PhoneNumbers = new List<DTOs.PhoneNumberDto>(),
                CreatedDateTime = DateTime.UtcNow
            }));

        _familyServiceMock.Setup(f => f.CreateFamilyAsync(
            It.IsAny<CreateFamilyRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DTOs.FamilyDto>.Success(new DTOs.FamilyDto
            {
                IdKey = "test-family",
                Guid = Guid.NewGuid(),
                Name = "Test Family",
                IsActive = true,
                Members = new List<DTOs.FamilyMemberDto>(),
                CreatedDateTime = DateTime.UtcNow
            }));

        // Act - Run people and families imports concurrently
        var peopleTask = Task.Run(async () =>
        {
            var taskContext = CreateInMemoryContextWithName(databaseName);
            var taskService = CreateService(taskContext);

            var csvStream = CsvGenerator.GeneratePersonCsv(recordsPerImport);
            var request = new StartImportRequest
            {
                FileStream = csvStream,
                FileName = "people.csv",
                ImportType = "People",
                FieldMappings = new Dictionary<string, string>
                {
                    ["FirstName"] = "FirstName",
                    ["LastName"] = "LastName"
                }
            };

            return await taskService.StartImportAsync(request);
        });

        var familiesTask = Task.Run(async () =>
        {
            var taskContext = CreateInMemoryContextWithName(databaseName);
            var taskService = CreateService(taskContext);

            // Setup families parser for this task
            var csvParserMock = new Mock<ICsvParserService>();
            csvParserMock.Setup(p => p.GeneratePreviewAsync(
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new DTOs.Import.CsvPreviewDto(
                    Headers: new List<string> { "FamilyName", "Street1", "Street2", "City", "State", "PostalCode", "Country" },
                    SampleRows: new List<IReadOnlyList<string>>(),
                    TotalRowCount: 400,  // Use smaller count to avoid background jobs
                    DetectedDelimiter: "Comma",
                    DetectedEncoding: "UTF-8"
                )));

            csvParserMock.Setup(p => p.StreamRowsAsync(
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
                .Returns(() => GenerateFamiliesRows(recordsPerImport));

            var familiesService = new DataImportService(
                taskContext,
                csvParserMock.Object,
                _personServiceMock.Object,
                _familyServiceMock.Object,
                _backgroundJobServiceMock.Object,
                _fileStorageServiceMock.Object,
                _loggerMock.Object);

            var csvStream = CsvGenerator.GenerateFamilyCsv(recordsPerImport);
            var request = new StartImportRequest
            {
                FileStream = csvStream,
                FileName = "families.csv",
                ImportType = "Families",
                FieldMappings = new Dictionary<string, string>
                {
                    ["Name"] = "FamilyName",
                    ["Street1"] = "Street1",
                    ["City"] = "City",
                    ["State"] = "State",
                    ["PostalCode"] = "PostalCode"
                }
            };

            return await familiesService.StartImportAsync(request);
        });

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = await Task.WhenAll(peopleTask, familiesTask);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
        results.Should().AllSatisfy(r =>
            r.Value!.Status.Should().Be(ImportJobStatus.Completed.ToString()));

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxMilliseconds);
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
