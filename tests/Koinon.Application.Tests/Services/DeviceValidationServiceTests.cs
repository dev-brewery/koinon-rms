using Koinon.Application.Interfaces;
using Koinon.Application.Services;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

public class DeviceValidationServiceTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<DeviceValidationService>> _mockLogger;
    private readonly DeviceValidationService _service;

    public DeviceValidationServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<DeviceValidationService>>();

        _service = new DeviceValidationService(
            _context,
            _mockCache.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ValidateKioskTokenAsync_WithValidToken_ReturnsDeviceId()
    {
        // Arrange
        var token = "valid-test-token-12345";
        var device = new Device
        {
            Name = "Test Kiosk",
            IsActive = true,
            KioskToken = token,
            KioskTokenExpiresAt = DateTime.UtcNow.AddDays(30),
            Guid = Guid.NewGuid(),
            CreatedDateTime = DateTime.UtcNow
        };

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateKioskTokenAsync(token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(device.Id, result.Value);
    }

    [Fact]
    public async Task ValidateKioskTokenAsync_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid-token";

        // Act
        var result = await _service.ValidateKioskTokenAsync(invalidToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateKioskTokenAsync_WithInactiveDevice_ReturnsNull()
    {
        // Arrange
        var token = "inactive-device-token";
        var device = new Device
        {
            Name = "Inactive Kiosk",
            IsActive = false,
            KioskToken = token,
            Guid = Guid.NewGuid(),
            CreatedDateTime = DateTime.UtcNow
        };

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateKioskTokenAsync(token);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateKioskTokenAsync_WithExpiredToken_ReturnsNull()
    {
        // Arrange
        var token = "expired-token";
        var device = new Device
        {
            Name = "Expired Token Kiosk",
            IsActive = true,
            KioskToken = token,
            KioskTokenExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            Guid = Guid.NewGuid(),
            CreatedDateTime = DateTime.UtcNow
        };

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateKioskTokenAsync(token);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateKioskTokenAsync_WithNullOrEmptyToken_ReturnsNull()
    {
        // Act & Assert
        Assert.Null(await _service.ValidateKioskTokenAsync(null!));
        Assert.Null(await _service.ValidateKioskTokenAsync(""));
        Assert.Null(await _service.ValidateKioskTokenAsync("   "));
    }

    [Fact]
    public async Task GenerateKioskTokenAsync_CreatesNewToken()
    {
        // Arrange
        var device = new Device
        {
            Name = "Test Kiosk",
            IsActive = true,
            Guid = Guid.NewGuid(),
            CreatedDateTime = DateTime.UtcNow
        };

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        var deviceIdKey = device.IdKey;

        // Act
        var token = await _service.GenerateKioskTokenAsync(deviceIdKey);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Equal(128, token.Length); // 64 bytes = 128 hex characters

        // Verify token is stored in database
        var updatedDevice = await _context.Devices.FindAsync(device.Id);
        Assert.NotNull(updatedDevice);
        Assert.Equal(token, updatedDevice.KioskToken);
    }

    [Fact]
    public async Task GenerateKioskTokenAsync_WithInvalidIdKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GenerateKioskTokenAsync("invalid-idkey"));
    }

    [Fact]
    public async Task GenerateKioskTokenAsync_WithNonExistentDevice_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentIdKey = IdKeyHelper.Encode(99999);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GenerateKioskTokenAsync(nonExistentIdKey));
    }

    [Fact]
    public async Task RevokeKioskTokenAsync_ClearsToken()
    {
        // Arrange
        var device = new Device
        {
            Name = "Test Kiosk",
            IsActive = true,
            KioskToken = "token-to-revoke",
            Guid = Guid.NewGuid(),
            CreatedDateTime = DateTime.UtcNow
        };

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        var deviceIdKey = device.IdKey;

        // Act
        var result = await _service.RevokeKioskTokenAsync(deviceIdKey);

        // Assert
        Assert.True(result);

        // Verify token is cleared in database
        var updatedDevice = await _context.Devices.FindAsync(device.Id);
        Assert.NotNull(updatedDevice);
        Assert.Null(updatedDevice.KioskToken);
        Assert.Null(updatedDevice.KioskTokenExpiresAt);
    }

    [Fact]
    public async Task RevokeKioskTokenAsync_WithInvalidIdKey_ReturnsFalse()
    {
        // Act
        var result = await _service.RevokeKioskTokenAsync("invalid-idkey");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RevokeKioskTokenAsync_WithNonExistentDevice_ReturnsFalse()
    {
        // Arrange
        var nonExistentIdKey = IdKeyHelper.Encode(99999);

        // Act
        var result = await _service.RevokeKioskTokenAsync(nonExistentIdKey);

        // Assert
        Assert.False(result);
    }

    // Test DbContext for in-memory testing
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
        public DbSet<BinaryFile> BinaryFiles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Location self-referential relationships
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
}
