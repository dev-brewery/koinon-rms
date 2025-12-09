# Sprint 7 Infrastructure Analysis
## Background Job & File Storage Architecture

**Date:** December 8, 2025
**Analysis Scope:** Issues #134 (Hangfire) and #135 (BinaryFile)
**Target:** Provide implementation architecture following koinon-rms clean architecture patterns

---

## Executive Summary

Both features require foundational infrastructure that fits cleanly into the existing clean architecture layers:

- **Background Job Infrastructure** provides async processing, retry mechanisms, and scheduled execution
- **File Storage Infrastructure** enables file persistence with audit trails and permission controls

Both should follow established patterns: interface-driven design in Application, options-based configuration in Infrastructure, and strict layer separation.

---

## Part 1: Background Job Infrastructure (#134 - Hangfire)

### 1.1 Architecture Placement

```
Clean Architecture Mapping:
├── Domain (NO CHANGES)
│   └── [Background jobs are application-level concerns]
│
├── Application
│   └── Interfaces/
│       ├── IBackgroundJobService              [NEW]
│       └── IBackgroundJobScheduler            [NEW - Optional for advanced scenarios]
│
├── Infrastructure
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs     [EXTEND - Register Hangfire]
│   ├── Options/
│   │   └── HangfireOptions.cs                 [NEW]
│   ├── Services/
│   │   └── HangfireJobService.cs             [NEW - implements IBackgroundJobService]
│   ├── Providers/
│   │   └── HangfireProvider.cs               [NEW - Optional configuration provider]
│   └── Middleware/
│       └── HangfireAuthorizationMiddleware.cs [NEW - Dashboard security]
│
└── Api
    └── Program.cs                            [EXTEND - Register Hangfire dashboard]
```

### 1.2 Interface Design

**File:** `src/Koinon.Application/Interfaces/IBackgroundJobService.cs`

```csharp
namespace Koinon.Application.Interfaces;

/// <summary>
/// Service interface for background job management via Hangfire.
/// Provides methods to enqueue jobs with retry policies, schedule delayed execution,
/// and execute recurring operations.
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueues a fire-and-forget job that executes as soon as possible.
    /// Supports automatic retries on failure (configurable via options).
    /// </summary>
    /// <param name="jobName">Human-readable job identifier for logging</param>
    /// <param name="job">Async delegate to execute</param>
    /// <returns>Hangfire job ID for tracking</returns>
    Task<string> EnqueueAsync(string jobName, Func<CancellationToken, Task> job);

    /// <summary>
    /// Schedules a job to run at a specific UTC DateTime.
    /// </summary>
    /// <param name="jobName">Human-readable job identifier</param>
    /// <param name="job">Async delegate to execute</param>
    /// <param name="scheduledUtc">UTC time when job should execute</param>
    /// <returns>Hangfire job ID</returns>
    Task<string> ScheduleAsync(
        string jobName,
        Func<CancellationToken, Task> job,
        DateTime scheduledUtc);

    /// <summary>
    /// Schedules a job to run after a delay from now.
    /// </summary>
    /// <param name="jobName">Human-readable job identifier</param>
    /// <param name="job">Async delegate to execute</param>
    /// <param name="delay">Time to wait before execution</param>
    /// <returns>Hangfire job ID</returns>
    Task<string> ScheduleAsync(
        string jobName,
        Func<CancellationToken, Task> job,
        TimeSpan delay);

    /// <summary>
    /// Registers a recurring job using CRON expression.
    /// Jobs are stored in Hangfire and persist across restarts.
    /// </summary>
    /// <param name="jobId">Unique identifier for this recurring job (must be stable)</param>
    /// <param name="job">Async delegate to execute</param>
    /// <param name="cronExpression">CRON expression (e.g., "0 */6 * * *" = every 6 hours)</param>
    void RegisterRecurringJob(
        string jobId,
        Func<CancellationToken, Task> job,
        string cronExpression);

    /// <summary>
    /// Removes a previously registered recurring job.
    /// </summary>
    void RemoveRecurringJob(string jobId);

    /// <summary>
    /// Checks if the background job service is operational.
    /// Returns false if storage is unavailable.
    /// </summary>
    bool IsConfigured { get; }
}

/// <summary>
/// Result of a background job operation.
/// </summary>
public record BackgroundJobResult(bool Success, string? JobId, string? ErrorMessage);
```

### 1.3 Options Configuration

**File:** `src/Koinon.Infrastructure/Options/HangfireOptions.cs`

**Pattern Match:** Similar to `TwilioOptions`

```csharp
namespace Koinon.Infrastructure.Options;

/// <summary>
/// Configuration options for Hangfire background job processing.
/// Loaded from the "Hangfire" configuration section.
/// </summary>
public class HangfireOptions
{
    public const string SectionName = "Hangfire";

    /// <summary>
    /// Enable Hangfire background job processing.
    /// If false, jobs are executed synchronously (no-op).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// PostgreSQL connection string for job storage.
    /// Defaults to main database connection if not specified.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Database schema name for Hangfire tables (PostgreSQL only).
    /// Defaults to "hangfire" if not specified.
    /// </summary>
    public string SchemaName { get; set; } = "hangfire";

    /// <summary>
    /// Maximum number of retries for failed jobs.
    /// Default: 3 retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Enable dashboard at /hangfire route (requires authentication).
    /// Default: true in Development, false in Production.
    /// </summary>
    public bool EnableDashboard { get; set; } = true;

    /// <summary>
    /// Job expiration time in hours.
    /// Completed jobs are deleted after this period.
    /// Default: 336 (14 days).
    /// </summary>
    public int JobExpirationHours { get; set; } = 336;

    /// <summary>
    /// Validation: Check if Hangfire is properly configured.
    /// </summary>
    public bool IsValid => true; // Always valid - connection string fallback handled in service
}
```

