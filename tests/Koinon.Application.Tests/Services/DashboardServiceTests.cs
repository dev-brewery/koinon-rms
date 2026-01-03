using FluentAssertions;
using Koinon.Application.Services;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

/// <summary>
/// Tests for DashboardService.
/// </summary>
public class DashboardServiceTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly Mock<ILogger<DashboardService>> _mockLogger;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: $"KoinonTestDb_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new KoinonDbContext(options);

        // Manually initialize the context
        _context.Database.EnsureCreated();

        // Setup logger mock
        _mockLogger = new Mock<ILogger<DashboardService>>();

        // Create service
        _service = new DashboardService(_context, _mockLogger.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // Add group types (family and non-family)
        var familyGroupType = new GroupType
        {
            Id = 1,
            Name = "Family",
            AllowMultipleLocations = false,
            IsSystem = true,
            CreatedDateTime = now
        };

        var servingGroupType = new GroupType
        {
            Id = 2,
            Name = "Serving Team",
            AllowMultipleLocations = false,
            IsSystem = false,
            CreatedDateTime = now
        };

        _context.GroupTypes.AddRange(familyGroupType, servingGroupType);

        // Add people (some deceased)
        var people = new List<Person>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe", IsDeceased = false, CreatedDateTime = now },
            new() { Id = 2, FirstName = "Jane", LastName = "Smith", IsDeceased = false, CreatedDateTime = now },
            new() { Id = 3, FirstName = "Bob", LastName = "Johnson", IsDeceased = false, CreatedDateTime = now },
            new() { Id = 4, FirstName = "Deceased", LastName = "Person", IsDeceased = true, CreatedDateTime = now }
        };
        _context.People.AddRange(people);

        // Add families using Family entity (not Group)
        var families = new List<Family>
        {
            new() { Id = 1, Name = "Doe Family", IsActive = true, CreatedDateTime = now },
            new() { Id = 2, Name = "Smith Family", IsActive = true, CreatedDateTime = now },
            new() { Id = 3, Name = "Archived Family", IsActive = false, CreatedDateTime = now }
        };
        _context.Families.AddRange(families);

        // Add non-family groups (should be counted in ActiveGroups)
        var groups = new List<Group>
        {
            new() { Id = 4, GroupTypeId = 2, Name = "Worship Team", IsActive = true, IsArchived = false, CreatedDateTime = now },
            new() { Id = 5, GroupTypeId = 2, Name = "Audio Team", IsActive = true, IsArchived = false, CreatedDateTime = now },
            new() { Id = 6, GroupTypeId = 2, Name = "Inactive Team", IsActive = false, IsArchived = false, CreatedDateTime = now },
            new() { Id = 7, GroupTypeId = 2, Name = "Archived Team", IsActive = true, IsArchived = true, ArchivedDateTime = now, CreatedDateTime = now }
        };
        _context.Groups.AddRange(groups);

        // Add schedules (active and inactive)
        var schedules = new List<Schedule>
        {
            new()
            {
                Id = 1,
                Name = "Sunday Morning",
                IsActive = true,
                WeeklyDayOfWeek = DayOfWeek.Sunday,
                WeeklyTimeOfDay = TimeSpan.FromHours(10),
                CheckInStartOffsetMinutes = 60,
                Order = 0,
                CreatedDateTime = now
            },
            new()
            {
                Id = 2,
                Name = "Wednesday Evening",
                IsActive = true,
                WeeklyDayOfWeek = DayOfWeek.Wednesday,
                WeeklyTimeOfDay = TimeSpan.FromHours(19),
                CheckInStartOffsetMinutes = 30,
                Order = 1,
                CreatedDateTime = now
            },
            new()
            {
                Id = 3,
                Name = "Inactive Schedule",
                IsActive = false,
                WeeklyDayOfWeek = DayOfWeek.Saturday,
                WeeklyTimeOfDay = TimeSpan.FromHours(18),
                Order = 2,
                CreatedDateTime = now
            }
        };
        _context.Schedules.AddRange(schedules);

        // Add attendances (check-ins for today and last week)
        var todayStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var lastWeek = today.AddDays(-7);
        var lastWeekStart = lastWeek.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var attendances = new List<Attendance>
        {
            // Today's check-ins
            new() { Id = 1, PersonAliasId = 1, OccurrenceId = 1, StartDateTime = todayStart.AddHours(9), CreatedDateTime = now },
            new() { Id = 2, PersonAliasId = 2, OccurrenceId = 1, StartDateTime = todayStart.AddHours(10), CreatedDateTime = now },
            new() { Id = 3, PersonAliasId = 3, OccurrenceId = 1, StartDateTime = todayStart.AddHours(11), CreatedDateTime = now },

            // Last week's check-ins (same day)
            new() { Id = 4, PersonAliasId = 1, OccurrenceId = 2, StartDateTime = lastWeekStart.AddHours(9), CreatedDateTime = now.AddDays(-7) },
            new() { Id = 5, PersonAliasId = 2, OccurrenceId = 2, StartDateTime = lastWeekStart.AddHours(10), CreatedDateTime = now.AddDays(-7) },

            // Other days (should not be counted)
            new() { Id = 6, PersonAliasId = 1, OccurrenceId = 3, StartDateTime = todayStart.AddDays(-2).AddHours(9), CreatedDateTime = now.AddDays(-2) }
        };
        _context.Attendances.AddRange(attendances);

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsCorrectPeopleCounts()
    {
        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.TotalPeople.Should().Be(3); // Excludes deceased person
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsCorrectFamilyCounts()
    {
        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.TotalFamilies.Should().Be(2); // Excludes archived family
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsCorrectGroupCounts()
    {
        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.ActiveGroups.Should().Be(2); // Only active, non-archived, non-family groups
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsCorrectScheduleCounts()
    {
        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.ActiveSchedules.Should().Be(2); // Only active schedules
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsCorrectCheckInCounts()
    {
        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.TodayCheckIns.Should().Be(3); // Check-ins for today
        stats.LastWeekCheckIns.Should().Be(2); // Check-ins for same day last week
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsUpcomingSchedules_SortedByNextOccurrence()
    {
        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.UpcomingSchedules.Should().NotBeNull();
        stats.UpcomingSchedules.Should().HaveCountLessOrEqualTo(5); // Maximum 5 schedules
        stats.UpcomingSchedules.Should().HaveCount(2); // We have 2 active schedules with weekly times

        // Verify schedules are sorted by next occurrence
        for (int i = 0; i < stats.UpcomingSchedules.Count - 1; i++)
        {
            stats.UpcomingSchedules[i].NextOccurrence.Should().BeOnOrBefore(stats.UpcomingSchedules[i + 1].NextOccurrence);
        }

        // Verify all schedules have valid data
        foreach (var schedule in stats.UpcomingSchedules)
        {
            schedule.IdKey.Should().NotBeNullOrEmpty();
            schedule.Name.Should().NotBeNullOrEmpty();
            schedule.NextOccurrence.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1)); // Should be in the future or very close to now
        }
    }

    [Fact]
    public async Task GetStatsAsync_WithNoData_ReturnsZeroCounts()
    {
        // Arrange - Clear all data
        _context.People.RemoveRange(_context.People);
        _context.Families.RemoveRange(_context.Families);
        _context.Groups.RemoveRange(_context.Groups);
        _context.Schedules.RemoveRange(_context.Schedules);
        _context.Attendances.RemoveRange(_context.Attendances);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.TotalPeople.Should().Be(0);
        stats.TotalFamilies.Should().Be(0);
        stats.ActiveGroups.Should().Be(0);
        stats.ActiveSchedules.Should().Be(0);
        stats.TodayCheckIns.Should().Be(0);
        stats.LastWeekCheckIns.Should().Be(0);
        stats.UpcomingSchedules.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatsAsync_UpcomingSchedules_CalculatesMinutesUntilCheckIn()
    {
        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.UpcomingSchedules.Should().NotBeEmpty();

        foreach (var schedule in stats.UpcomingSchedules)
        {
            // MinutesUntilCheckIn should be calculated based on NextOccurrence minus CheckInStartOffsetMinutes
            schedule.MinutesUntilCheckIn.Should().NotBe(0); // Should have a calculated value
        }
    }

    #region Giving Stats Tests

    [Fact]
    public async Task GetStatsAsync_GivingStats_CalculatesMTDTotalCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var currentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonth = currentMonth.AddMonths(-1);

        // Add defined values for transaction type and source
        var transactionType = new DefinedValue { Id = 100, Value = "Cash", DefinedTypeId = 1, CreatedDateTime = now };
        var sourceType = new DefinedValue { Id = 101, Value = "Website", DefinedTypeId = 2, CreatedDateTime = now };
        _context.DefinedValues.AddRange(transactionType, sourceType);

        // Add a batch
        var batch = new ContributionBatch
        {
            Id = 1,
            Name = "Test Batch",
            BatchDate = currentMonth,
            Status = BatchStatus.Open,
            CreatedDateTime = now
        };
        _context.ContributionBatches.Add(batch);

        // Add fund
        var fund = new Fund { Id = 1, Name = "General Fund", IsActive = true, CreatedDateTime = now };
        _context.Funds.Add(fund);

        // Add contributions - some in current month, some in previous month
        var contribution1 = new Contribution
        {
            Id = 1,
            TransactionDateTime = currentMonth.AddDays(5),
            TransactionTypeValueId = 100,
            SourceTypeValueId = 101,
            BatchId = 1,
            CreatedDateTime = now
        };
        var contribution2 = new Contribution
        {
            Id = 2,
            TransactionDateTime = currentMonth.AddDays(10),
            TransactionTypeValueId = 100,
            SourceTypeValueId = 101,
            BatchId = 1,
            CreatedDateTime = now
        };
        var contribution3 = new Contribution
        {
            Id = 3,
            TransactionDateTime = lastMonth.AddDays(5),
            TransactionTypeValueId = 100,
            SourceTypeValueId = 101,
            CreatedDateTime = now
        };
        _context.Contributions.AddRange(contribution1, contribution2, contribution3);

        // Add contribution details
        var detail1 = new ContributionDetail { Id = 1, ContributionId = 1, FundId = 1, Amount = 100.00m, CreatedDateTime = now };
        var detail2 = new ContributionDetail { Id = 2, ContributionId = 2, FundId = 1, Amount = 250.00m, CreatedDateTime = now };
        var detail3 = new ContributionDetail { Id = 3, ContributionId = 3, FundId = 1, Amount = 500.00m, CreatedDateTime = now }; // Last month - should not count
        _context.ContributionDetails.AddRange(detail1, detail2, detail3);

        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.GivingStats.Should().NotBeNull();
        stats.GivingStats.MonthToDateTotal.Should().Be(350.00m); // Only current month contributions
    }

    [Fact]
    public async Task GetStatsAsync_GivingStats_CalculatesYTDTotalCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var currentYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastYear = currentYear.AddYears(-1);

        // Add defined values
        var transactionType = new DefinedValue { Id = 100, Value = "Cash", DefinedTypeId = 1, CreatedDateTime = now };
        var sourceType = new DefinedValue { Id = 101, Value = "Website", DefinedTypeId = 2, CreatedDateTime = now };
        _context.DefinedValues.AddRange(transactionType, sourceType);

        // Add fund
        var fund = new Fund { Id = 1, Name = "General Fund", IsActive = true, CreatedDateTime = now };
        _context.Funds.Add(fund);

        // Add contributions - some in current year, some in previous year
        var contribution1 = new Contribution
        {
            Id = 1,
            TransactionDateTime = currentYear.AddMonths(1),
            TransactionTypeValueId = 100,
            SourceTypeValueId = 101,
            CreatedDateTime = now
        };
        var contribution2 = new Contribution
        {
            Id = 2,
            TransactionDateTime = currentYear.AddMonths(3),
            TransactionTypeValueId = 100,
            SourceTypeValueId = 101,
            CreatedDateTime = now
        };
        var contribution3 = new Contribution
        {
            Id = 3,
            TransactionDateTime = lastYear.AddMonths(6),
            TransactionTypeValueId = 100,
            SourceTypeValueId = 101,
            CreatedDateTime = now
        };
        _context.Contributions.AddRange(contribution1, contribution2, contribution3);

        // Add contribution details
        var detail1 = new ContributionDetail { Id = 1, ContributionId = 1, FundId = 1, Amount = 100.00m, CreatedDateTime = now };
        var detail2 = new ContributionDetail { Id = 2, ContributionId = 2, FundId = 1, Amount = 250.00m, CreatedDateTime = now };
        var detail3 = new ContributionDetail { Id = 3, ContributionId = 3, FundId = 1, Amount = 1000.00m, CreatedDateTime = now }; // Last year - should not count
        _context.ContributionDetails.AddRange(detail1, detail2, detail3);

        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.GivingStats.Should().NotBeNull();
        stats.GivingStats.YearToDateTotal.Should().Be(350.00m); // Only current year contributions
    }

    [Fact]
    public async Task GetStatsAsync_GivingStats_ReturnsLast5BatchesOrderedByDate()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Add 7 batches with different dates
        var batches = new List<ContributionBatch>();
        for (int i = 1; i <= 7; i++)
        {
            batches.Add(new ContributionBatch
            {
                Id = i,
                Name = $"Batch {i}",
                BatchDate = now.AddDays(-i),
                Status = i % 2 == 0 ? BatchStatus.Closed : BatchStatus.Open,
                CreatedDateTime = now
            });
        }
        _context.ContributionBatches.AddRange(batches);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.GivingStats.Should().NotBeNull();
        stats.GivingStats.RecentBatches.Should().HaveCount(5); // Maximum 5 batches

        // Verify ordering by BatchDate descending (most recent first)
        for (int i = 0; i < stats.GivingStats.RecentBatches.Count - 1; i++)
        {
            stats.GivingStats.RecentBatches[i].BatchDate.Should().BeOnOrAfter(stats.GivingStats.RecentBatches[i + 1].BatchDate);
        }
    }

    [Fact]
    public async Task GetStatsAsync_GivingStats_CalculatesBatchTotalsCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Add defined values
        var transactionType = new DefinedValue { Id = 100, Value = "Cash", DefinedTypeId = 1, CreatedDateTime = now };
        var sourceType = new DefinedValue { Id = 101, Value = "Website", DefinedTypeId = 2, CreatedDateTime = now };
        _context.DefinedValues.AddRange(transactionType, sourceType);

        // Add batch
        var batch = new ContributionBatch
        {
            Id = 1,
            Name = "Test Batch",
            BatchDate = now,
            Status = BatchStatus.Open,
            CreatedDateTime = now
        };
        _context.ContributionBatches.Add(batch);

        // Add fund
        var fund = new Fund { Id = 1, Name = "General Fund", IsActive = true, CreatedDateTime = now };
        _context.Funds.Add(fund);

        // Add contributions to the batch
        var contribution1 = new Contribution
        {
            Id = 1,
            TransactionDateTime = now,
            TransactionTypeValueId = 100,
            SourceTypeValueId = 101,
            BatchId = 1,
            CreatedDateTime = now
        };
        var contribution2 = new Contribution
        {
            Id = 2,
            TransactionDateTime = now,
            TransactionTypeValueId = 100,
            SourceTypeValueId = 101,
            BatchId = 1,
            CreatedDateTime = now
        };
        _context.Contributions.AddRange(contribution1, contribution2);

        // Add contribution details
        var detail1 = new ContributionDetail { Id = 1, ContributionId = 1, FundId = 1, Amount = 125.50m, CreatedDateTime = now };
        var detail2 = new ContributionDetail { Id = 2, ContributionId = 2, FundId = 1, Amount = 274.50m, CreatedDateTime = now };
        _context.ContributionDetails.AddRange(detail1, detail2);

        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.GivingStats.Should().NotBeNull();
        stats.GivingStats.RecentBatches.Should().HaveCount(1);
        stats.GivingStats.RecentBatches[0].Total.Should().Be(400.00m); // Sum of both contributions
        stats.GivingStats.RecentBatches[0].IdKey.Should().NotBeNullOrEmpty();
        stats.GivingStats.RecentBatches[0].Name.Should().Be("Test Batch");
        stats.GivingStats.RecentBatches[0].Status.Should().Be("Open");
    }

    [Fact]
    public async Task GetStatsAsync_GivingStats_ExcludesPostedBatches()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Add batches with different statuses
        var openBatch = new ContributionBatch
        {
            Id = 1,
            Name = "Open Batch",
            BatchDate = now,
            Status = BatchStatus.Open,
            CreatedDateTime = now
        };
        var closedBatch = new ContributionBatch
        {
            Id = 2,
            Name = "Closed Batch",
            BatchDate = now.AddDays(-1),
            Status = BatchStatus.Closed,
            CreatedDateTime = now
        };
        var postedBatch = new ContributionBatch
        {
            Id = 3,
            Name = "Posted Batch",
            BatchDate = now.AddDays(-2),
            Status = BatchStatus.Posted,
            CreatedDateTime = now
        };
        _context.ContributionBatches.AddRange(openBatch, closedBatch, postedBatch);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.GivingStats.Should().NotBeNull();
        stats.GivingStats.RecentBatches.Should().HaveCount(2); // Only Open and Closed, not Posted
        stats.GivingStats.RecentBatches.Should().Contain(b => b.Name == "Open Batch");
        stats.GivingStats.RecentBatches.Should().Contain(b => b.Name == "Closed Batch");
        stats.GivingStats.RecentBatches.Should().NotContain(b => b.Name == "Posted Batch");
    }

    #endregion

    #region Communications Stats Tests

    [Fact]
    public async Task GetStatsAsync_CommunicationsStats_CountsPendingCommunicationsCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Add communications with different statuses
        var pending1 = new Communication
        {
            Id = 1,
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Pending,
            Body = "Test message 1",
            CreatedDateTime = now
        };
        var pending2 = new Communication
        {
            Id = 2,
            CommunicationType = CommunicationType.Sms,
            Status = CommunicationStatus.Pending,
            Body = "Test message 2",
            CreatedDateTime = now
        };
        var sent = new Communication
        {
            Id = 3,
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Sent,
            Body = "Test message 3",
            CreatedDateTime = now
        };
        var draft = new Communication
        {
            Id = 4,
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Draft,
            Body = "Test message 4",
            CreatedDateTime = now
        };
        _context.Communications.AddRange(pending1, pending2, sent, draft);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.CommunicationsStats.Should().NotBeNull();
        stats.CommunicationsStats.PendingCount.Should().Be(2); // Only pending communications
    }

    [Fact]
    public async Task GetStatsAsync_CommunicationsStats_CountsSentThisWeekCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var sixDaysAgo = now.AddDays(-6);
        var eightDaysAgo = now.AddDays(-8);

        // Add communications - some within last 7 days, some older
        var sentRecent1 = new Communication
        {
            Id = 1,
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Sent,
            Body = "Recent 1",
            CreatedDateTime = sixDaysAgo
        };
        var sentRecent2 = new Communication
        {
            Id = 2,
            CommunicationType = CommunicationType.Sms,
            Status = CommunicationStatus.Sent,
            Body = "Recent 2",
            CreatedDateTime = now.AddDays(-3)
        };
        var sentOld = new Communication
        {
            Id = 3,
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Sent,
            Body = "Old",
            CreatedDateTime = eightDaysAgo
        };
        var pendingRecent = new Communication
        {
            Id = 4,
            CommunicationType = CommunicationType.Email,
            Status = CommunicationStatus.Pending,
            Body = "Pending",
            CreatedDateTime = now.AddDays(-2)
        };
        _context.Communications.AddRange(sentRecent1, sentRecent2, sentOld, pendingRecent);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.CommunicationsStats.Should().NotBeNull();
        stats.CommunicationsStats.SentThisWeekCount.Should().Be(2); // Only sent within last 7 days
    }

    [Fact]
    public async Task GetStatsAsync_CommunicationsStats_ReturnsLast5CommunicationsOrderedByCreatedDateTime()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Add 7 communications with different created dates
        var communications = new List<Communication>();
        for (int i = 1; i <= 7; i++)
        {
            communications.Add(new Communication
            {
                Id = i,
                CommunicationType = CommunicationType.Email,
                Status = CommunicationStatus.Sent,
                Subject = $"Communication {i}",
                Body = $"Body {i}",
                RecipientCount = i * 10,
                DeliveredCount = i * 8,
                FailedCount = i * 2,
                CreatedDateTime = now.AddDays(-i)
            });
        }
        _context.Communications.AddRange(communications);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.CommunicationsStats.Should().NotBeNull();
        stats.CommunicationsStats.RecentCommunications.Should().HaveCount(5); // Maximum 5

        // Verify ordering by CreatedDateTime descending (most recent first)
        for (int i = 0; i < stats.CommunicationsStats.RecentCommunications.Count - 1; i++)
        {
            stats.CommunicationsStats.RecentCommunications[i].CreatedDateTime
                .Should().BeOnOrAfter(stats.CommunicationsStats.RecentCommunications[i + 1].CreatedDateTime);
        }

        // Verify first communication has correct data
        var firstComm = stats.CommunicationsStats.RecentCommunications[0];
        firstComm.IdKey.Should().NotBeNullOrEmpty();
        firstComm.Subject.Should().Be("Communication 1");
        firstComm.CommunicationType.Should().Be("Email");
        firstComm.Status.Should().Be("Sent");
        firstComm.RecipientCount.Should().Be(10);
        firstComm.DeliveredCount.Should().Be(8);
        firstComm.FailedCount.Should().Be(2);
    }

    [Fact]
    public async Task GetStatsAsync_CommunicationsStats_WithNoData_ReturnsZeroCounts()
    {
        // Arrange - no communications in database (using existing empty state)

        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.CommunicationsStats.Should().NotBeNull();
        stats.CommunicationsStats.PendingCount.Should().Be(0);
        stats.CommunicationsStats.SentThisWeekCount.Should().Be(0);
        stats.CommunicationsStats.RecentCommunications.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatsAsync_GivingStats_WithNoData_ReturnsZeroTotals()
    {
        // Arrange - no contributions in database (using existing empty state)

        // Act
        var stats = await _service.GetStatsAsync(CancellationToken.None);

        // Assert
        stats.GivingStats.Should().NotBeNull();
        stats.GivingStats.MonthToDateTotal.Should().Be(0);
        stats.GivingStats.YearToDateTotal.Should().Be(0);
        stats.GivingStats.RecentBatches.Should().BeEmpty();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
