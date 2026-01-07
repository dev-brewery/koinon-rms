using AutoMapper;
using FluentAssertions;
using Koinon.Application.Common;
using Koinon.Application.DTOs.Reports;
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
/// Unit tests for ReportService.
/// </summary>
public class ReportServiceTests
{
    private readonly Mock<IBackgroundJobService> _backgroundJobServiceMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<ReportService>> _loggerMock;

    private readonly string _definitionIdKey = IdKeyHelper.Encode(1);
    private readonly string _runIdKey = IdKeyHelper.Encode(100);

    public ReportServiceTests()
    {
        _backgroundJobServiceMock = new Mock<IBackgroundJobService>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<ReportService>>();
    }

    private IApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }

    private ReportService CreateService(IApplicationDbContext context)
    {
        return new ReportService(
            context,
            _backgroundJobServiceMock.Object,
            _fileStorageServiceMock.Object,
            _serviceProviderMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    #region GetDefinitionsAsync Tests

    [Fact]
    public async Task GetDefinitionsAsync_ReturnsPagedDefinitions()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        context.ReportDefinitions.Add(new ReportDefinition
        {
            Id = 1,
            Name = "Attendance Report",
            IsActive = true,
            ReportType = ReportType.AttendanceSummary,
            ParameterSchema = "{}",
            OutputFormat = ReportOutputFormat.Pdf
        });
        context.ReportDefinitions.Add(new ReportDefinition
        {
            Id = 2,
            Name = "Giving Report",
            IsActive = true,
            ReportType = ReportType.GivingSummary,
            ParameterSchema = "{}",
            OutputFormat = ReportOutputFormat.Excel
        });
        await context.SaveChangesAsync();

        _mapperMock.Setup(m => m.Map<List<ReportDefinitionDto>>(It.IsAny<List<ReportDefinition>>()))
            .Returns((List<ReportDefinition> defs) => defs.Select(d => new ReportDefinitionDto
            {
                IdKey = IdKeyHelper.Encode(d.Id),
                Name = d.Name,
                IsActive = d.IsActive,
                IsSystem = d.IsSystem,
                ReportType = d.ReportType,
                OutputFormat = d.OutputFormat
            }).ToList());

        // Act
        var result = await service.GetDefinitionsAsync(includeInactive: false);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    #endregion

    #region GetDefinitionAsync Tests

    [Fact]
    public async Task GetDefinitionAsync_WithValidIdKey_ReturnsDefinition()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        var definition = new ReportDefinition
        {
            Id = 1,
            Name = "Test Report",
            IsActive = true,
            ReportType = ReportType.AttendanceSummary,
            ParameterSchema = "{}",
            OutputFormat = ReportOutputFormat.Pdf
        };
        context.ReportDefinitions.Add(definition);
        await context.SaveChangesAsync();

        _mapperMock.Setup(m => m.Map<ReportDefinitionDto>(It.IsAny<ReportDefinition>()))
            .Returns(new ReportDefinitionDto
            {
                IdKey = _definitionIdKey,
                Name = definition.Name,
                IsActive = definition.IsActive,
                IsSystem = definition.IsSystem,
                ReportType = definition.ReportType,
                OutputFormat = definition.OutputFormat
            });

        // Act
        var result = await service.GetDefinitionAsync(_definitionIdKey);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Report");
    }

    [Fact]
    public async Task GetDefinitionAsync_WithNotFoundId_ReturnsNull()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetDefinitionAsync(_definitionIdKey);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateDefinitionAsync Tests

    [Fact]
    public async Task CreateDefinitionAsync_WithValidRequest_CreatesDefinition()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        var request = new CreateReportDefinitionRequest
        {
            Name = "New Report",
            Description = "Test Description",
            ReportType = ReportType.AttendanceSummary,
            ParameterSchema = "{}",
            OutputFormat = ReportOutputFormat.Pdf
        };

        _mapperMock.Setup(m => m.Map<ReportDefinition>(It.IsAny<CreateReportDefinitionRequest>()))
            .Returns(new ReportDefinition
            {
                Name = request.Name,
                Description = request.Description,
                ReportType = request.ReportType,
                ParameterSchema = request.ParameterSchema ?? "{}",
                OutputFormat = request.OutputFormat,
                IsActive = true
            });

        _mapperMock.Setup(m => m.Map<ReportDefinitionDto>(It.IsAny<ReportDefinition>()))
            .Returns((ReportDefinition d) => new ReportDefinitionDto
            {
                IdKey = IdKeyHelper.Encode(d.Id),
                Name = d.Name,
                Description = d.Description,
                IsActive = d.IsActive,
                IsSystem = d.IsSystem,
                ReportType = d.ReportType,
                OutputFormat = d.OutputFormat
            });

        // Act
        var result = await service.CreateDefinitionAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Report");
    }

    #endregion

    #region UpdateDefinitionAsync Tests

    [Fact]
    public async Task UpdateDefinitionAsync_WithValidIdKey_UpdatesDefinition()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        var definition = new ReportDefinition
        {
            Id = 1,
            Name = "Original Name",
            IsActive = true,
            IsSystem = false,
            ReportType = ReportType.AttendanceSummary,
            ParameterSchema = "{}",
            OutputFormat = ReportOutputFormat.Pdf
        };
        context.ReportDefinitions.Add(definition);
        await context.SaveChangesAsync();

        var request = new UpdateReportDefinitionRequest
        {
            Name = "Updated Name"
        };

        _mapperMock.Setup(m => m.Map<ReportDefinitionDto>(It.IsAny<ReportDefinition>()))
            .Returns((ReportDefinition d) => new ReportDefinitionDto
            {
                IdKey = IdKeyHelper.Encode(d.Id),
                Name = d.Name,
                IsActive = d.IsActive,
                IsSystem = d.IsSystem,
                ReportType = d.ReportType,
                OutputFormat = d.OutputFormat
            });

        // Act
        var result = await service.UpdateDefinitionAsync(_definitionIdKey, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateDefinitionAsync_WithSystemDefinition_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        var definition = new ReportDefinition
        {
            Id = 1,
            Name = "System Report",
            IsActive = true,
            IsSystem = true,
            ReportType = ReportType.AttendanceSummary,
            ParameterSchema = "{}",
            OutputFormat = ReportOutputFormat.Pdf
        };
        context.ReportDefinitions.Add(definition);
        await context.SaveChangesAsync();

        var request = new UpdateReportDefinitionRequest { Name = "Updated" };

        // Act
        var result = await service.UpdateDefinitionAsync(_definitionIdKey, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("UNPROCESSABLE_ENTITY");
    }

    #endregion

    #region DeleteDefinitionAsync Tests

    [Fact]
    public async Task DeleteDefinitionAsync_WithValidIdKey_SoftDeletesDefinition()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        var definition = new ReportDefinition
        {
            Id = 1,
            Name = "Test Report",
            IsActive = true,
            IsSystem = false,
            ReportType = ReportType.AttendanceSummary,
            ParameterSchema = "{}",
            OutputFormat = ReportOutputFormat.Pdf
        };
        context.ReportDefinitions.Add(definition);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteDefinitionAsync(_definitionIdKey);

        // Assert
        result.IsSuccess.Should().BeTrue();
        definition.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteDefinitionAsync_WithSystemDefinition_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        var definition = new ReportDefinition
        {
            Id = 1,
            Name = "System Report",
            IsActive = true,
            IsSystem = true,
            ReportType = ReportType.AttendanceSummary,
            ParameterSchema = "{}",
            OutputFormat = ReportOutputFormat.Pdf
        };
        context.ReportDefinitions.Add(definition);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteDefinitionAsync(_definitionIdKey);

        // Assert
        result.IsFailure.Should().BeTrue();
        definition.IsActive.Should().BeTrue();
    }

    #endregion

    #region RunReportAsync Tests

    [Fact]
    public async Task RunReportAsync_WithValidRequest_CreatesRunAndEnqueuesJob()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        var definition = new ReportDefinition
        {
            Id = 1,
            Name = "Test Report",
            IsActive = true,
            ReportType = ReportType.AttendanceSummary,
            ParameterSchema = "{}",
            OutputFormat = ReportOutputFormat.Pdf
        };
        context.ReportDefinitions.Add(definition);
        await context.SaveChangesAsync();

        _backgroundJobServiceMock
            .Setup(b => b.Enqueue<ReportService>(It.IsAny<System.Linq.Expressions.Expression<Action<ReportService>>>()))
            .Returns("job-123");

        _mapperMock.Setup(m => m.Map<ReportRunDto>(It.IsAny<ReportRun>()))
            .Returns((ReportRun r) => new ReportRunDto
            {
                IdKey = IdKeyHelper.Encode(r.Id),
                ReportDefinitionIdKey = IdKeyHelper.Encode(r.ReportDefinitionId),
                ReportName = r.ReportDefinition?.Name ?? "",
                Status = r.Status
            });

        var request = new RunReportRequest
        {
            ReportDefinitionIdKey = _definitionIdKey
        };

        // Act
        var result = await service.RunReportAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ReportStatus.Pending);
        _backgroundJobServiceMock.Verify(b => b.Enqueue<ReportService>(
            It.IsAny<System.Linq.Expressions.Expression<Action<ReportService>>>()), Times.Once);
    }

    [Fact]
    public async Task RunReportAsync_WithInactiveDefinition_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        var definition = new ReportDefinition
        {
            Id = 1,
            Name = "Inactive Report",
            IsActive = false,
            ReportType = ReportType.AttendanceSummary,
            ParameterSchema = "{}",
            OutputFormat = ReportOutputFormat.Pdf
        };
        context.ReportDefinitions.Add(definition);
        await context.SaveChangesAsync();

        var request = new RunReportRequest
        {
            ReportDefinitionIdKey = _definitionIdKey
        };

        // Act
        var result = await service.RunReportAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("UNPROCESSABLE_ENTITY");
    }

    #endregion

    #region GetRunsAsync Tests

    [Fact]
    public async Task GetRunsAsync_ReturnsPagedRuns()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        var definition = new ReportDefinition
        {
            Id = 1,
            Name = "Test Report",
            ReportType = ReportType.AttendanceSummary,
            ParameterSchema = "{}",
            OutputFormat = ReportOutputFormat.Pdf
        };
        context.ReportDefinitions.Add(definition);

        context.ReportRuns.Add(new ReportRun
        {
            Id = 100,
            ReportDefinitionId = 1,
            Status = ReportStatus.Completed,
            Parameters = "{}"
        });
        context.ReportRuns.Add(new ReportRun
        {
            Id = 101,
            ReportDefinitionId = 1,
            Status = ReportStatus.Pending,
            Parameters = "{}"
        });
        await context.SaveChangesAsync();

        _mapperMock.Setup(m => m.Map<List<ReportRunDto>>(It.IsAny<List<ReportRun>>()))
            .Returns((List<ReportRun> runs) => runs.Select(r => new ReportRunDto
            {
                IdKey = IdKeyHelper.Encode(r.Id),
                ReportDefinitionIdKey = IdKeyHelper.Encode(r.ReportDefinitionId),
                ReportName = r.ReportDefinition?.Name ?? "",
                Status = r.Status
            }).ToList());

        // Act
        var result = await service.GetRunsAsync(page: 1, pageSize: 25);

        // Assert
        result.Items.Should().HaveCount(2);
    }

    #endregion

    #region GetRunAsync Tests

    [Fact]
    public async Task GetRunAsync_WithValidIdKey_ReturnsRun()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        var definition = new ReportDefinition
        {
            Id = 1,
            Name = "Test Report",
            ReportType = ReportType.AttendanceSummary,
            ParameterSchema = "{}",
            OutputFormat = ReportOutputFormat.Pdf
        };
        context.ReportDefinitions.Add(definition);

        var run = new ReportRun
        {
            Id = 100,
            ReportDefinitionId = 1,
            Status = ReportStatus.Completed,
            Parameters = "{}"
        };
        context.ReportRuns.Add(run);
        await context.SaveChangesAsync();

        _mapperMock.Setup(m => m.Map<ReportRunDto>(It.IsAny<ReportRun>()))
            .Returns(new ReportRunDto
            {
                IdKey = _runIdKey,
                ReportDefinitionIdKey = _definitionIdKey,
                ReportName = "Test Report",
                Status = ReportStatus.Completed
            });

        // Act
        var result = await service.GetRunAsync(_runIdKey);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(ReportStatus.Completed);
    }

    #endregion

    #region DownloadReportAsync Tests

    [Fact]
    public async Task DownloadReportAsync_WithCompletedReport_ReturnsFileStream()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        var definition = new ReportDefinition
        {
            Id = 1,
            Name = "Test Report",
            ReportType = ReportType.AttendanceSummary,
            ParameterSchema = "{}",
            OutputFormat = ReportOutputFormat.Pdf
        };
        context.ReportDefinitions.Add(definition);

        var binaryFile = new BinaryFile
        {
            Id = 1,
            FileName = "report.pdf",
            MimeType = "application/pdf",
            StorageKey = "storage-key-123"
        };
        context.BinaryFiles.Add(binaryFile);

        var run = new ReportRun
        {
            Id = 100,
            ReportDefinitionId = 1,
            Status = ReportStatus.Completed,
            Parameters = "{}",
            OutputFileId = 1
        };
        context.ReportRuns.Add(run);
        await context.SaveChangesAsync();

        var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        _fileStorageServiceMock
            .Setup(f => f.GetFileAsync("storage-key-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileStream);

        // Act
        var result = await service.DownloadReportAsync(_runIdKey);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Stream.Should().NotBeNull();
        result.Value.FileName.Should().Be("report.pdf");
        result.Value.MimeType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task DownloadReportAsync_WithNotCompletedReport_ReturnsNull()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = CreateService(context);

        var definition = new ReportDefinition
        {
            Id = 1,
            Name = "Test Report",
            ReportType = ReportType.AttendanceSummary,
            ParameterSchema = "{}",
            OutputFormat = ReportOutputFormat.Pdf
        };
        context.ReportDefinitions.Add(definition);

        var run = new ReportRun
        {
            Id = 100,
            ReportDefinitionId = 1,
            Status = ReportStatus.Pending,
            Parameters = "{}",
            OutputFileId = null
        };
        context.ReportRuns.Add(run);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DownloadReportAsync(_runIdKey);

        // Assert
        result.Should().BeNull();
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
        public DbSet<FamilyMember> FamilyMembers { get; set; } = null!;
        public DbSet<Family> Families { get; set; } = null!;
        public DbSet<GroupSchedule> GroupSchedules { get; set; } = null!;
        public DbSet<Campus> Campuses { get; set; } = null!;
        public DbSet<Location> Locations { get; set; } = null!;
        public DbSet<DefinedType> DefinedTypes { get; set; } = null!;
        public DbSet<DefinedValue> DefinedValues { get; set; } = null!;
        public DbSet<Schedule> Schedules { get; set; } = null!;
        public DbSet<Attendance> Attendances { get; set; } = null!;
        public DbSet<AttendanceOccurrence> AttendanceOccurrences { get; set; } = null!;
        public DbSet<AttendanceCode> AttendanceCodes { get; set; } = null!;
        public DbSet<Device> Devices { get; set; } = null!;
        public DbSet<LabelTemplate> LabelTemplates { get; set; } = null!;
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
        public DbSet<SecurityRole> SecurityRoles { get; set; } = null!;
        public DbSet<SecurityClaim> SecurityClaims { get; set; } = null!;
        public DbSet<PersonSecurityRole> PersonSecurityRoles { get; set; } = null!;
        public DbSet<RoleSecurityClaim> RoleSecurityClaims { get; set; } = null!;

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
