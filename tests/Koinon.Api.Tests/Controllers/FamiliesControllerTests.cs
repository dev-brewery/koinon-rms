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

public class FamiliesControllerTests
{
    private readonly Mock<IFamilyService> _familyServiceMock;
    private readonly Mock<ILogger<FamiliesController>> _loggerMock;
    private readonly FamiliesController _controller;

    // Valid IdKeys for testing (using IdKeyHelper.Encode)
    private readonly string _familyIdKey = IdKeyHelper.Encode(123);
    private readonly string _memberIdKey = IdKeyHelper.Encode(456);
    private readonly string _personIdKey = IdKeyHelper.Encode(789);
    private readonly string _person2IdKey = IdKeyHelper.Encode(790);
    private readonly string _roleIdKey = IdKeyHelper.Encode(10);
    private readonly string _campusIdKey = IdKeyHelper.Encode(5);
    private readonly string _newFamilyIdKey = IdKeyHelper.Encode(999);

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

    #region GetByIdKey Tests

    [Fact]
    public async Task GetByIdKey_ReturnsOk_WhenFamilyExists()
    {
        // Arrange
        var familyDto = new FamilyDto
        {
            IdKey = _familyIdKey,
            Guid = Guid.NewGuid(),
            Name = "Smith Family",
            IsActive = true,
            Members = new List<FamilyMemberDto>
            {
                new()
                {
                    IdKey = _memberIdKey,
                    Person = new PersonSummaryDto
                    {
                        IdKey = _personIdKey,
                        FirstName = "John",
                        LastName = "Smith",
                        FullName = "John Smith",
                        Gender = "Male"
                    },
                    Role = new GroupTypeRoleDto
                    {
                        IdKey = _roleIdKey,
                        Name = "Adult",
                        IsLeader = true
                    },
                    Status = "Active"
                }
            },
            CreatedDateTime = DateTime.UtcNow
        };

        _familyServiceMock
            .Setup(s => s.GetByIdKeyAsync(_familyIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(familyDto);

        // Act
        var result = await _controller.GetByIdKey(_familyIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedFamily = okResult.Value.Should().BeOfType<FamilyDto>().Subject;
        returnedFamily.IdKey.Should().Be(_familyIdKey);
        returnedFamily.Name.Should().Be("Smith Family");
        returnedFamily.Members.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdKey_ReturnsNotFound_WhenFamilyDoesNotExist()
    {
        // Arrange
        var nonExistentIdKey = IdKeyHelper.Encode(99999);
        _familyServiceMock
            .Setup(s => s.GetByIdKeyAsync(nonExistentIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FamilyDto?)null);

        // Act
        var result = await _controller.GetByIdKey(nonExistentIdKey);

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
                IdKey = _memberIdKey,
                Person = new PersonSummaryDto
                {
                    IdKey = _personIdKey,
                    FirstName = "John",
                    LastName = "Smith",
                    FullName = "John Smith",
                    Gender = "Male"
                },
                Role = new GroupTypeRoleDto
                {
                    IdKey = _roleIdKey,
                    Name = "Adult",
                    IsLeader = true
                },
                Status = "Active"
            },
            new()
            {
                IdKey = IdKeyHelper.Encode(457),
                Person = new PersonSummaryDto
                {
                    IdKey = _person2IdKey,
                    FirstName = "Jane",
                    LastName = "Smith",
                    FullName = "Jane Smith",
                    Gender = "Female"
                },
                Role = new GroupTypeRoleDto
                {
                    IdKey = _roleIdKey,
                    Name = "Adult",
                    IsLeader = false
                },
                Status = "Active"
            }
        };

        var familyDto = new FamilyDto
        {
            IdKey = _familyIdKey,
            Guid = Guid.NewGuid(),
            Name = "Smith Family",
            IsActive = true,
            Members = members,
            CreatedDateTime = DateTime.UtcNow
        };

        _familyServiceMock
            .Setup(s => s.GetByIdKeyAsync(_familyIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(familyDto);

        // Act
        var result = await _controller.GetMembers(_familyIdKey);

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
        var nonExistentIdKey = IdKeyHelper.Encode(99999);
        _familyServiceMock
            .Setup(s => s.GetByIdKeyAsync(nonExistentIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FamilyDto?)null);

        // Act
        var result = await _controller.GetMembers(nonExistentIdKey);

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
            CampusId = _campusIdKey
        };

        var createdFamily = new FamilyDto
        {
            IdKey = _newFamilyIdKey,
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
        createdResult.RouteValues!["idKey"].Should().Be(_newFamilyIdKey);

        var returnedFamily = createdResult.Value.Should().BeOfType<FamilyDto>().Subject;
        returnedFamily.Name.Should().Be("New Family");
        returnedFamily.IdKey.Should().Be(_newFamilyIdKey);
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
            PersonId = _personIdKey,
            RoleId = _roleIdKey
        };

        var addedMember = new FamilyMemberDto
        {
            IdKey = IdKeyHelper.Encode(789),
            Person = new PersonSummaryDto
            {
                IdKey = _personIdKey,
                FirstName = "John",
                LastName = "Doe",
                FullName = "John Doe",
                Gender = "Male"
            },
            Role = new GroupTypeRoleDto
            {
                IdKey = _roleIdKey,
                Name = "Adult",
                IsLeader = true
            },
            Status = "Active"
        };

        _familyServiceMock
            .Setup(s => s.AddFamilyMemberAsync(_familyIdKey, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FamilyMemberDto>.Success(addedMember));

        // Act
        var result = await _controller.AddMember(_familyIdKey, request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(FamiliesController.GetMembers));
        createdResult.RouteValues!["idKey"].Should().Be(_familyIdKey);

        var returnedMember = createdResult.Value.Should().BeOfType<FamilyMemberDto>().Subject;
        returnedMember.Person.IdKey.Should().Be(_personIdKey);
    }

    [Fact]
    public async Task AddMember_ReturnsNotFound_WhenFamilyDoesNotExist()
    {
        // Arrange
        var nonExistentIdKey = IdKeyHelper.Encode(99999);
        var request = new AddFamilyMemberRequest
        {
            PersonId = _personIdKey,
            RoleId = _roleIdKey
        };

        var error = new Error(
            "NOT_FOUND",
            "Family not found");

        _familyServiceMock
            .Setup(s => s.AddFamilyMemberAsync(nonExistentIdKey, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FamilyMemberDto>.Failure(error));

        // Act
        var result = await _controller.AddMember(nonExistentIdKey, request);

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
            .Setup(s => s.AddFamilyMemberAsync(_familyIdKey, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FamilyMemberDto>.Failure(error));

        // Act
        var result = await _controller.AddMember(_familyIdKey, request);

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
            .Setup(s => s.RemoveFamilyMemberAsync(_familyIdKey, _person2IdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.RemoveMember(_familyIdKey, _person2IdKey);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveMember_ReturnsNotFound_WhenFamilyOrPersonDoesNotExist()
    {
        // Arrange
        var nonExistentPersonIdKey = IdKeyHelper.Encode(99999);
        var error = new Error(
            "NOT_FOUND",
            "Family or person not found");

        _familyServiceMock
            .Setup(s => s.RemoveFamilyMemberAsync(_familyIdKey, nonExistentPersonIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.RemoveMember(_familyIdKey, nonExistentPersonIdKey);

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
            .Setup(s => s.RemoveFamilyMemberAsync(_familyIdKey, _person2IdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.RemoveMember(_familyIdKey, _person2IdKey);

        // Assert
        var unprocessableResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        var problemDetails = unprocessableResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    #endregion
}
