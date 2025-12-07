using Koinon.Application.Interfaces;
using Koinon.Domain.Entities;
using Koinon.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Koinon.Infrastructure.Data;

/// <summary>
/// Main Entity Framework Core DbContext for the Koinon RMS application.
/// Provides access to all entity sets and applies configurations.
/// </summary>
public class KoinonDbContext : DbContext, IApplicationDbContext
{
    public KoinonDbContext(DbContextOptions<KoinonDbContext> options)
        : base(options)
    {
    }

    // Core entity DbSets
    public DbSet<Person> People { get; set; } = null!;
    public DbSet<PersonAlias> PersonAliases { get; set; } = null!;
    public DbSet<PhoneNumber> PhoneNumbers { get; set; } = null!;

    // Group-related entities
    public DbSet<Group> Groups { get; set; } = null!;
    public DbSet<GroupType> GroupTypes { get; set; } = null!;
    public DbSet<GroupTypeRole> GroupTypeRoles { get; set; } = null!;
    public DbSet<GroupMember> GroupMembers { get; set; } = null!;

    // Organization entities
    public DbSet<Campus> Campuses { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;

    // Defined types and values
    public DbSet<DefinedType> DefinedTypes { get; set; } = null!;
    public DbSet<DefinedValue> DefinedValues { get; set; } = null!;

    // Attendance and check-in entities
    public DbSet<Schedule> Schedules { get; set; } = null!;
    public DbSet<Attendance> Attendances { get; set; } = null!;
    public DbSet<AttendanceOccurrence> AttendanceOccurrences { get; set; } = null!;
    public DbSet<AttendanceCode> AttendanceCodes { get; set; } = null!;
    public DbSet<Device> Devices { get; set; } = null!;

    // Authentication entities
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<SupervisorSession> SupervisorSessions { get; set; } = null!;
    public DbSet<SupervisorAuditLog> SupervisorAuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration implementations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KoinonDbContext).Assembly);

        // Apply PostgreSQL-specific configurations (full-text search, PostGIS, etc.)
        // Only apply if using PostgreSQL provider
        if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            modelBuilder.ApplyPostgreSqlConfigurations();
        }

        // Global query filters for soft delete
        // Groups and GroupMembers use IsArchived for soft delete
        modelBuilder.Entity<Group>()
            .HasQueryFilter(g => !g.IsArchived);

        modelBuilder.Entity<GroupMember>()
            .HasQueryFilter(gm => !gm.IsArchived);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Note: Individual configurations explicitly set snake_case table and column names.
        // This method is available for future convention-based customizations if needed.
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-populate NumberNormalized for PhoneNumber entities
        var phoneEntries = ChangeTracker.Entries<PhoneNumber>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in phoneEntries)
        {
            entry.Entity.NumberNormalized = new string(
                entry.Entity.Number?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());
        }

        // Auto-populate IssueDate for AttendanceCode entities
        var attendanceCodeEntries = ChangeTracker.Entries<AttendanceCode>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in attendanceCodeEntries)
        {
            entry.Entity.IssueDate = DateOnly.FromDateTime(entry.Entity.IssueDateTime);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        // Auto-populate NumberNormalized for PhoneNumber entities
        var phoneEntries = ChangeTracker.Entries<PhoneNumber>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in phoneEntries)
        {
            entry.Entity.NumberNormalized = new string(
                entry.Entity.Number?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());
        }

        // Auto-populate IssueDate for AttendanceCode entities
        var attendanceCodeEntries = ChangeTracker.Entries<AttendanceCode>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in attendanceCodeEntries)
        {
            entry.Entity.IssueDate = DateOnly.FromDateTime(entry.Entity.IssueDateTime);
        }

        return base.SaveChanges();
    }
}
