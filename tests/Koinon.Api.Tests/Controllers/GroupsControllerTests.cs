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

public class GroupsControllerTests
{
    private readonly Mock<IGroupService> _groupServiceMock;
    private readonly Mock<ILogger<GroupsController>> _loggerMock;
    private readonly GroupsController _controller;

    // Valid IdKeys for testing (using IdKeyHelper.Encode)
    private readonly string _groupIdKey = IdKeyHelper.Encode(123);
    private readonly string _group2IdKey = IdKeyHelper.Encode(456);
    private readonly string _personIdKey = IdKeyHelper.Encode(789);
    private readonly string _groupTypeIdKey = IdKeyHelper.Encode(10);
    private readonly string _campusIdKey = IdKeyHelper.Encode(5);
    private readonly string _parentGroupIdKey = IdKeyHelper.Encode(100);
    private readonly string _roleIdKey = IdKeyHelper.Encode(15);
    private readonly string _newGroupIdKey = IdKeyHelper.Encode(999);

    public GroupsControllerTests()
    {
        _groupServiceMock = new Mock<IGroupService>();
        _loggerMock = new Mock<ILogger<GroupsController>>();
        _controller = new GroupsController(_groupServiceMock.Object, _loggerMock.Object);

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
        var expectedResult = new PagedResult<GroupSummaryDto>(
            new List<GroupSummaryDto>
            {
                new() { IdKey = _groupIdKey, Name = "Youth Group", Description = "Middle school youth", IsActive = true, MemberCount = 15, GroupTypeName = "Serving Team" },
                new() { IdKey = _group2IdKey, Name = "Worship Team", Description = "Sunday worship", IsActive = true, MemberCount = 8, GroupTypeName = "Serving Team" }
            },
            totalCount: 2,
            page: 1,
            pageSize: 25
        );

        _groupServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<GroupSearchParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Search(
            query: "Youth",
            groupTypeId: null,
            campusId: null,
            parentGroupId: null,
            includeInactive: false,
            includeArchived: false,
            page: 1,
            pageSize: 25);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<GroupSummaryDto>>().Subject;
        pagedResult.Items.Should().HaveCount(2);
        pagedResult.TotalCount.Should().Be(2);
        pagedResult.Page.Should().Be(1);
        pagedResult.PageSize.Should().Be(25);
    }

