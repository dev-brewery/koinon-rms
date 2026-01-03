using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.PersonMerge;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

public class PersonMergeControllerTests
{
    private readonly Mock<IDuplicateDetectionService> _duplicateDetectionServiceMock;
    private readonly Mock<IPersonMergeService> _personMergeServiceMock;
    private readonly Mock<IDuplicateIgnoreService> _duplicateIgnoreServiceMock;
    private readonly Mock<ILogger<PersonMergeController>> _loggerMock;
    private readonly PersonMergeController _controller;

    // Valid IdKeys for testing
    private readonly string _person1IdKey = IdKeyHelper.Encode(123);
    private readonly string _person2IdKey = IdKeyHelper.Encode(456);
    private readonly string _person3IdKey = IdKeyHelper.Encode(789);
    private const int CurrentUserId = 999;

    public PersonMergeControllerTests()
    {
        _duplicateDetectionServiceMock = new Mock<IDuplicateDetectionService>();
        _personMergeServiceMock = new Mock<IPersonMergeService>();
        _duplicateIgnoreServiceMock = new Mock<IDuplicateIgnoreService>();
        _loggerMock = new Mock<ILogger<PersonMergeController>>();

        _controller = new PersonMergeController(
            _duplicateDetectionServiceMock.Object,
            _personMergeServiceMock.Object,
            _duplicateIgnoreServiceMock.Object,
            _loggerMock.Object);

        // Setup HttpContext with authenticated user claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, CurrentUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #region GetDuplicates Tests

    [Fact]
    public async Task GetDuplicates_WithValidParameters_ReturnsOkWithPagedResult()
    {
        // Arrange
        var duplicates = new List<DuplicateMatchDto>
        {
            new()
            {
                Person1IdKey = _person1IdKey,
                Person1Name = "John Doe",
                Person1Email = "john@example.com",
                Person1Phone = "555-0100",
                Person2IdKey = _person2IdKey,
                Person2Name = "John A Doe",
                Person2Email = "jdoe@example.com",
                Person2Phone = "555-0100",
                MatchScore = 85,
                MatchReasons = new List<string> { "Same name (fuzzy)", "Same phone" }
            }
        };

        var expectedResult = new PagedResult<DuplicateMatchDto>(
            duplicates,
            totalCount: 1,
            page: 1,
            pageSize: 20
        );

        _duplicateDetectionServiceMock
            .Setup(s => s.FindDuplicatesAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetDuplicates(page: 1, pageSize: 20);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var items = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<DuplicateMatchDto>>().Subject.ToList();
        items.Should().HaveCount(1);
        items[0].MatchScore.Should().Be(85);
        items[0].MatchReasons.Should().Contain("Same phone");
    }

    [Fact]
    public async Task GetDuplicates_WithDefaultParameters_UsesCorrectDefaults()
    {
        // Arrange
        var expectedResult = new PagedResult<DuplicateMatchDto>(
            new List<DuplicateMatchDto>(),
            totalCount: 0,
            page: 1,
            pageSize: 20
        );

        _duplicateDetectionServiceMock
            .Setup(s => s.FindDuplicatesAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetDuplicates();

        // Assert
        _duplicateDetectionServiceMock.Verify(
            s => s.FindDuplicatesAsync(1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetDuplicatesForPerson Tests

    [Fact]
    public async Task GetDuplicatesForPerson_WithValidIdKey_ReturnsOkWithList()
    {
        // Arrange
        var duplicates = new List<DuplicateMatchDto>
        {
            new()
            {
                Person1IdKey = _person1IdKey,
                Person1Name = "John Doe",
                Person2IdKey = _person2IdKey,
                Person2Name = "John A Doe",
                MatchScore = 85,
                MatchReasons = new List<string> { "Same name" }
            },
            new()
            {
                Person1IdKey = _person1IdKey,
                Person1Name = "John Doe",
                Person2IdKey = _person3IdKey,
                Person2Name = "Jonathan Doe",
                MatchScore = 75,
                MatchReasons = new List<string> { "Similar name" }
            }
        };

        _duplicateDetectionServiceMock
            .Setup(s => s.FindDuplicatesForPersonAsync(_person1IdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicates);

        // Act
        var result = await _controller.GetDuplicatesForPerson(_person1IdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var items = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<DuplicateMatchDto>>().Subject.ToList();
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDuplicatesForPerson_WithNoDuplicates_ReturnsEmptyList()
    {
        // Arrange
        _duplicateDetectionServiceMock
            .Setup(s => s.FindDuplicatesForPersonAsync(_person1IdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DuplicateMatchDto>());

        // Act
        var result = await _controller.GetDuplicatesForPerson(_person1IdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var items = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<DuplicateMatchDto>>().Subject.ToList();
        items.Should().BeEmpty();
    }

    #endregion

    #region ComparePeople Tests

    [Fact]
    public async Task ComparePeople_WithValidIdKeys_ReturnsOkWithComparison()
    {
        // Arrange
        var comparison = new PersonComparisonDto
        {
            Person1 = new PersonDto
            {
                IdKey = _person1IdKey,
                Guid = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                FullName = "John Doe",
                Gender = "Male",
                EmailPreference = "EmailAllowed",
                PhoneNumbers = new List<PhoneNumberDto>(),
                CreatedDateTime = DateTime.UtcNow
            },
            Person2 = new PersonDto
            {
                IdKey = _person2IdKey,
                Guid = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                FullName = "John A Doe",
                Gender = "Male",
                EmailPreference = "EmailAllowed",
                PhoneNumbers = new List<PhoneNumberDto>(),
                CreatedDateTime = DateTime.UtcNow
            },
            Person1AttendanceCount = 10,
            Person2AttendanceCount = 5,
            Person1GroupMembershipCount = 3,
            Person2GroupMembershipCount = 2,
            Person1ContributionTotal = 1000m,
            Person2ContributionTotal = 500m
        };

        _personMergeServiceMock
            .Setup(s => s.ComparePersonsAsync(_person1IdKey, _person2IdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comparison);

        // Act
        var result = await _controller.ComparePeople(_person1IdKey, _person2IdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var data = dataProperty!.GetValue(response).Should().BeOfType<PersonComparisonDto>().Subject;
        data.Person1AttendanceCount.Should().Be(10);
        data.Person2AttendanceCount.Should().Be(5);
    }

    [Fact]
    public async Task ComparePeople_WithNonExistentPerson_ReturnsNotFound()
    {
        // Arrange
        _personMergeServiceMock
            .Setup(s => s.ComparePersonsAsync(_person1IdKey, _person2IdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonComparisonDto?)null);

        // Act
        var result = await _controller.ComparePeople(_person1IdKey, _person2IdKey);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Person not found");
    }

    #endregion

    #region MergePeople Tests

    [Fact]
    public async Task MergePeople_WithValidRequest_ReturnsOkWithResult()
    {
        // Arrange
        var request = new PersonMergeRequestDto
        {
            SurvivorIdKey = _person1IdKey,
            MergedIdKey = _person2IdKey,
            FieldSelections = new Dictionary<string, string>
            {
                { "Email", _person1IdKey }
            }
        };

        var mergeResult = new PersonMergeResultDto
        {
            SurvivorIdKey = _person1IdKey,
            MergedIdKey = _person2IdKey,
            AliasesUpdated = 1,
            GroupMembershipsUpdated = 2,
            FamilyMembershipsUpdated = 1,
            PhoneNumbersUpdated = 2,
            AuthorizedPickupsUpdated = 1,
            CommunicationPreferencesUpdated = 1,
            RefreshTokensUpdated = 0,
            SecurityRolesUpdated = 1,
            SupervisorSessionsUpdated = 0,
            FollowUpsUpdated = 1,
            TotalRecordsUpdated = 10,
            MergedDateTime = DateTime.UtcNow
        };

        _personMergeServiceMock
            .Setup(s => s.MergeAsync(request, CurrentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonMergeResultDto>.Success(mergeResult));

        // Act
        var result = await _controller.MergePeople(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var data = dataProperty!.GetValue(response).Should().BeOfType<PersonMergeResultDto>().Subject;
        data.TotalRecordsUpdated.Should().Be(10);
    }

    [Fact]
    public async Task MergePeople_WithFailedMerge_ReturnsBadRequest()
    {
        // Arrange
        var request = new PersonMergeRequestDto
        {
            SurvivorIdKey = _person1IdKey,
            MergedIdKey = _person2IdKey,
            FieldSelections = new Dictionary<string, string>()
        };

        var error = new Error("MERGE_FAILED", "Cannot merge the same person");
        _personMergeServiceMock
            .Setup(s => s.MergeAsync(request, CurrentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonMergeResultDto>.Failure(error));

        // Act
        var result = await _controller.MergePeople(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Merge failed");
        problemDetails.Detail.Should().Contain("Cannot merge the same person");
    }

    [Fact]
    public async Task MergePeople_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var request = new PersonMergeRequestDto
        {
            SurvivorIdKey = _person1IdKey,
            MergedIdKey = _person2IdKey,
            FieldSelections = new Dictionary<string, string>()
        };

        // Create controller without authenticated user
        var unauthController = new PersonMergeController(
            _duplicateDetectionServiceMock.Object,
            _personMergeServiceMock.Object,
            _duplicateIgnoreServiceMock.Object,
            _loggerMock.Object);

        unauthController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await unauthController.MergePeople(request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var problemDetails = unauthorizedResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Authentication required");
    }

    #endregion

    #region GetMergeHistory Tests

    [Fact]
    public async Task GetMergeHistory_WithValidParameters_ReturnsOkWithPagedResult()
    {
        // Arrange
        var historyItems = new List<PersonMergeHistoryDto>
        {
            new()
            {
                IdKey = IdKeyHelper.Encode(1),
                SurvivorIdKey = _person1IdKey,
                SurvivorName = "John Doe",
                MergedIdKey = _person2IdKey,
                MergedName = "John A Doe",
                MergedByIdKey = IdKeyHelper.Encode(CurrentUserId),
                MergedByName = "Admin User",
                MergedDateTime = DateTime.UtcNow.AddDays(-1),
                Notes = "Duplicate record identified"
            }
        };

        var expectedResult = new PagedResult<PersonMergeHistoryDto>(
            historyItems,
            totalCount: 1,
            page: 1,
            pageSize: 20
        );

        _personMergeServiceMock
            .Setup(s => s.GetMergeHistoryAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetMergeHistory(page: 1, pageSize: 20);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var items = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<PersonMergeHistoryDto>>().Subject.ToList();
        items.Should().HaveCount(1);
        items[0].SurvivorName.Should().Be("John Doe");
        items[0].MergedByName.Should().Be("Admin User");
    }

    [Fact]
    public async Task GetMergeHistory_WithDefaultParameters_UsesCorrectDefaults()
    {
        // Arrange
        var expectedResult = new PagedResult<PersonMergeHistoryDto>(
            new List<PersonMergeHistoryDto>(),
            totalCount: 0,
            page: 1,
            pageSize: 20
        );

        _personMergeServiceMock
            .Setup(s => s.GetMergeHistoryAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetMergeHistory();

        // Assert
        _personMergeServiceMock.Verify(
            s => s.GetMergeHistoryAsync(1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region IgnoreDuplicate Tests

    [Fact]
    public async Task IgnoreDuplicate_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new IgnoreDuplicateRequestDto
        {
            Person1IdKey = _person1IdKey,
            Person2IdKey = _person2IdKey,
            Reason = "Father and son with same name"
        };

        _duplicateIgnoreServiceMock
            .Setup(s => s.IgnoreDuplicateAsync(request, CurrentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.IgnoreDuplicate(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var messageProperty = response.GetType().GetProperty("message");
        var message = messageProperty!.GetValue(response)!.ToString();
        message.Should().Contain("ignored successfully");
    }

    [Fact]
    public async Task IgnoreDuplicate_WithFailedIgnore_ReturnsBadRequest()
    {
        // Arrange
        var request = new IgnoreDuplicateRequestDto
        {
            Person1IdKey = _person1IdKey,
            Person2IdKey = _person2IdKey
        };

        var error = new Error("IGNORE_FAILED", "Duplicate pair already ignored");
        _duplicateIgnoreServiceMock
            .Setup(s => s.IgnoreDuplicateAsync(request, CurrentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.IgnoreDuplicate(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Ignore failed");
        problemDetails.Detail.Should().Contain("already ignored");
    }

    [Fact]
    public async Task IgnoreDuplicate_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var request = new IgnoreDuplicateRequestDto
        {
            Person1IdKey = _person1IdKey,
            Person2IdKey = _person2IdKey
        };

        // Create controller without authenticated user
        var unauthController = new PersonMergeController(
            _duplicateDetectionServiceMock.Object,
            _personMergeServiceMock.Object,
            _duplicateIgnoreServiceMock.Object,
            _loggerMock.Object);

        unauthController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await unauthController.IgnoreDuplicate(request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var problemDetails = unauthorizedResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Authentication required");
    }

    #endregion

    #region UnignoreDuplicate Tests

    [Fact]
    public async Task UnignoreDuplicate_WithValidIdKeys_ReturnsOk()
    {
        // Arrange
        _duplicateIgnoreServiceMock
            .Setup(s => s.UnignoreDuplicateAsync(_person1IdKey, _person2IdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.UnignoreDuplicate(_person1IdKey, _person2IdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var messageProperty = response.GetType().GetProperty("message");
        var message = messageProperty!.GetValue(response)!.ToString();
        message.Should().Contain("removed successfully");
    }

    [Fact]
    public async Task UnignoreDuplicate_WithFailedUnignore_ReturnsBadRequest()
    {
        // Arrange
        var error = new Error("UNIGNORE_FAILED", "Duplicate pair not found in ignore list");
        _duplicateIgnoreServiceMock
            .Setup(s => s.UnignoreDuplicateAsync(_person1IdKey, _person2IdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.UnignoreDuplicate(_person1IdKey, _person2IdKey);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Unignore failed");
        problemDetails.Detail.Should().Contain("not found");
    }

    #endregion
}
