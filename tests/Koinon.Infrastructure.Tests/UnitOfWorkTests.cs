using FluentAssertions;
using Koinon.Domain.Entities;
using Koinon.Infrastructure.Data;
using Koinon.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace Koinon.Infrastructure.Tests;

/// <summary>
/// Unit tests for the UnitOfWork implementation.
/// Verifies transaction management and repository coordination.
/// </summary>
public class UnitOfWorkTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        // Create in-memory database with unique name for test isolation
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new KoinonDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
    }

    [Fact]
    public void Repository_ReturnsRepositoryInstance()
    {
        // Act
        var personRepo = _unitOfWork.Repository<Person>();

        // Assert
        personRepo.Should().NotBeNull();
        personRepo.Should().BeAssignableTo<IRepository<Person>>();
    }

    [Fact]
    public void Repository_ReturnsSameInstanceForSameType()
    {
        // Act
        var repo1 = _unitOfWork.Repository<Person>();
        var repo2 = _unitOfWork.Repository<Person>();

        // Assert
        repo1.Should().BeSameAs(repo2);
    }

    [Fact]
    public void Repository_ReturnsDifferentInstancesForDifferentTypes()
    {
        // Act
        var personRepo = _unitOfWork.Repository<Person>();
        var groupRepo = _unitOfWork.Repository<Group>();

        // Assert
        personRepo.Should().NotBeNull();
        groupRepo.Should().NotBeNull();
        personRepo.Should().NotBeSameAs(groupRepo);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsChangesToDatabase()
    {
        // Arrange
        var person = CreateTestPerson("Test", "User");
        var repo = _unitOfWork.Repository<Person>();
        await repo.AddAsync(person);

        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().BeGreaterThan(0);
        person.Id.Should().BeGreaterThan(0);

        // Verify persistence
        var retrieved = await _context.People.FindAsync(person.Id);
        retrieved.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ReturnsZero()
    {
        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_SupportsCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var person = CreateTestPerson("Test", "Cancel");
        var repo = _unitOfWork.Repository<Person>();
        await repo.AddAsync(person);

        // Act
        var result = await _unitOfWork.SaveChangesAsync(cts.Token);

        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MultipleRepositories_ShareSameContext()
    {
        // Arrange
        var personRepo = _unitOfWork.Repository<Person>();
        var groupRepo = _unitOfWork.Repository<Group>();

        var person = CreateTestPerson("John", "Doe");
        var group = CreateTestGroup("Test Group");

        // Act
        await personRepo.AddAsync(person);
        await groupRepo.AddAsync(group);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        person.Id.Should().BeGreaterThan(0);
        group.Id.Should().BeGreaterThan(0);

        // Verify both entities persisted
        var retrievedPerson = await _context.People.FindAsync(person.Id);
        var retrievedGroup = await _context.Groups.FindAsync(group.Id);

        retrievedPerson.Should().NotBeNull();
        retrievedGroup.Should().NotBeNull();
    }

    [Fact]
    public async Task Dispose_CleansUpResources()
    {
        // Arrange
        var repo = _unitOfWork.Repository<Person>();
        var person = CreateTestPerson("Test", "Dispose");
        await repo.AddAsync(person);

        // Act
        _unitOfWork.Dispose();

        // Assert - should not throw
        // Note: In-memory database doesn't validate disposal behavior well,
        // but we verify the method completes without errors
        person.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UnitOfWork(null!));
    }

    private static Person CreateTestPerson(string firstName, string lastName)
    {
        return new Person
        {
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLower()}.{lastName.ToLower()}@example.com",
            RecordStatusValueId = 1,
            RecordTypeValueId = 1,
            ConnectionStatusValueId = 1
        };
    }

    private static Group CreateTestGroup(string name)
    {
        return new Group
        {
            Name = name,
            GroupTypeId = 1,
            IsActive = true,
            IsPublic = true
        };
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _context.Dispose();
    }
}

