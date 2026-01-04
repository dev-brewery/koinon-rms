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

namespace Koinon.Application.Tests.Services;

/// <summary>
/// Unit tests for DataExportService.
/// </summary>
public class DataExportServiceTests
{
    private readonly Mock<IBackgroundJobService> _backgroundJobServiceMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<DataExportService>> _loggerMock;

    private readonly string _exportJobIdKey = IdKeyHelper.Encode(1);

    public DataExportServiceTests()
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
        // Use provided or empty collections for testing
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

    private (Mock<IExportDataProvider>, Mock<IExportFormatGenerator>) CreateMockProvidersAndGenerators(
        ExportType exportType = ExportType.People,
        ReportOutputFormat outputFormat = ReportOutputFormat.Csv)
    {
        var mockProvider = new Mock<IExportDataProvider>();
        mockProvider.Setup(p => p.ExportType).Returns(exportType);
        mockProvider.Setup(p => p.GetAvailableFields()).Returns(new List<ExportFieldDto>
        {
            new() { FieldName = "Field1", DisplayName = "Field 1", DataType = "string", IsDefaultField = true }
        });
        mockProvider.Setup(p => p.GetDataAsync(
            It.IsAny<List<string>?>(),
            It.IsAny<Dictionary<string, string>?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dictionary<string, object?>>
            {
                new() { ["Field1"] = "Value1" }
            });

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

    #region GetExportJobsAsync Tests

    [Fact]
    public async Task GetExportJobsAsync_ReturnsPagedExportJobs()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        context.ExportJobs.Add(new ExportJob
        {
            Id = 1,
            ExportType = ExportType.People,
            Status = ReportStatus.Completed,
            OutputFormat = ReportOutputFormat.Csv,
            Parameters = "{}",
            CreatedDateTime = DateTime.UtcNow
        });
        context.ExportJobs.Add(new ExportJob
        {
            Id = 2,
            ExportType = ExportType.Groups,
            Status = ReportStatus.Pending,
            OutputFormat = ReportOutputFormat.Excel,
            Parameters = "{}",
            CreatedDateTime = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        _mapperMock.Setup(m => m.Map<List<ExportJobDto>>(It.IsAny<List<ExportJob>>()))
            .Returns((List<ExportJob> jobs) => jobs.Select(j => new ExportJobDto
            {
                IdKey = IdKeyHelper.Encode(j.Id),
                ExportType = j.ExportType,
                Status = j.Status,
                OutputFormat = j.OutputFormat,
                Parameters = j.Parameters,
                CreatedDateTime = j.CreatedDateTime
            }).ToList());

        // Act
        var result = await service.GetExportJobsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.First().ExportType.Should().Be(ExportType.Groups); // Most recent first
    }

    [Fact]
    public async Task GetExportJobsAsync_ReturnsEmptyWhenNoJobs()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        _mapperMock.Setup(m => m.Map<List<ExportJobDto>>(It.IsAny<List<ExportJob>>()))
            .Returns(new List<ExportJobDto>());

        // Act
        var result = await service.GetExportJobsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExportJobsAsync_SupportsPagination()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        for (int i = 1; i <= 30; i++)
        {
            context.ExportJobs.Add(new ExportJob
            {
                Id = i,
                ExportType = ExportType.People,
                Status = ReportStatus.Completed,
                OutputFormat = ReportOutputFormat.Csv,
                Parameters = "{}",
                CreatedDateTime = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await context.SaveChangesAsync();

        _mapperMock.Setup(m => m.Map<List<ExportJobDto>>(It.IsAny<List<ExportJob>>()))
            .Returns((List<ExportJob> jobs) => jobs.Select(j => new ExportJobDto
            {
                IdKey = IdKeyHelper.Encode(j.Id),
                ExportType = j.ExportType,
                Status = j.Status,
                OutputFormat = j.OutputFormat,
                Parameters = j.Parameters,
                CreatedDateTime = j.CreatedDateTime
            }).ToList());

        // Act
        var page1 = await service.GetExportJobsAsync(page: 1, pageSize: 10);
        var page2 = await service.GetExportJobsAsync(page: 2, pageSize: 10);

        // Assert
        page1.TotalCount.Should().Be(30);
        page1.Items.Should().HaveCount(10);
        page2.Items.Should().HaveCount(10);
        page1.Items.First().IdKey.Should().NotBe(page2.Items.First().IdKey);
    }

    #endregion

    #region GetExportJobAsync Tests

    [Fact]
    public async Task GetExportJobAsync_ReturnsExportJob_WhenFound()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        context.ExportJobs.Add(new ExportJob
        {
            Id = 1,
            ExportType = ExportType.People,
            Status = ReportStatus.Completed,
            OutputFormat = ReportOutputFormat.Csv,
            Parameters = "{}",
            CreatedDateTime = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        _mapperMock.Setup(m => m.Map<ExportJobDto>(It.IsAny<ExportJob>()))
            .Returns((ExportJob job) => new ExportJobDto
            {
                IdKey = IdKeyHelper.Encode(job.Id),
                ExportType = job.ExportType,
                Status = job.Status,
                OutputFormat = job.OutputFormat,
                Parameters = job.Parameters,
                CreatedDateTime = job.CreatedDateTime
            });

        // Act
        var result = await service.GetExportJobAsync(_exportJobIdKey);

        // Assert
        result.Should().NotBeNull();
        result!.IdKey.Should().Be(_exportJobIdKey);
        result.ExportType.Should().Be(ExportType.People);
    }

    [Fact]
    public async Task GetExportJobAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetExportJobAsync(_exportJobIdKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExportJobAsync_ReturnsNull_WhenInvalidIdKey()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetExportJobAsync("invalid-idkey");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region StartExportAsync Tests

    [Fact]
    public async Task StartExportAsync_CreatesExportJobAndQueuesBackgroundJob()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        var request = new StartExportRequest
        {
            ExportType = ExportType.People,
            OutputFormat = ReportOutputFormat.Csv,
            Fields = new List<string> { "FirstName", "LastName", "Email" },
            Filters = new Dictionary<string, string> { { "campus", "1" } }
        };

        _backgroundJobServiceMock.Setup(s => s.Enqueue<IDataExportService>(It.IsAny<System.Linq.Expressions.Expression<Action<IDataExportService>>>()))
            .Returns("job-123");

        _mapperMock.Setup(m => m.Map<ExportJobDto>(It.IsAny<ExportJob>()))
            .Returns((ExportJob job) => new ExportJobDto
            {
                IdKey = IdKeyHelper.Encode(job.Id),
                ExportType = job.ExportType,
                Status = job.Status,
                OutputFormat = job.OutputFormat,
                Parameters = job.Parameters,
                CreatedDateTime = job.CreatedDateTime
            });

        // Act
        var result = await service.StartExportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ExportType.Should().Be(ExportType.People);
        result.Value.Status.Should().Be(ReportStatus.Pending);
        result.Value.OutputFormat.Should().Be(ReportOutputFormat.Csv);

        // Verify background job was enqueued
        _backgroundJobServiceMock.Verify(
            s => s.Enqueue<IDataExportService>(It.IsAny<System.Linq.Expressions.Expression<Action<IDataExportService>>>()),
            Times.Once);

        // Verify export job was saved to database
        var savedJob = await context.ExportJobs.FirstOrDefaultAsync();
        savedJob.Should().NotBeNull();
        savedJob!.ExportType.Should().Be(ExportType.People);
    }

    [Fact]
    public async Task StartExportAsync_StoresParametersAsJson()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        var request = new StartExportRequest
        {
            ExportType = ExportType.Groups,
            OutputFormat = ReportOutputFormat.Excel,
            Fields = new List<string> { "Name", "GroupType" },
            Filters = new Dictionary<string, string> { { "isActive", "true" } }
        };

        _backgroundJobServiceMock.Setup(s => s.Enqueue<IDataExportService>(It.IsAny<System.Linq.Expressions.Expression<Action<IDataExportService>>>()))
            .Returns("job-456");

        _mapperMock.Setup(m => m.Map<ExportJobDto>(It.IsAny<ExportJob>()))
            .Returns((ExportJob job) => new ExportJobDto
            {
                IdKey = IdKeyHelper.Encode(job.Id),
                ExportType = job.ExportType,
                Status = job.Status,
                OutputFormat = job.OutputFormat,
                Parameters = job.Parameters,
                CreatedDateTime = job.CreatedDateTime
            });

        // Act
        var result = await service.StartExportAsync(request);

        // Assert
        var savedJob = await context.ExportJobs.FirstOrDefaultAsync();
        savedJob.Should().NotBeNull();
        savedJob!.Parameters.Should().Contain("fields");
        savedJob.Parameters.Should().Contain("filters");
    }

    #endregion

    #region ProcessExportJobAsync Tests

    [Fact]
    public async Task ProcessExportJobAsync_UpdatesStatusToProcessing()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var (mockProvider, mockGenerator) = CreateMockProvidersAndGenerators();
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
            .ReturnsAsync("storage-key-123");

        // Act
        await service.ProcessExportJobAsync(1);

        // Assert
        var job = await context.ExportJobs.FirstAsync(j => j.Id == 1);
        job.Status.Should().Be(ReportStatus.Completed);
        job.StartedAt.Should().NotBeNull();
        job.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessExportJobAsync_CreatesOutputFile()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var (mockProvider, mockGenerator) = CreateMockProvidersAndGenerators();
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
            .ReturnsAsync("storage-key-123");

        // Act
        await service.ProcessExportJobAsync(1);

        // Assert
        var job = await context.ExportJobs.Include(j => j.OutputFile).FirstAsync(j => j.Id == 1);
        job.OutputFileId.Should().NotBeNull();

        var outputFile = await context.BinaryFiles.FirstOrDefaultAsync(f => f.Id == job.OutputFileId);
        outputFile.Should().NotBeNull();
        outputFile!.StorageKey.Should().Be("storage-key-123");
        outputFile.MimeType.Should().Be("text/csv");
    }

    [Fact]
    public async Task ProcessExportJobAsync_HandlesFailureGracefully()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var (mockProvider, mockGenerator) = CreateMockProvidersAndGenerators();
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
            .ThrowsAsync(new InvalidOperationException("Storage service unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.ProcessExportJobAsync(1));

        var job = await context.ExportJobs.FirstAsync(j => j.Id == 1);
        job.Status.Should().Be(ReportStatus.Failed);
        job.ErrorMessage.Should().Be("Storage service unavailable");
        job.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessExportJobAsync_LogsError_WhenJobNotFound()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        await service.ProcessExportJobAsync(999);

        // Assert - verify logger was called (job not found is logged but doesn't throw)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region DownloadExportAsync Tests

    [Fact]
    public async Task DownloadExportAsync_ReturnsFileStream_WhenFound()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        var binaryFile = new BinaryFile
        {
            Id = 10,
            FileName = "export.csv",
            MimeType = "text/csv",
            StorageKey = "storage-key-123",
            FileSizeBytes = 1024,
            CreatedDateTime = DateTime.UtcNow
        };
        context.BinaryFiles.Add(binaryFile);

        context.ExportJobs.Add(new ExportJob
        {
            Id = 1,
            ExportType = ExportType.People,
            Status = ReportStatus.Completed,
            OutputFormat = ReportOutputFormat.Csv,
            Parameters = "{}",
            OutputFileId = 10,
            CreatedDateTime = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var fileStream = new MemoryStream();
        _fileStorageServiceMock.Setup(s => s.GetFileAsync("storage-key-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileStream);

        // Act
        var result = await service.DownloadExportAsync(_exportJobIdKey);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Stream.Should().BeSameAs(fileStream);
        result.Value.FileName.Should().Be("export.csv");
        result.Value.MimeType.Should().Be("text/csv");
    }

    [Fact]
    public async Task DownloadExportAsync_ReturnsNull_WhenJobNotFound()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = await service.DownloadExportAsync(_exportJobIdKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DownloadExportAsync_ReturnsNull_WhenOutputFileNotSet()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        context.ExportJobs.Add(new ExportJob
        {
            Id = 1,
            ExportType = ExportType.People,
            Status = ReportStatus.Pending,
            OutputFormat = ReportOutputFormat.Csv,
            Parameters = "{}",
            OutputFileId = null,
            CreatedDateTime = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var result = await service.DownloadExportAsync(_exportJobIdKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DownloadExportAsync_ReturnsNull_WhenStorageServiceReturnsNull()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        var binaryFile = new BinaryFile
        {
            Id = 10,
            FileName = "export.csv",
            MimeType = "text/csv",
            StorageKey = "storage-key-123",
            FileSizeBytes = 1024,
            CreatedDateTime = DateTime.UtcNow
        };
        context.BinaryFiles.Add(binaryFile);

        context.ExportJobs.Add(new ExportJob
        {
            Id = 1,
            ExportType = ExportType.People,
            Status = ReportStatus.Completed,
            OutputFormat = ReportOutputFormat.Csv,
            Parameters = "{}",
            OutputFileId = 10,
            CreatedDateTime = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        _fileStorageServiceMock.Setup(s => s.GetFileAsync("storage-key-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        // Act
        var result = await service.DownloadExportAsync(_exportJobIdKey);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAvailableFields Tests

    [Fact]
    public void GetAvailableFields_ReturnsPeopleFields_WhenExportTypePeople()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var mockProvider = new Mock<IExportDataProvider>();
        mockProvider.Setup(p => p.ExportType).Returns(ExportType.People);
        mockProvider.Setup(p => p.GetAvailableFields()).Returns(new List<ExportFieldDto>
        {
            new() { FieldName = "FirstName", DisplayName = "First Name", DataType = "string", IsDefaultField = true },
            new() { FieldName = "LastName", DisplayName = "Last Name", DataType = "string", IsDefaultField = true },
            new() { FieldName = "Email", DisplayName = "Email", DataType = "string", IsDefaultField = true }
        });
        var service = CreateService(context, exportProviders: new[] { mockProvider.Object });

        // Act
        var result = service.GetAvailableFields(ExportType.People);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(f => f.FieldName == "FirstName");
        result.Should().Contain(f => f.FieldName == "LastName");
        result.Should().Contain(f => f.FieldName == "Email");
        result.Should().Contain(f => f.IsDefaultField);
    }

    [Fact]
    public void GetAvailableFields_ReturnsGroupsFields_WhenExportTypeGroups()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var mockProvider = new Mock<IExportDataProvider>();
        mockProvider.Setup(p => p.ExportType).Returns(ExportType.Groups);
        mockProvider.Setup(p => p.GetAvailableFields()).Returns(new List<ExportFieldDto>
        {
            new() { FieldName = "Name", DisplayName = "Group Name", DataType = "string", IsDefaultField = true },
            new() { FieldName = "GroupType", DisplayName = "Group Type", DataType = "string", IsDefaultField = true }
        });
        var service = CreateService(context, exportProviders: new[] { mockProvider.Object });

        // Act
        var result = service.GetAvailableFields(ExportType.Groups);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(f => f.FieldName == "Name");
        result.Should().Contain(f => f.FieldName == "GroupType");
    }

    [Fact]
    public void GetAvailableFields_ReturnsEmptyList_WhenExportTypeCustom()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = service.GetAvailableFields(ExportType.Custom);

        // Assert
        result.Should().BeEmpty();
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