    [Fact]
    public async Task Search_WithFilters_PassesFiltersToService()
    {
        // Arrange
        var expectedResult = new PagedResult<GroupSummaryDto>(
            new List<GroupSummaryDto>(),
            totalCount: 0,
            page: 1,
            pageSize: 25
        );

        GroupSearchParameters? capturedParameters = null;
        _groupServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<GroupSearchParameters>(), It.IsAny<CancellationToken>()))
            .Callback<GroupSearchParameters, CancellationToken>((p, _) => capturedParameters = p)
            .ReturnsAsync(expectedResult);

        // Act
        await _controller.Search(
            query: "test",
            groupTypeId: _groupTypeIdKey,
            campusId: _campusIdKey,
            parentGroupId: _parentGroupIdKey,
            includeInactive: true,
            includeArchived: true,
            page: 2,
            pageSize: 50);

        // Assert
        capturedParameters.Should().NotBeNull();
        capturedParameters!.Query.Should().Be("test");
        capturedParameters.GroupTypeId.Should().Be(_groupTypeIdKey);
        capturedParameters.CampusId.Should().Be(_campusIdKey);
        capturedParameters.ParentGroupId.Should().Be(_parentGroupIdKey);
        capturedParameters.IncludeInactive.Should().BeTrue();
        capturedParameters.IncludeArchived.Should().BeTrue();
        capturedParameters.Page.Should().Be(2);
        capturedParameters.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task Search_WithInvalidGroupTypeId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Search(
            query: null,
            groupTypeId: "invalid-idkey",
            campusId: null,
            parentGroupId: null);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Search_WithInvalidCampusId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Search(
            query: null,
            groupTypeId: null,
            campusId: "invalid-idkey",
            parentGroupId: null);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Search_WithInvalidParentGroupId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Search(
            query: null,
            groupTypeId: null,
            campusId: null,
            parentGroupId: "invalid-idkey");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region GetByIdKey Tests

    [Fact]
    public async Task GetByIdKey_WithExistingGroup_ReturnsOkWithGroup()
    {
        // Arrange
        var expectedGroup = new GroupDto
        {
            IdKey = _groupIdKey,
            Guid = Guid.NewGuid(),
            Name = "Youth Group",
            Description = "Middle school youth",
            IsActive = true,
            IsArchived = false,
            IsSecurityRole = false,
            IsPublic = true,
            AllowGuests = true,
            GroupCapacity = 20,
            Order = 0,
            GroupType = new GroupTypeDto
            {
                IdKey = _groupTypeIdKey,
                Guid = Guid.NewGuid(),
                Name = "Serving Team",
                Description = null,
                IsFamilyGroupType = false,
                AllowMultipleLocations = false,
                Roles = new List<GroupTypeRoleDto>()
            },
            Campus = null,
            ParentGroup = null,
            Members = new List<GroupMemberDto>(),
            ChildGroups = new List<GroupSummaryDto>(),
            CreatedDateTime = DateTime.UtcNow,
            ModifiedDateTime = null,
            ArchivedDateTime = null
        };

        _groupServiceMock
            .Setup(s => s.GetByIdKeyAsync(_groupIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGroup);

        // Act
        var result = await _controller.GetByIdKey(_groupIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var group = okResult.Value.Should().BeOfType<GroupDto>().Subject;
        group.IdKey.Should().Be(_groupIdKey);
        group.Name.Should().Be("Youth Group");
    }

    [Fact]
    public async Task GetByIdKey_WithNonExistentGroup_ReturnsNotFound()
    {
        // Arrange
        var nonExistentIdKey = IdKeyHelper.Encode(99999);
        _groupServiceMock
            .Setup(s => s.GetByIdKeyAsync(nonExistentIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GroupDto?)null);

        // Act
        var result = await _controller.GetByIdKey(nonExistentIdKey);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Detail.Should().Contain(nonExistentIdKey);
    }

    [Fact]
    public async Task GetByIdKey_WithInvalidIdKey_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetByIdKey("invalid-idkey");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedWithGroup()
    {
        // Arrange
        var request = new CreateGroupRequest
        {
            Name = "New Youth Group",
            Description = "High school youth",
            GroupTypeId = _groupTypeIdKey,
            IsActive = true,
            IsPublic = true,
            AllowGuests = false,
            Order = 0
        };

        var createdGroup = new GroupDto
        {
            IdKey = _newGroupIdKey,
            Guid = Guid.NewGuid(),
            Name = "New Youth Group",
            Description = "High school youth",
            IsActive = true,
            IsArchived = false,
            IsSecurityRole = false,
            IsPublic = true,
            AllowGuests = false,
            GroupCapacity = null,
            Order = 0,
            GroupType = new GroupTypeDto
            {
                IdKey = _groupTypeIdKey,
                Guid = Guid.NewGuid(),
                Name = "Serving Team",
                Description = null,
                IsFamilyGroupType = false,
                AllowMultipleLocations = false,
                Roles = new List<GroupTypeRoleDto>()
            },
            Campus = null,
            ParentGroup = null,
            Members = new List<GroupMemberDto>(),
            ChildGroups = new List<GroupSummaryDto>(),
            CreatedDateTime = DateTime.UtcNow,
            ModifiedDateTime = null,
            ArchivedDateTime = null
        };

        _groupServiceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GroupDto>.Success(createdGroup));

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(GroupsController.GetByIdKey));
        createdResult.RouteValues.Should().ContainKey("idKey");
        createdResult.RouteValues!["idKey"].Should().Be(_newGroupIdKey);

        var group = createdResult.Value.Should().BeOfType<GroupDto>().Subject;
        group.IdKey.Should().Be(_newGroupIdKey);
        group.Name.Should().Be("New Youth Group");
    }

    [Fact]
    public async Task Create_WithValidationError_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateGroupRequest
        {
            Name = "",
            GroupTypeId = _groupTypeIdKey
        };

        var error = new Error(
            "VALIDATION_ERROR",
            "One or more validation errors occurred",
            new Dictionary<string, string[]>
            {
                { "Name", new[] { "Group name is required" } }
            }
        );

        _groupServiceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GroupDto>.Failure(error));

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
        var request = new CreateGroupRequest
        {
            Name = "Test Group",
            GroupTypeId = _groupTypeIdKey
        };

        var error = Error.UnprocessableEntity("Cannot create family groups via GroupService");

        _groupServiceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GroupDto>.Failure(error));

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
    public async Task Update_WithValidRequest_ReturnsOkWithUpdatedGroup()
    {
        // Arrange
        var request = new UpdateGroupRequest
        {
            Name = "Youth Group Updated",
            Description = "Updated description"
        };

        var updatedGroup = new GroupDto
        {
            IdKey = _groupIdKey,
            Guid = Guid.NewGuid(),
            Name = "Youth Group Updated",
            Description = "Updated description",
            IsActive = true,
            IsArchived = false,
            IsSecurityRole = false,
            IsPublic = true,
            AllowGuests = true,
            GroupCapacity = 20,
            Order = 0,
            GroupType = new GroupTypeDto
            {
                IdKey = _groupTypeIdKey,
                Guid = Guid.NewGuid(),
                Name = "Serving Team",
                Description = null,
                IsFamilyGroupType = false,
                AllowMultipleLocations = false,
                Roles = new List<GroupTypeRoleDto>()
            },
            Campus = null,
            ParentGroup = null,
            Members = new List<GroupMemberDto>(),
            ChildGroups = new List<GroupSummaryDto>(),
            CreatedDateTime = DateTime.UtcNow.AddDays(-1),
            ModifiedDateTime = DateTime.UtcNow,
            ArchivedDateTime = null
        };

        _groupServiceMock
            .Setup(s => s.UpdateAsync(_groupIdKey, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GroupDto>.Success(updatedGroup));

        // Act
        var result = await _controller.Update(_groupIdKey, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var group = okResult.Value.Should().BeOfType<GroupDto>().Subject;
        group.IdKey.Should().Be(_groupIdKey);
        group.Name.Should().Be("Youth Group Updated");
        group.ModifiedDateTime.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_WithNonExistentGroup_ReturnsNotFound()
    {
        // Arrange
        var nonExistentIdKey = IdKeyHelper.Encode(99999);
        var request = new UpdateGroupRequest
        {
            Name = "Test Group"
        };

        var error = Error.NotFound("Group", nonExistentIdKey);

        _groupServiceMock
            .Setup(s => s.UpdateAsync(nonExistentIdKey, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GroupDto>.Failure(error));

        // Act
        var result = await _controller.Update(nonExistentIdKey, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Update_WithInvalidIdKey_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateGroupRequest
        {
            Name = "Test"
        };

        // Act
        var result = await _controller.Update("invalid-idkey", request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithExistingGroup_ReturnsNoContent()
    {
        // Arrange
        _groupServiceMock
            .Setup(s => s.DeleteAsync(_groupIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.Delete(_groupIdKey);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithNonExistentGroup_ReturnsNotFound()
    {
        // Arrange
        var nonExistentIdKey = IdKeyHelper.Encode(99999);
        var error = Error.NotFound("Group", nonExistentIdKey);

        _groupServiceMock
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
        var error = Error.UnprocessableEntity("Cannot delete system-protected groups");

        _groupServiceMock
            .Setup(s => s.DeleteAsync(_groupIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.Delete(_groupIdKey);

        // Assert
        var unprocessableResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        var problemDetails = unprocessableResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public async Task Delete_WithInvalidIdKey_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Delete("invalid-idkey");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region GetMembers Tests

    [Fact]
    public async Task GetMembers_WithExistingGroup_ReturnsOkWithMembers()
    {
        // Arrange
        var expectedMembers = new List<GroupMemberDto>
        {
            new()
            {
                IdKey = IdKeyHelper.Encode(1),
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
                    Name = "Leader",
                    IsLeader = true
                },
                Status = "Active",
                DateTimeAdded = DateTime.UtcNow.AddDays(-30),
                InactiveDateTime = null,
                Note = null
            }
        };

        _groupServiceMock
            .Setup(s => s.GetMembersAsync(_groupIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMembers);

        // Act
        var result = await _controller.GetMembers(_groupIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var members = okResult.Value.Should().BeAssignableTo<IReadOnlyList<GroupMemberDto>>().Subject;
        members.Should().HaveCount(1);
        members[0].Person.IdKey.Should().Be(_personIdKey);
    }

    [Fact]
    public async Task GetMembers_WithNoMembers_ReturnsEmptyList()
    {
        // Arrange
        _groupServiceMock
            .Setup(s => s.GetMembersAsync(_groupIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GroupMemberDto>());

        // Act
        var result = await _controller.GetMembers(_groupIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var members = okResult.Value.Should().BeAssignableTo<IReadOnlyList<GroupMemberDto>>().Subject;
        members.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMembers_WithInvalidIdKey_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetMembers("invalid-idkey");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region AddMember Tests

    [Fact]
    public async Task AddMember_WithValidRequest_ReturnsCreatedWithMember()
    {
        // Arrange
        var request = new AddGroupMemberRequest
        {
            PersonId = _personIdKey,
            RoleId = _roleIdKey,
            Note = "New member"
        };

        var createdMember = new GroupMemberDto
        {
            IdKey = IdKeyHelper.Encode(1),
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
                Name = "Member",
                IsLeader = false
            },
            Status = "Active",
            DateTimeAdded = DateTime.UtcNow,
            InactiveDateTime = null,
            Note = "New member"
        };

        _groupServiceMock
            .Setup(s => s.AddMemberAsync(_groupIdKey, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GroupMemberDto>.Success(createdMember));

        // Act
        var result = await _controller.AddMember(_groupIdKey, request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(GroupsController.GetMembers));
        createdResult.RouteValues.Should().ContainKey("idKey");
        createdResult.RouteValues!["idKey"].Should().Be(_groupIdKey);

        var member = createdResult.Value.Should().BeOfType<GroupMemberDto>().Subject;
        member.Person.IdKey.Should().Be(_personIdKey);
    }

    [Fact]
    public async Task AddMember_WithNonExistentGroup_ReturnsNotFound()
    {
        // Arrange
        var request = new AddGroupMemberRequest
        {
            PersonId = _personIdKey,
            RoleId = _roleIdKey
        };

        var error = Error.NotFound("Group", _groupIdKey);

        _groupServiceMock
            .Setup(s => s.AddMemberAsync(_groupIdKey, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GroupMemberDto>.Failure(error));

        // Act
        var result = await _controller.AddMember(_groupIdKey, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task AddMember_WithInvalidIdKey_ReturnsBadRequest()
    {
        // Arrange
        var request = new AddGroupMemberRequest
        {
            PersonId = _personIdKey,
            RoleId = _roleIdKey
        };

        // Act
        var result = await _controller.AddMember("invalid-idkey", request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region RemoveMember Tests

    [Fact]
    public async Task RemoveMember_WithExistingMember_ReturnsNoContent()
    {
        // Arrange
        _groupServiceMock
            .Setup(s => s.RemoveMemberAsync(_groupIdKey, _personIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.RemoveMember(_groupIdKey, _personIdKey);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveMember_WithNonExistentMember_ReturnsNotFound()
    {
        // Arrange
        var error = Error.NotFound("GroupMember", $"Person {_personIdKey} in Group {_groupIdKey}");

        _groupServiceMock
            .Setup(s => s.RemoveMemberAsync(_groupIdKey, _personIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        // Act
        var result = await _controller.RemoveMember(_groupIdKey, _personIdKey);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task RemoveMember_WithInvalidGroupIdKey_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.RemoveMember("invalid-idkey", _personIdKey);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task RemoveMember_WithInvalidPersonIdKey_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.RemoveMember(_groupIdKey, "invalid-idkey");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion

    #region GetChildren Tests

    [Fact]
    public async Task GetChildren_WithExistingChildGroups_ReturnsOkWithChildren()
    {
        // Arrange
        var expectedChildren = new List<GroupSummaryDto>
        {
            new()
            {
                IdKey = _group2IdKey,
                Name = "Sub Group 1",
                Description = "First child group",
                IsActive = true,
                MemberCount = 5,
                GroupTypeName = "Small Group"
            }
        };

        _groupServiceMock
            .Setup(s => s.GetChildGroupsAsync(_groupIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedChildren);

        // Act
        var result = await _controller.GetChildren(_groupIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var children = okResult.Value.Should().BeAssignableTo<IReadOnlyList<GroupSummaryDto>>().Subject;
        children.Should().HaveCount(1);
        children[0].Name.Should().Be("Sub Group 1");
    }

    [Fact]
    public async Task GetChildren_WithNoChildren_ReturnsEmptyList()
    {
        // Arrange
        _groupServiceMock
            .Setup(s => s.GetChildGroupsAsync(_groupIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GroupSummaryDto>());

        // Act
        var result = await _controller.GetChildren(_groupIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var children = okResult.Value.Should().BeAssignableTo<IReadOnlyList<GroupSummaryDto>>().Subject;
        children.Should().BeEmpty();
    }

    [Fact]
    public async Task GetChildren_WithInvalidIdKey_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetChildren("invalid-idkey");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    #endregion
}
