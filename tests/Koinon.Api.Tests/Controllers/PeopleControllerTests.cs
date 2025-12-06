using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Requests;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
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

    // Valid IdKeys for testing (using IdKeyHelper.Encode)
    private readonly string _personIdKey = IdKeyHelper.Encode(123);
    private readonly string _person2IdKey = IdKeyHelper.Encode(456);
    private readonly string _familyIdKey = IdKeyHelper.Encode(100);
    private readonly string _campusIdKey = IdKeyHelper.Encode(5);
    private readonly string _statusIdKey = IdKeyHelper.Encode(10);
    private readonly string _connectionStatusIdKey = IdKeyHelper.Encode(20);
    private readonly string _newPersonIdKey = IdKeyHelper.Encode(999);

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
                new() { IdKey = _personIdKey, FirstName = "John", LastName = "Doe", FullName = "John Doe", Gender = "Male" },
                new() { IdKey = _person2IdKey, FirstName = "Jane", LastName = "Smith", FullName = "Jane Smith", Gender = "Female" }
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
            campusId: _campusIdKey,
            recordStatusId: _statusIdKey,
            connectionStatusId: _connectionStatusIdKey,
            includeInactive: true,
            page: 2,
            pageSize: 50);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.Query.Should().Be("test");
        capturedParameters.CampusId.Should().Be(_campusIdKey);
        capturedParameters.RecordStatusId.Should().Be(_statusIdKey);
        capturedParameters.ConnectionStatusId.Should().Be(_connectionStatusIdKey);
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
            IdKey = _personIdKey,
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
            .Setup(s => s.GetByIdKeyAsync(_personIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPerson);

        // Act
        var result = await _controller.GetByIdKey(_personIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var person = okResult.Value.Should().BeOfType<PersonDto>().Subject;
        person.IdKey.Should().Be(_personIdKey);
        person.FullName.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetByIdKey_WithNonExistentPerson_ReturnsNotFound()
    {
        // Arrange
        var nonExistentIdKey = IdKeyHelper.Encode(99999);
        _personServiceMock
            .Setup(s => s.GetByIdKeyAsync(nonExistentIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonDto?)null);

        // Act
        var result = await _controller.GetByIdKey(nonExistentIdKey);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Detail.Should().Contain(nonExistentIdKey);
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
            IdKey = _newPersonIdKey,
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
        createdResult.RouteValues!["idKey"].Should().Be(_newPersonIdKey);

        var person = createdResult.Value.Should().BeOfType<PersonDto>().Subject;
        person.IdKey.Should().Be(_newPersonIdKey);
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
            IdKey = _personIdKey,
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
            .Setup(s => s.UpdateAsync(_personIdKey, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Success(updatedPerson));

        // Act
        var result = await _controller.Update(_personIdKey, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var person = okResult.Value.Should().BeOfType<PersonDto>().Subject;
        person.IdKey.Should().Be(_personIdKey);
        person.FirstName.Should().Be("John Updated");
        person.ModifiedDateTime.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_WithNonExistentPerson_ReturnsNotFound()
    {
        // Arrange
        var nonExistentIdKey = IdKeyHelper.Encode(99999);
        var request = new UpdatePersonRequest
        {
            FirstName = "John"
        };

        var error = Error.NotFound("Person", nonExistentIdKey);

        _personServiceMock
            .Setup(s => s.UpdateAsync(nonExistentIdKey, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Failure(error));

        // Act
        var result = await _controller.Update(nonExistentIdKey, request);

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
            .Setup(s => s.UpdateAsync(_personIdKey, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PersonDto>.Failure(error));

        // Act
        var result = await _controller.Update(_personIdKey, request);

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
            .Setup(s => s.DeleteAsync(_personIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.Delete(_personIdKey);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithNonExistentPerson_ReturnsNotFound()
    {
        // Arrange
        var nonExistentIdKey = IdKeyHelper.Encode(99999);
        var error = Error.NotFound("Person", nonExistentIdKey);

        _personServiceMock
            .Setup(s => s.DeleteAsync(nonExistentIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.Delete(nonExistentIdKey);

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
            .Setup(s => s.DeleteAsync(_personIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.Delete(_personIdKey);

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
            IdKey = _familyIdKey,
            Name = "Doe Family",
            MemberCount = 4
        };

        _personServiceMock
            .Setup(s => s.GetFamilyAsync(_personIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedFamily);

        // Act
        var result = await _controller.GetFamily(_personIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var family = okResult.Value.Should().BeOfType<FamilySummaryDto>().Subject;
        family.IdKey.Should().Be(_familyIdKey);
        family.Name.Should().Be("Doe Family");
        family.MemberCount.Should().Be(4);
    }

    [Fact]
    public async Task GetFamily_WithNoFamily_ReturnsNotFound()
    {
        // Arrange
        _personServiceMock
            .Setup(s => s.GetFamilyAsync(_personIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FamilySummaryDto?)null);

        // Act
        var result = await _controller.GetFamily(_personIdKey);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Detail.Should().Contain(_personIdKey);
    }

    #endregion
}