### 1.4 Service Implementation

**File:** `src/Koinon.Infrastructure/Services/HangfireJobService.cs`

**Pattern Match:** Similar to `TwilioSmsService` and `SmtpEmailSender`

Key aspects:
- Primary constructor with injected dependencies
- Configuration validation
- Graceful degradation when disabled
- Comprehensive logging
- Cancellation token support

```csharp
namespace Koinon.Infrastructure.Services;

public class HangfireJobService(
    IBackgroundJobClient jobClient,           // Hangfire injected dependency
    IRecurringJobManager recurringJobManager, // Hangfire injected dependency
    IOptions<HangfireOptions> options,
    ILogger<HangfireJobService> logger) : IBackgroundJobService
{
    private readonly HangfireOptions _options = options.Value;

    public bool IsConfigured => _options.Enabled;

    public async Task<string> EnqueueAsync(
        string jobName,
        Func<CancellationToken, Task> job)
    {
        if (!IsConfigured)
        {
            logger.LogWarning(
                "Hangfire not configured. Job '{JobName}' will not be enqueued",
                jobName);
            return string.Empty;
        }

        try
        {
            var jobId = jobClient.Enqueue(
                () => ExecuteJobAsync(job, CancellationToken.None));

            logger.LogInformation(
                "Job enqueued: {JobName}, JobId: {JobId}",
                jobName,
                jobId);

            return jobId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enqueue job: {JobName}", jobName);
            return string.Empty;
        }
    }

    // ... Additional methods follow similar pattern with try-catch, logging, and null handling

    private static async Task ExecuteJobAsync(
        Func<CancellationToken, Task> job,
        CancellationToken ct)
    {
        await job(ct);
    }
}
```

### 1.5 DI Registration Pattern

**File:** `src/Koinon.Infrastructure/Extensions/ServiceCollectionExtensions.cs` (EXTEND existing)

**Location:** Add after email/SMS registration (around line 88)

```csharp
// Configure Hangfire options
services.Configure<HangfireOptions>(configuration.GetSection(HangfireOptions.SectionName));

// Register Hangfire with PostgreSQL storage
var hangfireConnectionString = configuration.GetConnectionString("Hangfire")
    ?? postgresConnectionString;

services.AddHangfire(config => config
    .UsePostgreSqlStorage(c => c
        .UseNpgsqlConnection(hangfireConnectionString))
    .WithJobExpirationTimeout(_options.JobExpirationHours));

// Add Hangfire server
services.AddHangfireServer(config =>
{
    config.WorkerCount = Environment.ProcessorCount;
});

// Register background job service
services.AddScoped<IBackgroundJobService, HangfireJobService>();
```

### 1.6 API Program.cs Integration

**File:** `src/Koinon.Api/Program.cs` (EXTEND)

Add before `app.Run()`:

```csharp
// Configure Hangfire dashboard (requires authentication)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new DashboardAuthorizationFilter() },
    IsReadOnlyDashboard = !app.Environment.IsDevelopment()
});
```

### 1.7 Usage Examples

In any application service:

```csharp
public class CommunicationService(
    IApplicationDbContext context,
    IBackgroundJobService backgroundJobService,
    ILogger<CommunicationService> logger) : ICommunicationService
{
    public async Task<Result<CommunicationDto>> SendAsync(string idKey, CancellationToken ct)
    {
        var communication = await context.Communications
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        communication.Status = CommunicationStatus.Pending;
        await context.SaveChangesAsync(ct);

        // Enqueue background job to send email/SMS
        var jobId = await backgroundJobService.EnqueueAsync(
            $"SendCommunication-{communication.Id}",
            async cancellationToken => await SendCommunicationInternally(
                communication.Id,
                cancellationToken));

        logger.LogInformation(
            "Communication {Id} queued for sending. JobId: {JobId}",
            communication.Id,
            jobId);

        return Result<CommunicationDto>.Success(mapper.Map<CommunicationDto>(communication));
    }
}
```

### 1.8 Database Migration

**File:** `src/Koinon.Infrastructure/Migrations/[timestamp]_AddHangfireSchema.cs`

