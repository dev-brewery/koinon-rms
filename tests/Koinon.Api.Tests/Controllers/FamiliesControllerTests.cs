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

public class FamiliesControllerTests
{
    private readonly Mock<IFamilyService> _familyServiceMock;
    private readonly Mock<ILogger<FamiliesController>> _loggerMock;
    private readonly FamiliesController _controller;

    public FamiliesControllerTests()
    {
        _familyServiceMock = new Mock<IFamilyService>();
        _loggerMock = new Mock<ILogger<FamiliesController>>();
        _controller = new FamiliesController(_familyServiceMock.Object, _loggerMock.Object);

        // Setup HttpContext for controller
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region Search Tests

    [Fact]
    public async Task Search_ReturnsOkWithEmptyResult_WhenSearchNotImplemented()
    {
        // Act
        var result = await _controller.Search(
            query: "Smith",
            campusId: null,
            includeInactive: false,
            page: 1,
            pageSize: 25);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<FamilySummaryDto>>().Subject;
        pagedResult.Items.Should().BeEmpty();
        pagedResult.TotalCount.Should().Be(0);
        pagedResult.Page.Should().Be(1);
        pagedResult.PageSize.Should().Be(25);
    }

    [Fact]
    public async Task Search_ValidatesPageSize_WhenExceedsMaximum()
    {
        // Act
        var result = await _controller.Search(
            query: null,
            campusId: null,
            includeInactive: false,
            page: 1,
            pageSize: 200); // Exceeds max of 100

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<FamilySummaryDto>>().Subject;
        pagedResult.PageSize.Should().Be(100); // Should be capped at 100
    }

    #endregion

    #region GetByIdKey Tests

    [Fact]
    public async Task GetByIdKey_ReturnsOk_WhenFamilyExists()
    {
        // Arrange
        var familyDto = new FamilyDto
        {
            IdKey = "family123",
            Guid = Guid.NewGuid(),
            Name = "Smith Family",
            IsActive = true,
            Members = new List<FamilyMemberDto>
            {
                new()
                {
                    IdKey = "member1",
                    Person = new PersonSummaryDto
                    {
                        IdKey = "person1",
                        FirstName = "John",
                        LastName = "Smith",
                        FullName = "John Smith",
                        Gender = "Male"
                    },
                    Role = new GroupTypeRoleDto
                    {
                        IdKey = "role1",
                        Name = "Adult",
                        IsLeader = true
                    },
                    Status = "Active"
                }
            },
            CreatedDateTime = DateTime.UtcNow
        };

        _familyServiceMock
            .Setup(s => s.GetByIdKeyAsync("family123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(familyDto);

        // Act
        var result = await _controller.GetByIdKey("family123");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedFamily = okResult.Value.Should().BeOfType<FamilyDto>().Subject;
        returnedFamily.IdKey.Should().Be("family123");
        returnedFamily.Name.Should().Be("Smith Family");
        returnedFamily.Members.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdKey_ReturnsNotFound_WhenFamilyDoesNotExist()
    {
        // Arrange
        _familyServiceMock
            .Setup(s => s.GetByIdKeyAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FamilyDto?)null);

        // Act
        var result = await _controller.GetByIdKey("nonexistent");

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Title.Should().Be("Family not found");
    }

    #endregion

    #region GetMembers Tests

    [Fact]
    public async Task GetMembers_ReturnsOk_WhenFamilyExists()
    {
        // Arrange
        var members = new List<FamilyMemberDto>
        {
            new()
            {
                IdKey = "member1",
                Person = new PersonSummaryDto
                {
                    IdKey = "person1",
                    FirstName = "John",
                    LastName = "Smith",
                    FullName = "John Smith",
                    Gender = "Male"
                },
                Role = new GroupTypeRoleDto
                {
                    IdKey = "role1",
                    Name = "Adult",
                    IsLeader = true
                },
                Status = "Active"
            },
            new()
            {
                IdKey = "member2",
                Person = new PersonSummaryDto
                {
                    IdKey = "person2",
                    FirstName = "Jane",
                    LastName = "Smith",
                    FullName = "Jane Smith",
                    Gender = "Female"
                },
                Role = new GroupTypeRoleDto
                {
                    IdKey = "role1",
                    Name = "Adult",
                    IsLeader = false
                },
                Status = "Active"
            }
        };

        var familyDto = new FamilyDto
        {
            IdKey = "family123",
            Guid = Guid.NewGuid(),
            Name = "Smith Family",
            IsActive = true,
            Members = members,
            CreatedDateTime = DateTime.UtcNow
        };

        _familyServiceMock
            .Setup(s => s.GetByIdKeyAsync("family123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(familyDto);

        // Act
        var result = await _controller.GetMembers("family123");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMembers = okResult.Value.Should().BeAssignableTo<IReadOnlyList<FamilyMemberDto>>().Subject;
        returnedMembers.Should().HaveCount(2);
        returnedMembers[0].Person.FirstName.Should().Be("John");
        returnedMembers[1].Person.FirstName.Should().Be("Jane");
    }

    [Fact]
    public async Task GetMembers_ReturnsNotFound_WhenFamilyDoesNotExist()
    {
        // Arrange
        _familyServiceMock
            .Setup(s => s.GetByIdKeyAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FamilyDto?)null);

        // Act
        var result = await _controller.GetMembers("nonexistent");

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ReturnsCreated_WhenSuccessful()
    {
        // Arrange
        var request = new CreateFamilyRequest
        {
            Name = "New Family",
            CampusId = "campus123"
        };

        var createdFamily = new FamilyDto
        {
            IdKey = "family456",
            Guid = Guid.NewGuid(),
            Name = "New Family",
            IsActive = true,
            Members = new List<FamilyMemberDto>(),
            CreatedDateTime = DateTime.UtcNow
        };

        _familyServiceMock
            .Setup(s => s.CreateFamilyAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FamilyDto>.Success(createdFamily));

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(FamiliesController.GetByIdKey));
        createdResult.RouteValues!["idKey"].Should().Be("family456");

        var returnedFamily = createdResult.Value.Should().BeOfType<FamilyDto>().Subject;
        returnedFamily.Name.Should().Be("New Family");
        returnedFamily.IdKey.Should().Be("family456");
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var request = new CreateFamilyRequest
        {
            Name = "" // Invalid - empty name
        };

        var error = new Error(
            "VALIDATION_ERROR",
            "Validation failed",
            new Dictionary<string, string[]>
            {
                ["Name"] = new[] { "Family name is required" }
            });

        _familyServiceMock
            .Setup(s => s.CreateFamilyAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FamilyDto>.Failure(error));

        // Act
        var result = await _controller.Create(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Extensions["errors"].Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ReturnsUnprocessableEntity_WhenBusinessRuleViolated()
    {
        // Arrange
        var request = new CreateFamilyRequest
        {
            Name = "Test Family"
        };

        var error = new Error(
            "DUPLICATE_FAMILY",
            "A family with this name already exists");

        _familyServiceMock
            .Setup(s => s.CreateFamilyAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FamilyDto>.Failure(error));

        // Act
        var result = await _controller.Create(request);

        // Assert
        var unprocessableResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        var problemDetails = unprocessableResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        problemDetails.Title.Should().Be("DUPLICATE_FAMILY");
    }

    #endregion

    #region AddMember Tests

    [Fact]
    public async Task AddMember_ReturnsCreated_WhenSuccessful()
    {
        // Arrange
        var request = new AddFamilyMemberRequest
        {
            PersonId = "person123",
            RoleId = "role456"
        };

        var addedMember = new FamilyMemberDto
        {
            IdKey = "member789",
            Person = new PersonSummaryDto
            {
                IdKey = "person123",
                FirstName = "John",
                LastName = "Doe",
                FullName = "John Doe",
                Gender = "Male"
            },
            Role = new GroupTypeRoleDto
            {
                IdKey = "role456",
                Name = "Adult",
                IsLeader = true
            },
            Status = "Active"
        };

        _familyServiceMock
            .Setup(s => s.AddFamilyMemberAsync("family123", request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FamilyMemberDto>.Success(addedMember));

        // Act
        var result = await _controller.AddMember("family123", request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(FamiliesController.GetMembers));
        createdResult.RouteValues!["idKey"].Should().Be("family123");

        var returnedMember = createdResult.Value.Should().BeOfType<FamilyMemberDto>().Subject;
        returnedMember.Person.IdKey.Should().Be("person123");
    }

    [Fact]
    public async Task AddMember_ReturnsNotFound_WhenFamilyDoesNotExist()
    {
        // Arrange
        var request = new AddFamilyMemberRequest
        {
            PersonId = "person123",
            RoleId = "role456"
        };

        var error = new Error(
            "NOT_FOUND",
            "Family not found");

        _familyServiceMock
            .Setup(s => s.AddFamilyMemberAsync("nonexistent", request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FamilyMemberDto>.Failure(error));

        // Act
        var result = await _controller.AddMember("nonexistent", request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task AddMember_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var request = new AddFamilyMemberRequest
        {
            PersonId = "",
            RoleId = ""
        };

        var error = new Error(
            "VALIDATION_ERROR",
            "Validation failed",
            new Dictionary<string, string[]>
            {
                ["PersonId"] = new[] { "Person ID is required" },
                ["RoleId"] = new[] { "Role ID is required" }
            });

        _familyServiceMock
            .Setup(s => s.AddFamilyMemberAsync("family123", request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FamilyMemberDto>.Failure(error));

        // Act
        var result = await _controller.AddMember("family123", request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region RemoveMember Tests

    [Fact]
    public async Task RemoveMember_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        _familyServiceMock
            .Setup(s => s.RemoveFamilyMemberAsync("family123", "person456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.RemoveMember("family123", "person456");

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveMember_ReturnsNotFound_WhenFamilyOrPersonDoesNotExist()
    {
        // Arrange
        var error = new Error(
            "NOT_FOUND",
            "Family or person not found");

        _familyServiceMock
            .Setup(s => s.RemoveFamilyMemberAsync("family123", "nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.RemoveMember("family123", "nonexistent");

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task RemoveMember_ReturnsUnprocessableEntity_WhenBusinessRuleViolated()
    {
        // Arrange
        var error = new Error(
            "CANNOT_REMOVE_LAST_MEMBER",
            "Cannot remove the last member from a family");

        _familyServiceMock
            .Setup(s => s.RemoveFamilyMemberAsync("family123", "person456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.RemoveMember("family123", "person456");

        // Assert
        var unprocessableResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        var problemDetails = unprocessableResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ReturnsNotFound_WhenNotImplemented()
    {
        // Arrange
        var request = new UpdateFamilyRequest
        {
            Name = "Updated Name"
        };

        // Act
        var result = await _controller.Update("family123", request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Not implemented");
    }

    #endregion
}
