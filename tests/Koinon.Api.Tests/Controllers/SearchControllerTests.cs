using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.DTOs;
using Koinon.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

public class SearchControllerTests
{
    private readonly Mock<IGlobalSearchService> _searchServiceMock;
    private readonly Mock<ILogger<SearchController>> _loggerMock;
    private readonly SearchController _controller;

    public SearchControllerTests()
    {
        _searchServiceMock = new Mock<IGlobalSearchService>();
        _loggerMock = new Mock<ILogger<SearchController>>();

        _controller = new SearchController(
            _searchServiceMock.Object,
            _loggerMock.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task Search_WithValidQuery_ReturnsOkWithResults()
    {
        // Arrange
        const string query = "john";
        var expectedResults = new List<GlobalSearchResultDto>
        {
            new("People", "abc123", "John Doe", "john.doe@example.com", null),
            new("Families", "def456", "Doe Family", "123 Main St", null)
        };

        var expectedResponse = new GlobalSearchResponse(
            Results: expectedResults,
            TotalCount: 2,
            PageNumber: 1,
            PageSize: 20,
            CategoryCounts: new Dictionary<string, int>
            {
                { "People", 1 },
                { "Families", 1 },
                { "Groups", 0 }
            });

        _searchServiceMock
            .Setup(s => s.SearchAsync(query, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Search(query, null, 1, 20, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GlobalSearchResponse>().Subject;

        response.Results.Should().HaveCount(2);
        response.TotalCount.Should().Be(2);
        response.Results.First().Title.Should().Be("John Doe");
    }

    [Fact]
    public async Task Search_WithEmptyQuery_ReturnsBadRequest()
    {
        // Arrange
        const string query = "";

        // Act
        var result = await _controller.Search(query, null, 1, 20, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Detail.Should().Contain("Query parameter 'q' is required");
    }

    [Fact]
    public async Task Search_WithShortQuery_ReturnsBadRequest()
    {
        // Arrange
        const string query = "a";

        // Act
        var result = await _controller.Search(query, null, 1, 20, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Detail.Should().Contain("Query must be at least 2 characters long");
    }

    [Fact]
    public async Task Search_WithInvalidCategory_ReturnsBadRequest()
    {
        // Arrange
        const string query = "john";
        const string invalidCategory = "InvalidCategory";

        // Act
        var result = await _controller.Search(query, invalidCategory, 1, 20, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Detail.Should().Contain("Invalid category");
        problemDetails.Detail.Should().Contain("People, Families, Groups");
    }

    [Fact]
    public async Task Search_WithValidCategory_ReturnsFilteredResults()
    {
        // Arrange
        const string query = "john";
        const string category = "People";
        var expectedResults = new List<GlobalSearchResultDto>
        {
            new("People", "abc123", "John Doe", "john.doe@example.com", null)
        };

        var expectedResponse = new GlobalSearchResponse(
            Results: expectedResults,
            TotalCount: 1,
            PageNumber: 1,
            PageSize: 20,
            CategoryCounts: new Dictionary<string, int>
            {
                { "People", 1 },
                { "Families", 0 },
                { "Groups", 0 }
            });

        _searchServiceMock
            .Setup(s => s.SearchAsync(query, category, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Search(query, category, 1, 20, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GlobalSearchResponse>().Subject;

        response.Results.Should().HaveCount(1);
        response.Results.First().Category.Should().Be("People");
        response.CategoryCounts["People"].Should().Be(1);
    }

    [Fact]
    public async Task Search_WithInvalidPageNumber_ReturnsBadRequest()
    {
        // Arrange
        const string query = "john";
        const int invalidPageNumber = 0;

        // Act
        var result = await _controller.Search(query, null, invalidPageNumber, 20, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Detail.Should().Contain("Page number must be at least 1");
    }

    [Fact]
    public async Task Search_WithLargePageSize_ClampsTo100()
    {
        // Arrange
        const string query = "john";
        const int largePageSize = 500;
        var expectedResponse = new GlobalSearchResponse(
            Results: new List<GlobalSearchResultDto>(),
            TotalCount: 0,
            PageNumber: 1,
            PageSize: 100, // Should be clamped
            CategoryCounts: new Dictionary<string, int>
            {
                { "People", 0 },
                { "Families", 0 },
                { "Groups", 0 }
            });

        _searchServiceMock
            .Setup(s => s.SearchAsync(query, null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Search(query, null, 1, largePageSize, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GlobalSearchResponse>().Subject;

        response.PageSize.Should().Be(100);
        _searchServiceMock.Verify(
            s => s.SearchAsync(query, null, 1, 100, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