Hangfire automatically creates its schema on first run, BUT you should create a migration for version control:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Hangfire will create its own schema and tables
    // This migration documents the expected schema for the team
    migrationBuilder.Sql(@"
        CREATE SCHEMA IF NOT EXISTS hangfire;

        COMMENT ON SCHEMA hangfire IS 'Hangfire background job storage';
    ");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // Note: Dropping the Hangfire schema will lose all job history
    migrationBuilder.Sql(@"DROP SCHEMA IF EXISTS hangfire CASCADE;");
}
```

### 1.9 Key Design Decisions

| Decision | Rationale | Trade-offs |
|----------|-----------|-----------|
| **PostgreSQL Storage** | Single database (no external dependencies), familiar operations | Slightly larger connection pool usage |
| **Graceful Degradation** | Service works when Hangfire disabled (useful for testing) | Synchronous execution when disabled lacks retry/scheduling |
| **Async/Await Pattern** | Matches .NET 8 conventions throughout codebase | Hangfire has async support limitations (jobs must be serializable) |
| **No Interfaces in Domain** | Background jobs are infrastructure concern | Keeps Domain pure, but requires Application-level interface |
| **Dashboard with Auth** | Prevents unauthorized access to job history | Requires filter implementation (simple one-liner) |

### 1.10 Risk Mitigation

**Risk:** Job serialization failures (if passing complex objects)
**Mitigation:** Use `Func<CancellationToken, Task>` pattern - forces jobs to be simple closures. For data, load from DB inside the job closure.

**Risk:** Job failures in production
**Mitigation:** All jobs caught and logged. Failed jobs visible in dashboard. MaxRetries configurable. Consider alerting on repeated failures.

**Risk:** Database schema conflicts
**Mitigation:** Hangfire uses isolated `hangfire` schema. Never conflicts with application tables.

---

## Part 2: File Storage Infrastructure (#135 - BinaryFile)

### 2.1 Architecture Placement

```
Clean Architecture Mapping:
├── Domain
│   └── Entities/
│       └── BinaryFile.cs                     [NEW]
│
├── Application
│   ├── Interfaces/
│   │   └── IFileStorageService.cs           [NEW]
│   ├── DTOs/
│   │   ├── FileUploadDto.cs                 [NEW]
│   │   └── BinaryFileDto.cs                 [NEW]
│   └── Validators/
│       └── FileUploadValidator.cs            [NEW]
│
├── Infrastructure
│   ├── Configurations/
│   │   └── BinaryFileConfiguration.cs       [NEW - EF Core config]
│   ├── Options/
│   │   └── FileStorageOptions.cs            [NEW]
│   ├── Services/
│   │   ├── FileStorageService.cs            [NEW - implements IFileStorageService]
│   │   └── FileStorageProvider/
│   │       ├── IFileStorageProvider.cs      [NEW - abstraction]
│   │       ├── DatabaseFileStorageProvider.cs [NEW - stores in BYTEA column]
│   │       └── LocalFileSystemProvider.cs   [NEW - optional future: file system]
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs   [EXTEND - Register services]
│
└── Api
    ├── Controllers/
    │   └── FilesController.cs                [NEW - endpoints for upload/download]
    └── Program.cs                            [EXTEND - Register services]
```

### 2.2 Entity Design

**File:** `src/Koinon.Domain/Entities/BinaryFile.cs`

**Pattern Match:** Follow `Entity` base class, include auditable fields

```csharp
namespace Koinon.Domain.Entities;