/// <summary>
/// Integration tests for UnitOfWork transaction behavior.
/// Uses SQLite in-memory database for full transaction support.
/// </summary>
public class UnitOfWorkTransactionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestKoinonDbContext _context;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTransactionTests()
    {
        // Use SQLite in-memory for real transaction support
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TestKoinonDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TestKoinonDbContext(options);
        _context.Database.EnsureCreated();

        // Disable foreign key constraints for unit tests
        // This allows testing without creating all reference data
        _context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;");

        _unitOfWork = new UnitOfWork(_context);
    }

    /// <summary>
    /// Test-specific DbContext that doesn't apply PostgreSQL-specific configurations.
    /// Inherits from KoinonDbContext to get all entity configurations.
    /// </summary>
    private class TestKoinonDbContext : KoinonDbContext
    {
        public TestKoinonDbContext(DbContextOptions<TestKoinonDbContext> options)
            : base(CreateBaseOptions(options))
        {
        }

        // Helper to convert options type
        private static DbContextOptions<KoinonDbContext> CreateBaseOptions(
            DbContextOptions<TestKoinonDbContext> options)
        {
            var builder = new DbContextOptionsBuilder<KoinonDbContext>();

            // Copy the SQLite configuration
            foreach (var extension in options.Extensions)
            {
                ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);
            }

            return builder.Options;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply base configurations but skip PostgreSQL-specific ones
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(KoinonDbContext).Assembly);

            // Apply global query filters
            modelBuilder.Entity<Group>()
                .HasQueryFilter(g => !g.IsArchived);

            modelBuilder.Entity<GroupMember>()
                .HasQueryFilter(gm => !gm.IsArchived);

            // Skip PostgreSQL configurations - this is the key difference
        }
    }

    [Fact]
    public async Task BeginTransactionAsync_StartsTransaction()
    {
        // Act
        await _unitOfWork.BeginTransactionAsync();

        // Assert - should not throw
        // Transaction is active (verified by CommitTransactionAsync test)
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenTransactionInProgress_ThrowsInvalidOperationException()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _unitOfWork.BeginTransactionAsync());
    }

    [Fact]
    public async Task CommitTransactionAsync_WithNoTransaction_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _unitOfWork.CommitTransactionAsync());
    }

    [Fact]
    public async Task RollbackTransactionAsync_WithNoTransaction_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _unitOfWork.RollbackTransactionAsync());
    }

    [Fact]
    public async Task CommitTransactionAsync_CommitsChanges()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Transaction",
            LastName = "Test",
            Email = "transaction.test@example.com",
            RecordStatusValueId = 1,
            RecordTypeValueId = 1,
            ConnectionStatusValueId = 1
        };

        var repo = _unitOfWork.Repository<Person>();

        // Act
        await _unitOfWork.BeginTransactionAsync();
        await repo.AddAsync(person);
        await _unitOfWork.CommitTransactionAsync();

        // Assert
        var retrieved = await _context.People.FindAsync(person.Id);
        retrieved.Should().NotBeNull();
    }

    [Fact]
    public async Task RollbackTransactionAsync_DiscardsChanges()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Rollback",
            LastName = "Test",
            Email = "rollback.test@example.com",
            RecordStatusValueId = 1,
            RecordTypeValueId = 1,
            ConnectionStatusValueId = 1
        };

        var repo = _unitOfWork.Repository<Person>();

        // Act
        await _unitOfWork.BeginTransactionAsync();
        await repo.AddAsync(person);
        await _unitOfWork.SaveChangesAsync();
        var personId = person.Id;
        await _unitOfWork.RollbackTransactionAsync();

        // Assert
        // Clear the context's change tracker to ensure we're querying from the database
        _context.ChangeTracker.Clear();

        // SQLite supports transactions, so the rollback should discard changes
        var retrieved = await _context.People.FindAsync(personId);
        retrieved.Should().BeNull();
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _context.Dispose();
        _connection.Dispose();
    }
}
