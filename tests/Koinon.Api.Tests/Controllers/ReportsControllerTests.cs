using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.Common;
using Koinon.Application.DTOs.Reports;
using Koinon.Application.Interfaces;
using Koinon.Domain.Data;
using Koinon.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Api.Tests.Controllers;

/// <summary>
/// Unit tests for ReportsController.
/// </summary>
public class ReportsControllerTests
{
    private readonly Mock<IReportService> _reportServiceMock;
    private readonly Mock<ILogger<ReportsController>> _loggerMock;
    private readonly ReportsController _controller;

    private readonly string _definitionIdKey = IdKeyHelper.Encode(1);
    private readonly string _runIdKey = IdKeyHelper.Encode(100);

    public ReportsControllerTests()
    {
        _reportServiceMock = new Mock<IReportService>();
        _loggerMock = new Mock<ILogger<ReportsController>>();
        _controller = new ReportsController(_reportServiceMock.Object, _loggerMock.Object);

        // Setup HttpContext
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GetDefinitions Tests

    [Fact]
    public async Task GetDefinitions_Returns200WithList()
    {
        // Arrange
        var definitions = new List<ReportDefinitionDto>
        {
            new() { IdKey = _definitionIdKey, Name = "Test Report", IsActive = true, IsSystem = false, ReportType = ReportType.AttendanceSummary, OutputFormat = ReportOutputFormat.Pdf },
            new() { IdKey = IdKeyHelper.Encode(2), Name = "Another Report", IsActive = true, IsSystem = false, ReportType = ReportType.GivingSummary, OutputFormat = ReportOutputFormat.Excel }
        };

        var pagedResult = new PagedResult<ReportDefinitionDto>(definitions, 2, 1, 25);

        _reportServiceMock
            .Setup(s => s.GetDefinitionsAsync(It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetDefinitions(includeInactive: false);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var items = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<ReportDefinitionDto>>().Subject.ToList();
        items.Should().HaveCount(2);
    }

    #endregion

    #region CreateDefinition Tests

    [Fact]
    public async Task CreateDefinition_WithValidRequest_Returns201()
    {
        // Arrange
        var request = new CreateReportDefinitionRequest
        {
            Name = "New Report",
            Description = "Test Description",
            ReportType = ReportType.AttendanceSummary,
            OutputFormat = ReportOutputFormat.Pdf
        };

        var createdDto = new ReportDefinitionDto
        {
            IdKey = _definitionIdKey,
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            IsSystem = false,
            ReportType = request.ReportType,
            OutputFormat = request.OutputFormat
        };

        _reportServiceMock
            .Setup(s => s.CreateDefinitionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReportDefinitionDto>.Success(createdDto));

        // Act
        var result = await _controller.CreateDefinition(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(ReportsController.GetDefinition));

        var response = createdResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var definition = dataProperty!.GetValue(response).Should().BeAssignableTo<ReportDefinitionDto>().Subject;
        definition.Name.Should().Be("New Report");
    }

    #endregion

    #region GetDefinition Tests

    [Fact]
    public async Task GetDefinition_WithValidIdKey_Returns200()
    {
        // Arrange
        var definitionDto = new ReportDefinitionDto
        {
            IdKey = _definitionIdKey,
            Name = "Test Report",
            IsActive = true,
            IsSystem = false,
            ReportType = ReportType.AttendanceSummary,
            OutputFormat = ReportOutputFormat.Pdf
        };

        _reportServiceMock
            .Setup(s => s.GetDefinitionAsync(_definitionIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definitionDto);

        // Act
        var result = await _controller.GetDefinition(_definitionIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var definition = dataProperty!.GetValue(response).Should().BeAssignableTo<ReportDefinitionDto>().Subject;
        definition.Name.Should().Be("Test Report");
    }

    [Fact]
    public async Task GetDefinition_WithInvalidIdKey_Returns404()
    {
        // Arrange
        _reportServiceMock
            .Setup(s => s.GetDefinitionAsync("invalid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReportDefinitionDto?)null);

        // Act
        var result = await _controller.GetDefinition("invalid");

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region UpdateDefinition Tests

    [Fact]
    public async Task UpdateDefinition_WithValidRequest_Returns200()
    {
        // Arrange
        var request = new UpdateReportDefinitionRequest
        {
            Name = "Updated Name"
        };

        var updatedDto = new ReportDefinitionDto
        {
            IdKey = _definitionIdKey,
            Name = "Updated Name",
            IsActive = true,
            IsSystem = false,
            ReportType = ReportType.AttendanceSummary,
            OutputFormat = ReportOutputFormat.Pdf
        };

        _reportServiceMock
            .Setup(s => s.UpdateDefinitionAsync(_definitionIdKey, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReportDefinitionDto>.Success(updatedDto));

        // Act
        var result = await _controller.UpdateDefinition(_definitionIdKey, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var definition = dataProperty!.GetValue(response).Should().BeAssignableTo<ReportDefinitionDto>().Subject;
        definition.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateDefinition_WithNotFound_Returns404()
    {
        // Arrange
        var request = new UpdateReportDefinitionRequest { Name = "Updated" };

        _reportServiceMock
            .Setup(s => s.UpdateDefinitionAsync(_definitionIdKey, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReportDefinitionDto>.Failure(Error.NotFound("ReportDefinition", _definitionIdKey)));

        // Act
        var result = await _controller.UpdateDefinition(_definitionIdKey, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateDefinition_WithSystemDefinition_Returns422()
    {
        // Arrange
        var request = new UpdateReportDefinitionRequest { Name = "Updated" };

        _reportServiceMock
            .Setup(s => s.UpdateDefinitionAsync(_definitionIdKey, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReportDefinitionDto>.Failure(Error.UnprocessableEntity("Cannot modify system report definitions")));

        // Act
        var result = await _controller.UpdateDefinition(_definitionIdKey, request);

        // Assert
        var unprocessableResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        unprocessableResult.StatusCode.Should().Be(422);
    }

    #endregion

    #region DeleteDefinition Tests

    [Fact]
    public async Task DeleteDefinition_WithValidIdKey_Returns204()
    {
        // Arrange
        _reportServiceMock
            .Setup(s => s.DeleteDefinitionAsync(_definitionIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.DeleteDefinition(_definitionIdKey);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteDefinition_WithNotFound_Returns404()
    {
        // Arrange
        _reportServiceMock
            .Setup(s => s.DeleteDefinitionAsync(_definitionIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.NotFound("ReportDefinition", _definitionIdKey)));

        // Act
        var result = await _controller.DeleteDefinition(_definitionIdKey);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteDefinition_WithSystemDefinition_Returns422()
    {
        // Arrange
        _reportServiceMock
            .Setup(s => s.DeleteDefinitionAsync(_definitionIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.UnprocessableEntity("Cannot delete system report definitions")));

        // Act
        var result = await _controller.DeleteDefinition(_definitionIdKey);

        // Assert
        var unprocessableResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        unprocessableResult.StatusCode.Should().Be(422);
    }

    #endregion

    #region RunReport Tests

    [Fact]
    public async Task RunReport_WithValidRequest_Returns201()
    {
        // Arrange
        var request = new RunReportRequest
        {
            ReportDefinitionIdKey = _definitionIdKey,
            Parameters = "{}"
        };

        var runDto = new ReportRunDto
        {
            IdKey = _runIdKey,
            ReportDefinitionIdKey = _definitionIdKey,
            ReportName = "Test Report",
            Status = ReportStatus.Pending
        };

        _reportServiceMock
            .Setup(s => s.RunReportAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReportRunDto>.Success(runDto));

        // Act
        var result = await _controller.RunReport(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(ReportsController.GetRun));

        var response = createdResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var run = dataProperty!.GetValue(response).Should().BeAssignableTo<ReportRunDto>().Subject;
        run.Status.Should().Be(ReportStatus.Pending);
    }

    [Fact]
    public async Task RunReport_WithInvalidDefinition_Returns404()
    {
        // Arrange
        var request = new RunReportRequest
        {
            ReportDefinitionIdKey = "invalid",
            Parameters = "{}"
        };

        _reportServiceMock
            .Setup(s => s.RunReportAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReportRunDto>.Failure(Error.NotFound("ReportDefinition", "invalid")));

        // Act
        var result = await _controller.RunReport(request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region GetRuns Tests

    [Fact]
    public async Task GetRuns_Returns200WithList()
    {
        // Arrange
        var runs = new List<ReportRunDto>
        {
            new() { IdKey = _runIdKey, ReportName = "Test Report", Status = ReportStatus.Completed, ReportDefinitionIdKey = _definitionIdKey },
            new() { IdKey = IdKeyHelper.Encode(101), ReportName = "Test Report", Status = ReportStatus.Pending, ReportDefinitionIdKey = _definitionIdKey }
        };

        var pagedResult = new PagedResult<ReportRunDto>(runs, 2, 1, 25);

        _reportServiceMock
            .Setup(s => s.GetRunsAsync(null, 1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetRuns();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var items = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<ReportRunDto>>().Subject.ToList();
        items.Should().HaveCount(2);
    }

    #endregion

    #region GetRun Tests

    [Fact]
    public async Task GetRun_WithValidIdKey_Returns200()
    {
        // Arrange
        var runDto = new ReportRunDto
        {
            IdKey = _runIdKey,
            ReportDefinitionIdKey = _definitionIdKey,
            ReportName = "Test Report",
            Status = ReportStatus.Completed
        };

        _reportServiceMock
            .Setup(s => s.GetRunAsync(_runIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(runDto);

        // Act
        var result = await _controller.GetRun(_runIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var run = dataProperty!.GetValue(response).Should().BeAssignableTo<ReportRunDto>().Subject;
        run.Status.Should().Be(ReportStatus.Completed);
    }

    [Fact]
    public async Task GetRun_WithInvalidIdKey_Returns404()
    {
        // Arrange
        _reportServiceMock
            .Setup(s => s.GetRunAsync("invalid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReportRunDto?)null);

        // Act
        var result = await _controller.GetRun("invalid");

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region DownloadReport Tests

    [Fact]
    public async Task DownloadReport_WithCompletedReport_ReturnsFileWithCorrectContentType()
    {
        // Arrange
        var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var fileName = "report.pdf";
        var mimeType = "application/pdf";

        _reportServiceMock
            .Setup(s => s.DownloadReportAsync(_runIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((fileStream, fileName, mimeType));

        // Act
        var result = await _controller.DownloadReport(_runIdKey);

        // Assert
        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.FileStream.Should().NotBeNull();
        fileResult.ContentType.Should().Be(mimeType);
        fileResult.FileDownloadName.Should().Be(fileName);
    }

    [Fact]
    public async Task DownloadReport_WithNotCompletedReport_Returns404()
    {
        // Arrange
        _reportServiceMock
            .Setup(s => s.DownloadReportAsync(_runIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Stream, string, string)?)null);

        // Act
        var result = await _controller.DownloadReport(_runIdKey);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DownloadReport_WithExcelFile_ReturnsCorrectMimeType()
    {
        // Arrange
        var fileStream = new MemoryStream(new byte[] { 0x50, 0x4B }); // PK header
        var fileName = "report.xlsx";
        var mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        _reportServiceMock
            .Setup(s => s.DownloadReportAsync(_runIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((fileStream, fileName, mimeType));

        // Act
        var result = await _controller.DownloadReport(_runIdKey);

        // Assert
        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.ContentType.Should().Be(mimeType);
        fileResult.FileDownloadName.Should().Be(fileName);
    }

    [Fact]
    public async Task DownloadReport_WithCsvFile_ReturnsCorrectMimeType()
    {
        // Arrange
        var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Name,Email\nJohn,john@test.com"));
        var fileName = "report.csv";
        var mimeType = "text/csv";

        _reportServiceMock
            .Setup(s => s.DownloadReportAsync(_runIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((fileStream, fileName, mimeType));

        // Act
        var result = await _controller.DownloadReport(_runIdKey);

        // Assert
        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.ContentType.Should().Be(mimeType);
        fileResult.FileDownloadName.Should().Be(fileName);
    }

    #endregion
}