/// <summary>
/// Represents a file stored in the system (photos, documents, attachments).
/// Tracks file metadata, mime type, and audit information.
/// </summary>
public class BinaryFile : Entity
{
    /// <summary>
    /// Original filename (user-provided).
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// MIME type of the file (e.g., "image/jpeg", "application/pdf").
    /// </summary>
    public required string MimeType { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MD5 hash of file content for integrity verification.
    /// Allows detecting duplicate uploads.
    /// </summary>
    public required string ContentHash { get; set; }

    /// <summary>
    /// Storage provider identifier (e.g., "database", "filesystem").
    /// Allows migration between storage providers without data loss.
    /// </summary>
    public required string StorageProvider { get; set; }

    /// <summary>
    /// Provider-specific storage path or key.
    /// For database storage: column reference.
    /// For file system: relative path.
    /// </summary>
    public required string StoragePath { get; set; }

    /// <summary>
    /// Optional entity type this file is associated with.
    /// Examples: "Person", "Group", "Communication".
    /// Used for orphan cleanup and permission checks.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Optional entity ID this file is associated with.
    /// Used together with EntityType for relationship tracking.
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// File purpose/category (e.g., "photo", "document", "receipt").
    /// Helps with organization and filtering.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Optional description of file content.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Is this file marked for deletion.
    /// Soft delete allows recovery and cleanup jobs.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// DateTime when file was marked for deletion.
    /// NULL if not deleted.
    /// </summary>
    public DateTime? DeletedDateTime { get; set; }

    /// <summary>
    /// The actual binary content.
    /// Stored in separate property/column for efficient querying.
    /// </summary>
    public byte[]? Content { get; set; }
}
```

**Key Design Decisions:**

| Field | Decision | Rationale |
|-------|----------|-----------|
| **ContentHash** | Include MD5 | Detect duplicate uploads without storing twice |
| **StorageProvider** | String (not enum) | Allow future provider additions without migration |
| **EntityType/EntityId** | Optional FK pattern | Track relationships without explicit foreign keys initially |
| **Category** | Denormalized | Fast filtering without joins |
| **IsDeleted** | Soft delete | Allow recovery, audit trail, gradual cleanup |
| **Content** | Separate field | Can be loaded separately for efficiency |

### 2.3 EF Core Configuration

**File:** `src/Koinon.Infrastructure/Configurations/BinaryFileConfiguration.cs`

**Pattern Match:** Follow `PersonConfiguration` style

```csharp
namespace Koinon.Infrastructure.Configurations;

public class BinaryFileConfiguration : IEntityTypeConfiguration<BinaryFile>
{
    public void Configure(EntityTypeBuilder<BinaryFile> builder)
    {
        builder.ToTable("binary_file");

        // Primary key
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");

        // GUID unique index
        builder.Property(f => f.Guid)
            .HasColumnName("guid")
            .IsRequired();
        builder.HasIndex(f => f.Guid)
            .IsUnique()
            .HasDatabaseName("uix_binary_file_guid");

        // Ignore computed properties
        builder.Ignore(f => f.IdKey);

        // File metadata
        builder.Property(f => f.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(f => f.MimeType)
            .HasColumnName("mime_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(f => f.FileSize)
            .HasColumnName("file_size")
            .IsRequired();

        builder.Property(f => f.ContentHash)
            .HasColumnName("content_hash")
            .HasMaxLength(32)  // MD5 is always 32 chars
            .IsRequired();

        // Storage information
        builder.Property(f => f.StorageProvider)
            .HasColumnName("storage_provider")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(f => f.StoragePath)
            .HasColumnName("storage_path")
            .IsRequired();

        // Entity relationship
        builder.Property(f => f.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(50);

        builder.Property(f => f.EntityId)
            .HasColumnName("entity_id");

        builder.HasIndex(f => new { f.EntityType, f.EntityId })
            .HasDatabaseName("ix_binary_file_entity");

        // Category for filtering
        builder.Property(f => f.Category)
            .HasColumnName("category")
            .HasMaxLength(50);

        builder.HasIndex(f => f.Category)
            .HasDatabaseName("ix_binary_file_category");

        builder.Property(f => f.Description)
            .HasColumnName("description");

        // Soft delete
        builder.Property(f => f.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(f => f.DeletedDateTime)
            .HasColumnName("deleted_date_time");

        builder.HasIndex(f => new { f.IsDeleted, f.DeletedDateTime })
            .HasDatabaseName("ix_binary_file_deleted");

        // Binary content (in main table for simplicity)
        builder.Property(f => f.Content)
            .HasColumnName("content")
            .HasColumnType("bytea");  // PostgreSQL binary type

        // Audit fields
        builder.Property(f => f.CreatedDateTime)
            .HasColumnName("created_date_time")
            .IsRequired();

        builder.Property(f => f.ModifiedDateTime)
            .HasColumnName("modified_date_time");

        builder.Property(f => f.CreatedByPersonAliasId)
            .HasColumnName("created_by_person_alias_id");

        builder.Property(f => f.ModifiedByPersonAliasId)
            .HasColumnName("modified_by_person_alias_id");
    }
}
```

**PostgreSQL Notes:**
- `bytea` is efficient for binary data in PostgreSQL
- Content can be excluded from queries using `.Ignore()` if needed for performance
- Consider partitioning by `created_date_time` if table grows very large

### 2.4 Interface Design

**File:** `src/Koinon.Application/Interfaces/IFileStorageService.cs`

```csharp
namespace Koinon.Application.Interfaces;

/// <summary>
/// Service for storing and retrieving binary files (photos, documents, attachments).
/// Abstracts the underlying storage mechanism (database, filesystem, cloud).
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file and returns metadata.
    /// Validates file type, size, and integrity.
    /// </summary>
    /// <param name="fileName">Original filename</param>
    /// <param name="mimeType">File MIME type (e.g., "image/jpeg")</param>
    /// <param name="content">Binary file content</param>
    /// <param name="entityType">Optional entity type (e.g., "Person") for relationship tracking</param>
    /// <param name="entityId">Optional entity ID for relationship tracking</param>
    /// <param name="category">Optional category for filtering (e.g., "photo")</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Uploaded file metadata</returns>
    Task<Result<BinaryFileDto>> UploadAsync(
        string fileName,
        string mimeType,
        byte[] content,
        string? entityType = null,
        int? entityId = null,
        string? category = null,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads a file by its IdKey.
    /// Includes authorization check (user can only download if they created it
    /// or have admin role).
    /// </summary>
    /// <param name="idKey">File's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>File content and metadata</returns>
    Task<Result<FileContentDto>> DownloadAsync(
        string idKey,
        CancellationToken ct = default);

    /// <summary>
    /// Gets file metadata without downloading content.
    /// </summary>
    /// <param name="idKey">File's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>File metadata</returns>
    Task<Result<BinaryFileDto>> GetMetadataAsync(
        string idKey,
        CancellationToken ct = default);

    /// <summary>
    /// Marks a file as deleted (soft delete).
    /// Allows recovery within retention period.
    /// </summary>
    /// <param name="idKey">File's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<Result> DeleteAsync(
        string idKey,
        CancellationToken ct = default);

    /// <summary>
    /// Permanently deletes a file (hard delete).
    /// Used for compliance or cleanup after retention period.
    /// </summary>
    /// <param name="idKey">File's IdKey</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<Result> PermanentlyDeleteAsync(
        string idKey,
        CancellationToken ct = default);

    /// <summary>
    /// Lists files for an entity (e.g., all photos for a person).
    /// </summary>
    /// <param name="entityType">Entity type (e.g., "Person")</param>
    /// <param name="entityId">Entity ID</param>
    /// <param name="category">Optional category filter</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of files</returns>
    Task<Result<List<BinaryFileDto>>> ListByEntityAsync(
        string entityType,
        int entityId,
        string? category = null,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if file service is operational.
    /// </summary>
    bool IsConfigured { get; }
}

/// <summary>
/// DTO for binary file metadata.
/// </summary>
public record BinaryFileDto(
    string IdKey,
    string FileName,
    string MimeType,
    long FileSize,
    string Category,
    string EntityType,
    int? EntityId,
    DateTime CreatedDateTime,
    bool IsDeleted);

/// <summary>
/// DTO for file download (includes content).
/// </summary>
public record FileContentDto(
    string IdKey,
    string FileName,
    string MimeType,
    byte[] Content,
    long FileSize);
```

### 2.5 Options Configuration

**File:** `src/Koinon.Infrastructure/Options/FileStorageOptions.cs`

```csharp
namespace Koinon.Infrastructure.Options;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Storage provider type: "Database" or "FileSystem".
    /// Default: "Database" (stores in PostgreSQL bytea column).
    /// </summary>
    public string Provider { get; set; } = "Database";

    /// <summary>
    /// Maximum file size in bytes (default: 10 MB).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Allowed MIME types (comma-separated).
    /// Leave empty to allow all types.
    /// Example: "image/jpeg,image/png,application/pdf"
    /// </summary>
    public string AllowedMimeTypes { get; set; } = string.Empty;

    /// <summary>
    /// File system storage directory (if using FileSystem provider).
    /// Relative to application root or absolute path.
    /// </summary>
    public string FileSystemPath { get; set; } = "storage/files";

    /// <summary>
    /// Days to keep soft-deleted files before hard deletion.
    /// Default: 30 days. Set to 0 to disable soft delete.
    /// </summary>
    public int SoftDeleteRetentionDays { get; set; } = 30;

    /// <summary>
    /// Enable automatic cleanup of expired soft-deleted files.
    /// Cleanup runs daily if enabled (scheduled job).
    /// </summary>
    public bool EnableAutoCleanup { get; set; } = true;

    /// <summary>
    /// Enable virus scanning on upload (future enhancement).
    /// Default: false.
    /// </summary>
    public bool EnableVirusScanning { get; set; } = false;

    public bool IsValid => !string.IsNullOrWhiteSpace(Provider);
}
```

### 2.6 Service Implementation

**File:** `src/Koinon.Infrastructure/Services/FileStorageService.cs`

```csharp
namespace Koinon.Infrastructure.Services;

/// <summary>
/// Implementation of IFileStorageService using configurable storage provider.
/// Handles validation, audit logging, and permission checks.
/// </summary>
public class FileStorageService(
    IApplicationDbContext context,
    IOptions<FileStorageOptions> options,
    IFileStorageProvider storageProvider,
    IUserContext userContext,
    IMapper mapper,
    ILogger<FileStorageService> logger) : IFileStorageService
{
    private readonly FileStorageOptions _options = options.Value;

    public bool IsConfigured => _options.IsValid;

    public async Task<Result<BinaryFileDto>> UploadAsync(
        string fileName,
        string mimeType,
        byte[] content,
        string? entityType = null,
        int? entityId = null,
        string? category = null,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("File storage not configured");
            return Result<BinaryFileDto>.Failure("File storage not configured");
        }

        // Validation
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Result<BinaryFileDto>.Failure("Filename is required");
        }

        if (content == null || content.Length == 0)
        {
            return Result<BinaryFileDto>.Failure("File content is required");
        }

        if (content.Length > _options.MaxFileSizeBytes)
        {
            return Result<BinaryFileDto>.Failure(
                $"File size exceeds maximum of {_options.MaxFileSizeBytes} bytes");
        }

        // Validate MIME type
        if (!string.IsNullOrWhiteSpace(_options.AllowedMimeTypes))
        {
            var allowedTypes = _options.AllowedMimeTypes.Split(',');
            if (!allowedTypes.Contains(mimeType))
            {
                return Result<BinaryFileDto>.Failure(
                    $"MIME type '{mimeType}' is not allowed");
            }
        }

        try
        {
            // Calculate content hash
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(content);
                var contentHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                // Store in database
                var binaryFile = new BinaryFile
                {
                    FileName = Path.GetFileName(fileName),
                    MimeType = mimeType,
                    FileSize = content.Length,
                    ContentHash = contentHash,
                    StorageProvider = _options.Provider,
                    StoragePath = $"file_{Guid.NewGuid()}",
                    EntityType = entityType,
                    EntityId = entityId,
                    Category = category,
                    Content = content,
                    CreatedDateTime = DateTime.UtcNow,
                    CreatedByPersonAliasId = userContext.CurrentPersonAliasId
                };

                context.BinaryFiles.Add(binaryFile);
                await context.SaveChangesAsync(ct);

                logger.LogInformation(
                    "File uploaded: {FileName}, Size: {FileSize}, Id: {Id}",
                    binaryFile.FileName,
                    binaryFile.FileSize,
                    binaryFile.IdKey);

                return Result<BinaryFileDto>.Success(mapper.Map<BinaryFileDto>(binaryFile));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload file: {FileName}", fileName);
            return Result<BinaryFileDto>.Failure("Failed to upload file");
        }
    }

