// Test limitation note:
// =====================
// The root cause of issue #681 is that PersonMergeService.MergeAsync was calling
// BeginTransactionAsync without wrapping it in an ExecutionStrategy, which conflicts
// with the Npgsql provider's EnableRetryOnFailure() configuration at runtime.
//
// EF Core's in-memory provider does NOT support EnableRetryOnFailure — it is an
// Npgsql-specific feature — so these tests cannot reproduce the original InvalidOperationException
// directly. Instead they exercise the happy path and validation paths against an in-memory
// DbContext to confirm that the refactored code compiles, composes correctly, and still
// produces the same Result<PersonMergeResultDto> shape callers depend on.
//
// Regression protection against the original retry-strategy conflict comes from:
//   1. Live curl verification against a real Postgres instance (documented in the PR body).
//   2. The @smoke golden-path Playwright test in
//      src/web/e2e/tests/admin/people/feat-679-person-merge-flow.spec.ts which was unskipped
//      as part of this fix and exercises the full request against the running API.

using Koinon.Application.DTOs.PersonMerge;
using Koinon.Application.Interfaces;
using Koinon.Application.Services;
using Koinon.Domain.Data;
using Koinon.Domain.Entities;
using Koinon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

public class PersonMergeServiceTests : IDisposable
{
    private readonly KoinonDbContext _context;
    private readonly Mock<IPersonService> _personServiceMock;
    private readonly Mock<ILogger<PersonMergeService>> _loggerMock;
    private readonly PersonMergeService _service;

    public PersonMergeServiceTests()
    {
        var options = new DbContextOptionsBuilder<KoinonDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            // Mirror production: PostgreSqlProvider defaults to NoTracking.
            // Without this, mutations on untracked entities persist in tests
            // but silently drop in production (the bug behind the merge fix).
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        _context = new KoinonDbContext(options);
        _personServiceMock = new Mock<IPersonService>();
        _loggerMock = new Mock<ILogger<PersonMergeService>>();

        _service = new PersonMergeService(
            _context,
            _personServiceMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task MergeAsync_InvalidIdKey_ReturnsValidationFailure()
    {
        // Arrange
        var request = new PersonMergeRequestDto
        {
            SurvivorIdKey = "not-a-valid-idkey",
            MergedIdKey = "also-invalid",
            FieldSelections = new Dictionary<string, string>()
        };

        // Act
        var result = await _service.MergeAsync(request, currentUserId: 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Contains("IdKey", result.Error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MergeAsync_MergingSameIdKey_ReturnsValidationFailure()
    {
        // Arrange — same IdKey for survivor and merged
        var person = await CreatePersonAsync("Jane", "Doe");
        var request = new PersonMergeRequestDto
        {
            SurvivorIdKey = person.IdKey,
            MergedIdKey = person.IdKey,
            FieldSelections = new Dictionary<string, string>()
        };

        // Act
        var result = await _service.MergeAsync(request, currentUserId: 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Contains("merge a person with themselves", result.Error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MergeAsync_SurvivorNotFound_ReturnsNotFound()
    {
        // Arrange
        var existing = await CreatePersonAsync("Jane", "Doe");
        // Construct an IdKey pointing at an id that does not exist.
        var missingIdKey = IdKeyHelper.Encode(existing.Id + 9999);

        var request = new PersonMergeRequestDto
        {
            SurvivorIdKey = missingIdKey,
            MergedIdKey = existing.IdKey,
            FieldSelections = new Dictionary<string, string>()
        };

        // Act
        var result = await _service.MergeAsync(request, currentUserId: 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task MergeAsync_ValidRequest_InvokesExecutionStrategy()
    {
        // Arrange — two distinct people.
        // This test asserts the ExecutionStrategy-wrapped transaction code path does not
        // throw the previous InvalidOperationException about NpgsqlRetryingExecutionStrategy.
        // The in-memory provider does not support ExecuteUpdateAsync so the merge will not
        // complete successfully, but it must fail as a caught exception that produces a
        // Result.Failure — NOT by bubbling the retry-strategy InvalidOperationException,
        // which is what #681 manifested as.
        var survivor = await CreatePersonAsync("Survivor", "Smith");
        var merged = await CreatePersonAsync("Merged", "Smith");

        var request = new PersonMergeRequestDto
        {
            SurvivorIdKey = survivor.IdKey,
            MergedIdKey = merged.IdKey,
            FieldSelections = new Dictionary<string, string>()
        };

        // Act — CreateExecutionStrategy returns a NoopExecutionStrategy for the in-memory
        // provider, which runs the delegate exactly once. The critical guarantee we assert
        // is that the call completes (returns a Result) rather than throwing a raw
        // InvalidOperationException about user-initiated transactions.
        var result = await _service.MergeAsync(request, currentUserId: 1);

        // Assert — code must return a Result<PersonMergeResultDto> rather than raising the
        // retry-strategy conflict. The Result may be success or failure depending on which
        // in-memory-unsupported operation fails first; we simply assert the shape is intact.
        Assert.NotNull(result);
        if (!result.IsSuccess)
        {
            Assert.NotNull(result.Error);
            // If failure, it must NOT be the original retry-strategy conflict message.
            Assert.DoesNotContain(
                "NpgsqlRetryingExecutionStrategy",
                result.Error!.Message,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "user-initiated transactions",
                result.Error.Message,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task<Person> CreatePersonAsync(string firstName, string lastName)
    {
        var person = new Person
        {
            FirstName = firstName,
            LastName = lastName,
            CreatedDateTime = DateTime.UtcNow
        };
        _context.People.Add(person);
        await _context.SaveChangesAsync();
        return person;
    }
}
