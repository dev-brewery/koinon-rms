using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

public class PeopleControllerTests
{
    private readonly Mock<IPersonService> _personServiceMock;
    private readonly Mock<ILogger<PeopleController>> _loggerMock;
    private readonly PeopleController _controller;

    public PeopleControllerTests()
    {
        _personServiceMock = new Mock<IPersonService>();
        _loggerMock = new Mock<ILogger<PeopleController>>();
        _controller = new PeopleController(_personServiceMock.Object, _loggerMock.Object);

        // Setup HttpContext for controller
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region Search Tests

    [Fact]
    public async Task Search_WithValidParameters_ReturnsOkWithPagedResult()
    {
        // Arrange
        var expectedResult = new PagedResult<PersonSummaryDto>(
            new List<PersonSummaryDto>
            {
                new() { IdKey = "test1", FirstName = "John", LastName = "Doe", FullName = "John Doe", Gender = "Male" },
                new() { IdKey = "test2", FirstName = "Jane", LastName = "Smith", FullName = "Jane Smith", Gender = "Female" }
            },
            totalCount: 2,
            page: 1,
            pageSize: 25
        );

        _personServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<PersonSearchParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Search(
            query: "John",
            campusId: null,
            recordStatusId: null,
            connectionStatusId: null,
            includeInactive: false,
            page: 1,
            pageSize: 25);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<PersonSummaryDto>>().Subject;
        pagedResult.Items.Should().HaveCount(2);
        pagedResult.TotalCount.Should().Be(2);
        pagedResult.Page.Should().Be(1);
        pagedResult.PageSize.Should().Be(25);
    }

    [Fact]
    public async Task Search_WithFilters_PassesFiltersToService()
    {
        // Arrange
        var expectedResult = new PagedResult<PersonSummaryDto>(
            new List<PersonSummaryDto>(),
            totalCount: 0,
            page: 1,
            pageSize: 25
        );

        PersonSearchParameters? capturedParameters = null;
        _personServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<PersonSearchParameters>(), It.IsAny<CancellationToken>()))
            .Callback<PersonSearchParameters, CancellationToken>((p, _) => capturedParameters = p)
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.Search(
            query: "test",
            campusId: "campus123",
            recordStatusId: "status456",
            connectionStatusId: "conn789",
            includeInactive: true,
            page: 2,
            pageSize: 50);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.Query.Should().Be("test");
        capturedParameters.CampusId.Should().Be("campus123");
        capturedParameters.RecordStatusId.Should().Be("status456");
        capturedParameters.ConnectionStatusId.Should().Be("conn789");
        capturedParameters.IncludeInactive.Should().BeTrue();
        capturedParameters.Page.Should().Be(2);
        capturedParameters.PageSize.Should().Be(50);
    }

    #endregion

    #region GetByIdKey Tests

    [Fact]
    public async Task GetByIdKey_WithExistingPerson_ReturnsOkWithPerson()
    {
        // Arrange
        var expectedPerson = new PersonDto
        {
            IdKey = "test123",
            Guid = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Gender = "Male",
            EmailPreference = "EmailAllowed",
            PhoneNumbers = new List<PhoneNumberDto>(),
            CreatedDateTime = DateTime.UtcNow
        };

        _personServiceMock
            .Setup(s => s.GetByIdKeyAsync("test123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPerson);

        // Act
        var result = await _controller.GetByIdKey("test123");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var person = okResult.Value.Should().BeOfType<PersonDto>().Subject;
        person.IdKey.Should().Be("test123");
        person.FullName.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetByIdKey_WithNonExistentPerson_ReturnsNotFound()
    {
        // Arrange
        _personServiceMock
            .Setup(s => s.GetByIdKeyAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonDto?)null);

        // Act
        var result = await _controller.GetByIdKey("nonexistent");

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Detail.Should().Contain("nonexistent");
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedWithPerson()
    {
        // Arrange
        var request = new CreatePersonRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        var createdPerson = new PersonDto
        {
            IdKey = "new123",
            Guid = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Email = "john.doe@example.com",
            Gender = "Unknown",
            EmailPreference = "EmailAllowed",
            PhoneNumbers = new List<PhoneNumberDto>(),
            CreatedDateTime = DateTime.UtcNow
        };

        _personServiceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Success(createdPerson));

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(PeopleController.GetByIdKey));
        createdResult.RouteValues.Should().ContainKey("idKey");
        createdResult.RouteValues!["idKey"].Should().Be("new123");

        var person = createdResult.Value.Should().BeOfType<PersonDto>().Subject;
        person.IdKey.Should().Be("new123");
        person.FullName.Should().Be("John Doe");
    }

    [Fact]
    public async Task Create_WithValidationError_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreatePersonRequest
        {
            FirstName = "",
            LastName = "Doe"
        };

        var error = new Error(
            "VALIDATION_ERROR",
            "One or more validation errors occurred",
            new Dictionary<string, string[]>
            {
                { "FirstName", new[] { "First name is required" } }
            }
        );

        _personServiceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Failure(error));

        // Act
        var result = await _controller.Create(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Extensions.Should().ContainKey("errors");
    }

    [Fact]
    public async Task Create_WithBusinessRuleViolation_ReturnsUnprocessableEntity()
    {
        // Arrange
        var request = new CreatePersonRequest
        {
            FirstName = "John",
            LastName = "Doe"
        };

        var error = Error.UnprocessableEntity("Cannot create person without family");

        _personServiceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Failure(error));

        // Act
        var result = await _controller.Create(request);

        // Assert
        var unprocessableResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        var problemDetails = unprocessableResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidRequest_ReturnsOkWithUpdatedPerson()
    {
        // Arrange
        var request = new UpdatePersonRequest
        {
            FirstName = "John Updated",
            Email = "john.updated@example.com"
        };

        var updatedPerson = new PersonDto
        {
            IdKey = "test123",
            Guid = Guid.NewGuid(),
            FirstName = "John Updated",
            LastName = "Doe",
            FullName = "John Updated Doe",
            Email = "john.updated@example.com",
            Gender = "Male",
            EmailPreference = "EmailAllowed",
            PhoneNumbers = new List<PhoneNumberDto>(),
            CreatedDateTime = DateTime.UtcNow.AddDays(-1),
            ModifiedDateTime = DateTime.UtcNow
        };

        _personServiceMock
            .Setup(s => s.UpdateAsync("test123", request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Success(updatedPerson));

        // Act
        var result = await _controller.Update("test123", request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var person = okResult.Value.Should().BeOfType<PersonDto>().Subject;
        person.IdKey.Should().Be("test123");
        person.FirstName.Should().Be("John Updated");
        person.ModifiedDateTime.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_WithNonExistentPerson_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdatePersonRequest
        {
            FirstName = "John"
        };

        var error = Error.NotFound("Person", "nonexistent");

        _personServiceMock
            .Setup(s => s.UpdateAsync("nonexistent", request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Failure(error));

        // Act
        var result = await _controller.Update("nonexistent", request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Update_WithValidationError_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdatePersonRequest
        {
            Email = "invalid-email"
        };

        var error = new Error(
            "VALIDATION_ERROR",
            "One or more validation errors occurred",
            new Dictionary<string, string[]>
            {
                { "Email", new[] { "Invalid email format" } }
            }
        );

        _personServiceMock
            .Setup(s => s.UpdateAsync("test123", request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Failure(error));

        // Act
        var result = await _controller.Update("test123", request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithExistingPerson_ReturnsNoContent()
    {
        // Arrange
        _personServiceMock
            .Setup(s => s.DeleteAsync("test123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.Delete("test123");

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithNonExistentPerson_ReturnsNotFound()
    {
        // Arrange
        var error = Error.NotFound("Person", "nonexistent");

        _personServiceMock
            .Setup(s => s.DeleteAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.Delete("nonexistent");

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Delete_WithBusinessRuleViolation_ReturnsUnprocessableEntity()
    {
        // Arrange
        var error = Error.UnprocessableEntity("Cannot delete person with active attendance");

        _personServiceMock
            .Setup(s => s.DeleteAsync("test123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.Delete("test123");

        // Assert
        var unprocessableResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        var problemDetails = unprocessableResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    #endregion

    #region GetFamily Tests

    [Fact]
    public async Task GetFamily_WithExistingFamily_ReturnsOkWithFamily()
    {
        // Arrange
        var expectedFamily = new FamilySummaryDto
        {
            IdKey = "family123",
            Name = "Doe Family",
            MemberCount = 4
        };

        _personServiceMock
            .Setup(s => s.GetFamilyAsync("test123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedFamily);

        // Act
        var result = await _controller.GetFamily("test123");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var family = okResult.Value.Should().BeOfType<FamilySummaryDto>().Subject;
        family.IdKey.Should().Be("family123");
        family.Name.Should().Be("Doe Family");
        family.MemberCount.Should().Be(4);
    }

    [Fact]
    public async Task GetFamily_WithNoFamily_ReturnsNotFound()
    {
        // Arrange
        _personServiceMock
            .Setup(s => s.GetFamilyAsync("test123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FamilySummaryDto?)null);

        // Act
        var result = await _controller.GetFamily("test123");

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Detail.Should().Contain("test123");
    }

    #endregion
}
