using FluentAssertions;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Infrastructure.Data;
using Koinon.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Koinon.Infrastructure.Tests.Repositories;

/// <summary>
/// Unit tests for the generic Repository implementation.
/// Uses in-memory database for fast, isolated testing.
/// </summary>
public class RepositoryTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly Repository<Person> _repository;

    public RepositoryTests()
    {
        // Create in-memory database with unique name for test isolation
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new KoinonDbContext(options);
        _repository = new Repository<Person>(_context);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityExists_ReturnsEntity()
    {
        // Arrange
        var person = CreateTestPerson("John", "Doe");
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(person.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(person.Id);
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdKeyAsync_WhenValidIdKey_ReturnsEntity()
    {
        // Arrange
        var person = CreateTestPerson("Jane", "Smith");
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        var idKey = IdKeyHelper.Encode(person.Id);

        // Act
        var result = await _repository.GetByIdKeyAsync(idKey);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(person.Id);
        result.FirstName.Should().Be("Jane");
    }

    [Fact]
    public async Task GetByIdKeyAsync_WhenInvalidIdKey_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdKeyAsync("invalid-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdKeyAsync_WhenEmptyIdKey_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdKeyAsync(string.Empty);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByGuidAsync_WhenEntityExists_ReturnsEntity()
    {
        // Arrange
        var person = CreateTestPerson("Bob", "Johnson");
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByGuidAsync(person.Guid);

        // Assert
        result.Should().NotBeNull();
        result!.Guid.Should().Be(person.Guid);
        result.FirstName.Should().Be("Bob");
    }

    [Fact]
    public async Task GetByGuidAsync_WhenEntityDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByGuidAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var people = new[]
        {
            CreateTestPerson("Alice", "Williams"),
            CreateTestPerson("Charlie", "Brown"),
            CreateTestPerson("David", "Jones")
        };

        await _context.People.AddRangeAsync(people);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(p => p.FirstName == "Alice");
        result.Should().Contain(p => p.FirstName == "Charlie");
        result.Should().Contain(p => p.FirstName == "David");
    }

    [Fact]
    public async Task GetAllAsync_WhenNoEntities_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindAsync_WithMatchingPredicate_ReturnsMatchingEntities()
    {
        // Arrange
        var people = new[]
        {
            CreateTestPerson("John", "Smith"),
            CreateTestPerson("Jane", "Smith"),
            CreateTestPerson("Bob", "Jones")
        };

        await _context.People.AddRangeAsync(people);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindAsync(p => p.LastName == "Smith");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.FirstName == "John");
        result.Should().Contain(p => p.FirstName == "Jane");
    }

    [Fact]
    public async Task FindAsync_WithNonMatchingPredicate_ReturnsEmptyList()
    {
        // Arrange
        var person = CreateTestPerson("Test", "User");
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindAsync(p => p.LastName == "NonExistent");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Query_ReturnsQueryableWithNoTracking()
    {
        // Arrange
        var people = new[]
        {
            CreateTestPerson("Alice", "Test"),
            CreateTestPerson("Bob", "Test")
        };

        _context.People.AddRange(people);
        _context.SaveChanges();

        // Act
        var query = _repository.Query();

        // Assert
        query.Should().NotBeNull();
        query.Should().BeAssignableTo<IQueryable<Person>>();

        // Verify it returns the correct data
        var result = query.Where(p => p.LastName == "Test").ToList();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddAsync_AddsEntityAndSetsCreatedDateTime()
    {
        // Arrange
        var person = CreateTestPerson("New", "Person");
        var beforeAdd = DateTime.UtcNow;

        // Act
        var result = await _repository.AddAsync(person);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.CreatedDateTime.Should().BeOnOrAfter(beforeAdd);
        result.CreatedDateTime.Should().BeOnOrBefore(DateTime.UtcNow);

        // Verify it's in the database
        var retrieved = await _context.People.FindAsync(result.Id);
        retrieved.Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_WithNullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _repository.AddAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntityAndSetsModifiedDateTime()
    {
        // Arrange
        var person = CreateTestPerson("Original", "Name");
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Detach to simulate retrieving from database
        _context.Entry(person).State = EntityState.Detached;

        // Modify the person
        person.FirstName = "Updated";
        var beforeUpdate = DateTime.UtcNow;

        // Act
        await _repository.UpdateAsync(person);
        await _context.SaveChangesAsync();

        // Assert
        person.ModifiedDateTime.Should().NotBeNull();
        person.ModifiedDateTime.Should().BeOnOrAfter(beforeUpdate);

        // Verify the change persisted
        var retrieved = await _context.People.FindAsync(person.Id);
        retrieved!.FirstName.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateAsync_WithNullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        // Arrange
        var person = CreateTestPerson("To", "Delete");
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        var personId = person.Id;

        // Act
        await _repository.DeleteAsync(person);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _context.People.FindAsync(personId);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _repository.DeleteAsync(null!));
    }

    [Fact]
    public async Task ExistsAsync_WhenEntityExists_ReturnsTrue()
    {
        // Arrange
        var person = CreateTestPerson("Exists", "Test");
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(person.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenEntityDoesNotExist_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var people = new[]
        {
            CreateTestPerson("One", "Test"),
            CreateTestPerson("Two", "Test"),
            CreateTestPerson("Three", "Test")
        };

        await _context.People.AddRangeAsync(people);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task CountAsync_WhenNoEntities_ReturnsZero()
    {
        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task Repository_SupportsCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var person = CreateTestPerson("Test", "Cancel");
        await _context.People.AddAsync(person);
        await _context.SaveChangesAsync();

        // Act - verify methods accept cancellation token without throwing
        var getByIdTask = _repository.GetByIdAsync(person.Id, cts.Token);
        var getAllTask = _repository.GetAllAsync(cts.Token);
        var countTask = _repository.CountAsync(cts.Token);

        // Assert
        await getByIdTask;
        await getAllTask;
        await countTask;
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

    public void Dispose()
    {
        _context.Dispose();
    }
}