    public async Task<Result<FileContentDto>> DownloadAsync(
        string idKey,
        CancellationToken ct = default)
    {
        if (!IdKeyHelper.TryDecode(idKey, out int id))
        {
            return Result<FileContentDto>.Failure("Invalid file ID");
        }

        var file = await context.BinaryFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, ct);

        if (file == null)
        {
            return Result<FileContentDto>.Failure("File not found");
        }

        // AUTHORIZATION: Check if user can download (owner or admin)
        var isOwner = file.CreatedByPersonAliasId == userContext.CurrentPersonAliasId;
        var isAdmin = userContext.IsInRole("Admin");

        if (!isOwner && !isAdmin)
        {
            logger.LogWarning(
                "Unauthorized download attempt for file {FileId} by user {UserId}",
                idKey,
                userContext.CurrentPersonId);
            return Result<FileContentDto>.Failure("You do not have permission to download this file");
        }

        try
        {
            return Result<FileContentDto>.Success(new FileContentDto(
                file.IdKey,
                file.FileName,
                file.MimeType,
                file.Content ?? [],
                file.FileSize));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download file: {FileId}", idKey);
            return Result<FileContentDto>.Failure("Failed to download file");
        }
    }

    // ... Additional methods follow similar pattern
}
```

### 2.7 Storage Provider Abstraction

**File:** `src/Koinon.Infrastructure/Services/FileStorage/IFileStorageProvider.cs`

Allows future migration to cloud storage without changing service interface:

```csharp
namespace Koinon.Infrastructure.Services.FileStorage;

