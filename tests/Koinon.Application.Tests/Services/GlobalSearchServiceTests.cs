using FluentAssertions;
using Koinon.Application.Services;
using Koinon.Domain.Entities;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

/// <summary>
/// Tests for GlobalSearchService.
/// </summary>
public class GlobalSearchServiceTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly Mock<ILogger<GlobalSearchService>> _mockLogger;
    private readonly GlobalSearchService _service;

    public GlobalSearchServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: $"GlobalSearchTestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new KoinonDbContext(options);
        _context.Database.EnsureCreated();

        // Setup logger mock
        _mockLogger = new Mock<ILogger<GlobalSearchService>>();

        // Create service
        _service = new GlobalSearchService(_context, _mockLogger.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add campus
        var campus = new Campus
        {
            Id = 1,
            Name = "Main Campus",
            ShortCode = "MAIN",
            IsActive = true,
            CreatedDateTime = DateTime.UtcNow,
            Guid = Guid.NewGuid()
        };
        _context.Campuses.Add(campus);

        // Add people
        var people = new[]
        {
            new Person
            {
                Id = 1,
                FirstName = "John",
                LastName = "Smith",
                Email = "john.smith@example.com",
                CreatedDateTime = DateTime.UtcNow,
                Guid = Guid.NewGuid()
            },
            new Person
            {
                Id = 2,
                FirstName = "Jane",
                LastName = "Doe",
                NickName = "JD",
                Email = "jane.doe@example.com",
                CreatedDateTime = DateTime.UtcNow,
                Guid = Guid.NewGuid()
            },
            new Person
            {
                Id = 3,
                FirstName = "Robert",
                LastName = "Johnson",
                Email = "robert.johnson@example.com",
                CreatedDateTime = DateTime.UtcNow,
                Guid = Guid.NewGuid()
            }
        };
        _context.People.AddRange(people);

        // Add families
        var families = new[]
        {
            new Family
            {
                Id = 1,
                Name = "Smith Family",
                CampusId = 1,
                CreatedDateTime = DateTime.UtcNow,
                Guid = Guid.NewGuid()
            },
            new Family
            {
                Id = 2,
                Name = "Doe Family",
                CampusId = 1,
                CreatedDateTime = DateTime.UtcNow,
                Guid = Guid.NewGuid()
            }
        };
        _context.Families.AddRange(families);

        // Add group type
        var groupType = new GroupType
        {
            Id = 1,
            Name = "Small Group",
            CreatedDateTime = DateTime.UtcNow,
            Guid = Guid.NewGuid()
        };
        _context.GroupTypes.Add(groupType);

        // Add groups
        var groups = new[]
        {
            new Group
            {
                Id = 1,
                Name = "Youth Group",
                Description = "A group for young adults",
                GroupTypeId = 1,
                CampusId = 1,
                CreatedDateTime = DateTime.UtcNow,
                Guid = Guid.NewGuid()
            },
            new Group
            {
                Id = 2,
                Name = "Bible Study",
                Description = "Weekly bible study for all ages",
                GroupTypeId = 1,
                CampusId = 1,
                CreatedDateTime = DateTime.UtcNow,
                Guid = Guid.NewGuid()
            }
        };
        _context.Groups.AddRange(groups);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsEmptyResults()
    {
        // Act
        var result = await _service.SearchAsync("");

        // Assert
        result.Results.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_WithWhitespaceQuery_ReturnsEmptyResults()
    {
        // Act
        var result = await _service.SearchAsync("   ");

        // Assert
        result.Results.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_FindsPersonByFirstName()
    {
        // Act
        var result = await _service.SearchAsync("John");

        // Assert
        // "John" matches both "John Smith" (first name) and "Robert Johnson" (last name contains John)
        result.Results.Should().Contain(r => r.Category == "People" && r.Title.StartsWith("John"));
        result.CategoryCounts.Should().ContainKey("People");
    }

    [Fact]
    public async Task SearchAsync_FindsPersonByLastName()
    {
        // Act
        var result = await _service.SearchAsync("Smith");

        // Assert
        result.Results.Should().Contain(r => r.Category == "People" && r.Title.Contains("Smith"));
    }

    [Fact]
    public async Task SearchAsync_FindsPersonByEmail()
    {
        // Act
        var result = await _service.SearchAsync("jane.doe@example.com");

        // Assert
        result.Results.Should().Contain(r => r.Category == "People" && r.Subtitle == "jane.doe@example.com");
    }

    [Fact]
    public async Task SearchAsync_FindsPersonByNickName()
    {
        // Act
        var result = await _service.SearchAsync("JD");

        // Assert
        result.Results.Should().Contain(r => r.Category == "People");
    }

    [Fact]
    public async Task SearchAsync_FindsFamilyByName()
    {
        // Act
        var result = await _service.SearchAsync("Smith Family");

        // Assert
        result.Results.Should().Contain(r => r.Category == "Families" && r.Title == "Smith Family");
        result.CategoryCounts.Should().ContainKey("Families");
    }

    [Fact]
    public async Task SearchAsync_FindsGroupByName()
    {
        // Act
        var result = await _service.SearchAsync("Youth Group");

        // Assert
        result.Results.Should().Contain(r => r.Category == "Groups" && r.Title == "Youth Group");
        result.CategoryCounts.Should().ContainKey("Groups");
    }

    [Fact]
    public async Task SearchAsync_FindsGroupByDescription()
    {
        // Act
        var result = await _service.SearchAsync("bible study");

        // Assert
        result.Results.Should().Contain(r => r.Category == "Groups" && r.Title.Contains("Bible Study"));
    }

    [Fact]
    public async Task SearchAsync_WithCategoryFilter_OnlyReturnsThatCategory()
    {
        // Act
        var result = await _service.SearchAsync("Smith", category: "People");

        // Assert
        result.Results.Should().AllSatisfy(r => r.Category.Should().Be("People"));
        result.CategoryCounts.Keys.Should().OnlyContain(k => k == "People");
    }

    [Fact]
    public async Task SearchAsync_ReturnsCategoryCounts()
    {
        // Act
        var result = await _service.SearchAsync("Smith");

        // Assert
        result.CategoryCounts.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SearchAsync_ReturnsPaginationInfo()
    {
        // Act
        var result = await _service.SearchAsync("e", pageNumber: 1, pageSize: 10);

        // Assert
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task SearchAsync_Pagination_SkipsAndTakesCorrectly()
    {
        // Act
        var page1 = await _service.SearchAsync("e", pageNumber: 1, pageSize: 1);
        var page2 = await _service.SearchAsync("e", pageNumber: 2, pageSize: 1);

        // Assert - if there are results, pagination should work
        if (page1.TotalCount > 1)
        {
            page1.Results.Should().HaveCount(1);
            if (page2.Results.Count > 0)
            {
                page1.Results[0].IdKey.Should().NotBe(page2.Results[0].IdKey);
            }
        }
    }

    [Fact]
    public async Task SearchAsync_EnforcesMaxPageSize()
    {
        // Act
        var result = await _service.SearchAsync("e", pageSize: 200);

        // Assert - max page size is 100
        result.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task SearchAsync_IsCaseInsensitive()
    {
        // Act
        var lowerResult = await _service.SearchAsync("john");
        var upperResult = await _service.SearchAsync("JOHN");
        var mixedResult = await _service.SearchAsync("JoHn");

        // Assert
        lowerResult.TotalCount.Should().Be(upperResult.TotalCount);
        lowerResult.TotalCount.Should().Be(mixedResult.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_ReturnsIdKeyNotIntegerId()
    {
        // Act
        var result = await _service.SearchAsync("John");

        // Assert
        result.Results.Should().AllSatisfy(r =>
        {
            r.IdKey.Should().NotBeNullOrEmpty();
            // IdKey should not be a plain integer
            int.TryParse(r.IdKey, out _).Should().BeFalse();
        });
    }

    [Fact]
    public async Task SearchAsync_NoResults_ReturnsEmptyResponse()
    {
        // Act
        var result = await _service.SearchAsync("XyzNonExistent123");

        // Assert
        result.Results.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.CategoryCounts.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_PartialMatch_FindsResults()
    {
        // Act - search for "Smi" should find "Smith"
        var result = await _service.SearchAsync("Smi");

        // Assert
        result.Results.Should().NotBeEmpty();
        result.Results.Should().Contain(r => r.Title.Contains("Smith") || r.Title.Contains("Smith Family"));
    }
}
