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

public class LocationsControllerTests
{
    private readonly Mock<ILocationService> _mockService;
    private readonly Mock<ILogger<LocationsController>> _mockLogger;
    private readonly LocationsController _controller;

    public LocationsControllerTests()
    {
        _mockService = new Mock<ILocationService>();
        _mockLogger = new Mock<ILogger<LocationsController>>();
        _controller = new LocationsController(_mockService.Object, _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult_WithLocations()
    {
        // Arrange
        var locations = new List<LocationSummaryDto> 
        { 
            new() 
            { 
                IdKey = "loc-1", 
                Name = "Test Location", 
                IsActive = true 
            } 
        };
        _mockService.Setup(x => x.GetAllAsync(It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(locations);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTree_ReturnsOkResult_WithTree()
    {
        // Arrange
        var tree = new List<LocationDto>
        {
            new()
            {
                IdKey = "root-1",
                Guid = Guid.NewGuid(),
                Name = "Root",
                IsActive = true,
                Order = 1,
                CreatedDateTime = DateTime.UtcNow,
                Children = new List<LocationDto>()
            }
        };
        _mockService.Setup(x => x.GetTreeAsync(It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<LocationDto>>.Success(tree));

        // Act
        var result = await _controller.GetTree();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdKey_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var location = new LocationDto
        {
            IdKey = "valid-key",
            Guid = Guid.NewGuid(),
            Name = "Test",
            IsActive = true,
            Order = 1,
            CreatedDateTime = DateTime.UtcNow,
            Children = new List<LocationDto>()
        };
        _mockService.Setup(x => x.GetByIdKeyAsync("valid-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LocationDto>.Success(location));

        // Act
        var result = await _controller.GetByIdKey("valid-key");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdKey_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(x => x.GetByIdKeyAsync("invalid-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LocationDto>.Failure(Error.NotFound("Location", "invalid-key")));

        // Act
        var result = await _controller.GetByIdKey("invalid-key");

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateLocationRequest { Name = "New Location" };
        var createdDto = new LocationDto
        {
            IdKey = "new-key",
            Guid = Guid.NewGuid(),
            Name = "New Location",
            IsActive = true,
            Order = 1,
            CreatedDateTime = DateTime.UtcNow,
            Children = new List<LocationDto>()
        };
        _mockService.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LocationDto>.Success(createdDto));

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(LocationsController.GetByIdKey));
        createdResult.RouteValues!["idKey"].Should().Be("new-key");
    }

    [Fact]
    public async Task Create_WithValidationError_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateLocationRequest { Name = "" };
        var error = Error.FromFluentValidation(new FluentValidation.Results.ValidationResult(new[]
        {
            new FluentValidation.Results.ValidationFailure("Name", "Required")
        }));

        _mockService.Setup(x => x.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LocationDto>.Failure(error));

        // Act
        var result = await _controller.Create(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task Update_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new UpdateLocationRequest { Name = "Updated" };
        var updatedDto = new LocationDto
        {
            IdKey = "key",
            Guid = Guid.NewGuid(),
            Name = "Updated",
            IsActive = true,
            Order = 1,
            CreatedDateTime = DateTime.UtcNow,
            Children = new List<LocationDto>()
        };

        _mockService.Setup(x => x.UpdateAsync("key", request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LocationDto>.Success(updatedDto));

        // Act
        var result = await _controller.Update("key", request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        _mockService.Setup(x => x.DeleteAsync("key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.Delete("key");

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}