/// <summary>
/// Abstraction for different file storage backends.
/// Allows migration between storage providers without changing application code.
/// </summary>
public interface IFileStorageProvider
{
    /// <summary>
    /// Stores file content and returns provider-specific path/key.
    /// </summary>
    Task<string> StoreAsync(byte[] content, string fileName, CancellationToken ct);

    /// <summary>
    /// Retrieves file content by provider path.
    /// </summary>
    Task<byte[]> RetrieveAsync(string storagePath, CancellationToken ct);

    /// <summary>
    /// Deletes file from storage.
    /// </summary>
    Task DeleteAsync(string storagePath, CancellationToken ct);
}

/// <summary>
/// Database storage provider - stores files in PostgreSQL bytea column.
/// Benefits: Single database, ACID transactions, no external dependencies.
/// Trade-offs: Limited scalability for very large files.
/// </summary>
public class DatabaseFileStorageProvider : IFileStorageProvider
{
    public Task<string> StoreAsync(byte[] content, string fileName, CancellationToken ct)
    {
        // Return a reference for database storage (not actually used in our implementation)
        return Task.FromResult($"db://{fileName}_{Guid.NewGuid()}");
    }

    public Task<byte[]> RetrieveAsync(string storagePath, CancellationToken ct)
    {
        // Database storage loads content directly from BinaryFile.Content
        throw new NotImplementedException("Use DbContext to load content");
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct)
    {
        // Database storage deletes via DbContext
        throw new NotImplementedException("Use DbContext to delete");
    }
}
```

### 2.8 API Controller

**File:** `src/Koinon.Api/Controllers/FilesController.cs`

```csharp
namespace Koinon.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[ValidateIdKey]
public class FilesController(
    IFileStorageService fileStorageService,
    ILogger<FilesController> logger) : ControllerBase
{
    /// <summary>
    /// Uploads a file with optional metadata.
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(BinaryFileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string? entityType = null,
        [FromForm] int? entityId = null,
        [FromForm] string? category = null,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid file",
                Detail = "File is required and must not be empty"
            });
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);

        var result = await fileStorageService.UploadAsync(
            file.FileName,
            file.ContentType,
            stream.ToArray(),
            entityType,
            entityId,
            category,
            ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Upload failed",
                Detail = result.ErrorMessage
            });
        }

        return CreatedAtAction(nameof(Download), new { idKey = result.Data.IdKey }, result.Data);
    }

    /// <summary>
    /// Downloads a file by its IdKey.
    /// </summary>
    [HttpGet("{idKey}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(string idKey, CancellationToken ct = default)
    {
        var result = await fileStorageService.DownloadAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            return NotFound(new ProblemDetails
            {
                Title = "File not found",
                Detail = result.ErrorMessage
            });
        }

        var fileContent = result.Data;
        return File(
            fileContent.Content,
            fileContent.MimeType,
            fileContent.FileName);
    }

    /// <summary>
    /// Lists files for an entity (e.g., all photos for a person).
    /// </summary>
    [HttpGet("entity/{entityType}/{entityId:int}")]
    [ProducesResponseType(typeof(List<BinaryFileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListByEntity(
        string entityType,
        int entityId,
        [FromQuery] string? category = null,
        CancellationToken ct = default)
    {
        var result = await fileStorageService.ListByEntityAsync(
            entityType,
            entityId,
            category,
            ct);

        return Ok(result.Data ?? new List<BinaryFileDto>());
    }

    /// <summary>
    /// Deletes a file (soft delete).
    /// </summary>
    [HttpDelete("{idKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string idKey, CancellationToken ct = default)
    {
        var result = await fileStorageService.DeleteAsync(idKey, ct);

        if (!result.IsSuccess)
        {
            return NotFound(new ProblemDetails
            {
                Title = "File not found or cannot be deleted",
                Detail = result.ErrorMessage
            });
        }

        return NoContent();
    }
}
```

### 2.9 Database Migration

**File:** `src/Koinon.Infrastructure/Migrations/[timestamp]_AddBinaryFileEntity.cs`

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "binary_file",
        columns: table => new
        {
            id = table.Column<int>(nullable: false)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            guid = table.Column<Guid>(nullable: false),
            file_name = table.Column<string>(maxLength: 255, nullable: false),
            mime_type = table.Column<string>(maxLength: 100, nullable: false),
            file_size = table.Column<long>(nullable: false),
            content_hash = table.Column<string>(maxLength: 32, nullable: false),
            storage_provider = table.Column<string>(maxLength: 50, nullable: false),
            storage_path = table.Column<string>(nullable: false),
            entity_type = table.Column<string>(maxLength: 50, nullable: true),
            entity_id = table.Column<int>(nullable: true),
            category = table.Column<string>(maxLength: 50, nullable: true),
            description = table.Column<string>(nullable: true),
            is_deleted = table.Column<bool>(nullable: false, defaultValue: false),
            deleted_date_time = table.Column<DateTime>(nullable: true),
            content = table.Column<byte[]>(nullable: true),
            created_date_time = table.Column<DateTime>(nullable: false),
            modified_date_time = table.Column<DateTime>(nullable: true),
            created_by_person_alias_id = table.Column<int>(nullable: true),
            modified_by_person_alias_id = table.Column<int>(nullable: true)
        },
        constraints: table =>
        {
            table.PrimaryKey("pk_binary_file", x => x.id);
        });

    migrationBuilder.CreateIndex(
        name: "uix_binary_file_guid",
        table: "binary_file",
        column: "guid",
        unique: true);

    migrationBuilder.CreateIndex(
        name: "ix_binary_file_entity",
        table: "binary_file",
        columns: new[] { "entity_type", "entity_id" });

    migrationBuilder.CreateIndex(
        name: "ix_binary_file_category",
        table: "binary_file",
        column: "category");

    migrationBuilder.CreateIndex(
        name: "ix_binary_file_deleted",
        table: "binary_file",
        columns: new[] { "is_deleted", "deleted_date_time" });
}
```

### 2.10 DI Registration

**File:** `src/Koinon.Infrastructure/Extensions/ServiceCollectionExtensions.cs` (EXTEND)

Add after file storage options are configured:

```csharp
// Configure file storage options
services.Configure<FileStorageOptions>(
    configuration.GetSection(FileStorageOptions.SectionName));

// Register file storage provider (currently database, but abstraction allows changes)
services.AddSingleton<IFileStorageProvider, DatabaseFileStorageProvider>();

// Register file storage service
services.AddScoped<IFileStorageService, FileStorageService>();

// Register background job for cleanup of soft-deleted files
// (depends on IBackgroundJobService from #134)
services.AddScoped<IHostedService, FileCleanupBackgroundService>();
```

### 2.11 Update IApplicationDbContext

**File:** `src/Koinon.Application/Interfaces/IApplicationDbContext.cs` (EXTEND)

Add to DbSet properties:

```csharp
DbSet<BinaryFile> BinaryFiles { get; }
```

---

## Part 3: Implementation Order & Dependencies

### Phase 1: Foundation (Sprint 7, Week 1)
1. Create `BinaryFile` entity and EF configuration
2. Create migration for `binary_file` table
3. Create `IFileStorageService` interface and DTOs
4. Create `FileStorageService` implementation
5. Add `FilesController` for upload/download endpoints
6. Test file upload/download workflows

### Phase 2: Background Jobs (Sprint 7, Week 2)
1. Create `IBackgroundJobService` interface
2. Add `HangfireOptions` configuration
3. Create `HangfireJobService` implementation
4. Register Hangfire in DI
5. Create Hangfire migration for schema
6. Integrate jobs into `CommunicationService` (for sending emails/SMS)
7. Test job enqueueing and execution

### Phase 3: Integration & Polish (Sprint 7, Week 3-4)
1. File cleanup background job (soft delete + retention)
2. Dashboard authorization filter
3. Add tests for both features
4. Documentation and examples
5. Load testing (especially for file uploads)

### Dependency Chain

```
IApplicationDbContext
  └── BinaryFile entity + configuration
      └── IFileStorageService
          └── FileStorageService
              └── FilesController

IBackgroundJobService
  └── HangfireJobService
      └── CommunicationService (can now enqueue jobs)
          └── CommunicationSenderBackgroundService (updated to use jobs)

Optional: FileCleanupBackgroundService
  └── IBackgroundJobService (depends on Phase 2)
      └── Scheduled cleanup of soft-deleted files
```

---

## Part 4: Configuration Examples

### appsettings.Development.json

```json
{
  "FileStorage": {
    "Provider": "Database",
    "MaxFileSizeBytes": 10485760,
    "AllowedMimeTypes": "image/jpeg,image/png,image/webp,application/pdf",
    "SoftDeleteRetentionDays": 30,
    "EnableAutoCleanup": true
  },
  "Hangfire": {
    "Enabled": true,
    "EnableDashboard": true,
    "MaxRetries": 3,
    "JobExpirationHours": 336
  }
}
```

### appsettings.Production.json

```json
{
  "FileStorage": {
    "Provider": "Database",
    "MaxFileSizeBytes": 52428800,
    "AllowedMimeTypes": "image/jpeg,image/png,image/webp,application/pdf",
    "SoftDeleteRetentionDays": 90,
    "EnableAutoCleanup": true
  },
  "Hangfire": {
    "Enabled": true,
    "EnableDashboard": false,
    "MaxRetries": 5,
    "JobExpirationHours": 720
  }
}
```

---

## Part 5: Testing Strategy

### Unit Tests

**BinaryFile Upload Validation:**
```csharp
[TestFixture]
public class FileStorageServiceTests
{
    [Test]
    public async Task UploadAsync_WhenFileTooLarge_ReturnsFail()
    {
        var options = Options.Create(new FileStorageOptions
        {
            MaxFileSizeBytes = 1000
        });

        var service = new FileStorageService(
            context, options, provider, userContext, mapper, logger);

        var result = await service.UploadAsync(
            "test.pdf",
            "application/pdf",
            new byte[2000]);

        Assert.IsFalse(result.IsSuccess);
    }
}
```

### Integration Tests

**Background Job Execution:**
```csharp
[TestFixture]
public class HangfireJobServiceTests
{
    [Test]
    public async Task EnqueueAsync_WhenEnabled_CreatesJobInHangfire()
    {
        var jobClient = new Mock<IBackgroundJobClient>();
        jobClient.Setup(x => x.Enqueue(It.IsAny<Expression<Action<Task>>>()))
            .Returns("job-123");

        var service = new HangfireJobService(
            jobClient.Object, recurringJobManager, options, logger);

        var jobId = await service.EnqueueAsync(
            "TestJob",
            async ct => await Task.Delay(100, ct));

        jobClient.Verify(x => x.Enqueue(It.IsAny<Expression<Action<Task>>>()), Times.Once);
        Assert.AreEqual("job-123", jobId);
    }
}
```

---

## Part 6: Risk Mitigation

### Risk Matrix

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| **Large file uploads** | High | Medium | Enforce MaxFileSizeBytes, stream uploads, add progress feedback |
| **Job failure storms** | Medium | Medium | Exponential backoff, max retries, alerting |
| **Database growth** | Medium | Low | Index by `created_date_time`, partition strategy for future |
| **Permission leaks** | Low | High | Test authorization thoroughly, audit logging |
| **Migration conflicts** | Low | Medium | Run migrations in development, test on staging |

### Monitoring & Alerting

1. **Background Jobs:**
   - Failed job count (alert if > 10/hour)
   - Stuck jobs (in "Processing" state > 1 hour)
   - Dashboard access (audit log all views)

2. **File Storage:**
   - Orphaned files (no entity reference after N days)
   - Disk usage trends (if using file system)
   - Upload success rate (should be > 99%)

---

## Part 7: Future Enhancements

### Post-MVP Improvements

1. **Cloud Storage Support:**
   - Azure Blob Storage provider
   - AWS S3 provider
   - Google Cloud Storage provider
   - Implement `IFileStorageProvider` for each

2. **Advanced Job Features:**
   - Job priorities
   - Dead-letter queue for repeated failures
   - Job dependencies/chains
   - Saga pattern for long-running processes

3. **File Management:**
   - Virus scanning integration (ClamAV)
   - Image optimization/thumbnail generation
   - Duplicate detection by content hash
   - File compression for archival

4. **Performance:**
   - Async file uploads with progress callbacks
   - Chunked uploads for large files
   - Content Delivery Network (CDN) integration
   - Presigned URLs for direct downloads

5. **Security:**
   - Encryption at rest (transparent)
   - Rate limiting per user
   - File access audit log
   - Watermarking for sensitive files

---

## Part 8: Quick Reference

### Entity Layer (Domain)
- BinaryFile.cs - Complete entity with audit fields and soft delete
- No changes to existing entities

### Application Layer
- IBackgroundJobService - Interface for job management
- IFileStorageService - Interface for file storage
- DTOs for both features
- Validators for file uploads

### Infrastructure Layer
- HangfireOptions, FileStorageOptions - Configuration
- HangfireJobService, FileStorageService - Implementations
- BinaryFileConfiguration - EF Core entity mapping
- ServiceCollectionExtensions - DI registration (extends existing)

### API Layer
- FilesController - Upload/download endpoints (new)
- Program.cs - Hangfire middleware + services registration
- Update middleware order if needed

### Database
- Two migrations:
  1. Add `binary_file` table with indexes
  2. Add `hangfire` schema (auto-created by Hangfire)

---

## Conclusion

Both features integrate seamlessly with koinon-rms's existing clean architecture:

1. **Follow established patterns:**
   - Primary constructors with dependency injection
   - Options-based configuration
   - Graceful degradation when disabled
   - Comprehensive logging and error handling

2. **Zero breaking changes:**
   - Only additive changes to code
   - Migrations handle schema changes
   - Existing services unaffected

3. **Testable design:**
   - Interface-driven implementation
   - Mock-friendly dependencies
   - Async/await patterns for async operations

4. **Performance-conscious:**
   - Indexes on frequently queried columns
   - Lazy loading of file content
   - Background processing for long-running tasks
   - Rate limiting and retry mechanisms

The recommended implementation order prioritizes file storage first (simpler, no new hosted services) followed by background jobs (infrastructure-heavy, enables async processing).
