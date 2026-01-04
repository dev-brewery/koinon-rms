using FluentAssertions;
using Koinon.Api.Controllers;
using Koinon.Application.Common;
using Koinon.Application.DTOs;
using Koinon.Application.DTOs.Exports;
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
/// Unit tests for ExportsController.
/// </summary>
public class ExportsControllerTests
{
    private readonly Mock<IDataExportService> _dataExportServiceMock;
    private readonly Mock<ILogger<ExportsController>> _loggerMock;
    private readonly ExportsController _controller;

    private readonly string _exportJobIdKey = IdKeyHelper.Encode(1);

    public ExportsControllerTests()
    {
        _dataExportServiceMock = new Mock<IDataExportService>();
        _loggerMock = new Mock<ILogger<ExportsController>>();
        _controller = new ExportsController(_dataExportServiceMock.Object, _loggerMock.Object);

        // Setup HttpContext
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GetExports Tests

    [Fact]
    public async Task GetExports_Returns200WithPaginatedList()
    {
        // Arrange
        var exportJobs = new List<ExportJobDto>
        {
            new()
            {
                IdKey = _exportJobIdKey,
                ExportType = ExportType.People,
                Status = ReportStatus.Pending,
                OutputFormat = ReportOutputFormat.Excel,
                Parameters = "{}",
                CreatedDateTime = DateTime.UtcNow
            },
            new()
            {
                IdKey = IdKeyHelper.Encode(2),
                ExportType = ExportType.Families,
                Status = ReportStatus.Completed,
                OutputFormat = ReportOutputFormat.Csv,
                Parameters = "{}",
                CreatedDateTime = DateTime.UtcNow.AddHours(-1)
            }
        };

        var pagedResult = new PagedResult<ExportJobDto>(exportJobs, 2, 1, 25);

        _dataExportServiceMock
            .Setup(s => s.GetExportJobsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetExports();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var items = dataProperty!.GetValue(response).Should().BeAssignableTo<IEnumerable<ExportJobDto>>().Subject.ToList();
        items.Should().HaveCount(2);

        var metaProperty = response.GetType().GetProperty("meta");
        var meta = metaProperty!.GetValue(response)!;
        var totalCountProperty = meta.GetType().GetProperty("totalCount");
        totalCountProperty!.GetValue(meta).Should().Be(2);
    }

    [Fact]
    public async Task GetExports_WithInvalidPage_NormalizesToPage1()
    {
        // Arrange
        var pagedResult = new PagedResult<ExportJobDto>(new List<ExportJobDto>(), 0, 1, 25);

        _dataExportServiceMock
            .Setup(s => s.GetExportJobsAsync(1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetExports(page: 0);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _dataExportServiceMock.Verify(
            s => s.GetExportJobsAsync(1, 25, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetExports_WithInvalidPageSize_NormalizesTo25()
    {
        // Arrange
        var pagedResult = new PagedResult<ExportJobDto>(new List<ExportJobDto>(), 0, 1, 25);

        _dataExportServiceMock
            .Setup(s => s.GetExportJobsAsync(1, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetExports(pageSize: 200);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _dataExportServiceMock.Verify(
            s => s.GetExportJobsAsync(1, 25, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetExport Tests

    [Fact]
    public async Task GetExport_WithValidIdKey_Returns200()
    {
        // Arrange
        var exportJobDto = new ExportJobDto
        {
            IdKey = _exportJobIdKey,
            ExportType = ExportType.People,
            Status = ReportStatus.Completed,
            OutputFormat = ReportOutputFormat.Excel,
            Parameters = "{}",
            RecordCount = 150,
            CreatedDateTime = DateTime.UtcNow
        };

        _dataExportServiceMock
            .Setup(s => s.GetExportJobAsync(_exportJobIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportJobDto);

        // Act
        var result = await _controller.GetExport(_exportJobIdKey);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var exportJob = dataProperty!.GetValue(response).Should().BeAssignableTo<ExportJobDto>().Subject;
        exportJob.IdKey.Should().Be(_exportJobIdKey);
        exportJob.ExportType.Should().Be(ExportType.People);
    }

    [Fact]
    public async Task GetExport_WithInvalidIdKey_Returns404()
    {
        // Arrange
        _dataExportServiceMock
            .Setup(s => s.GetExportJobAsync(_exportJobIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExportJobDto?)null);

        // Act
        var result = await _controller.GetExport(_exportJobIdKey);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Export job not found");
    }

    #endregion

    #region StartExport Tests

    [Fact]
    public async Task StartExport_WithValidRequest_Returns201()
    {
        // Arrange
        var request = new StartExportRequest
        {
            ExportType = ExportType.People,
            OutputFormat = ReportOutputFormat.Excel,
            Fields = new List<string> { "FirstName", "LastName", "Email" },
            Filters = new Dictionary<string, string> { { "IsActive", "true" } }
        };

        var createdDto = new ExportJobDto
        {
            IdKey = _exportJobIdKey,
            ExportType = request.ExportType,
            Status = ReportStatus.Pending,
            OutputFormat = request.OutputFormat,
            Parameters = "{\"Fields\":[\"FirstName\",\"LastName\",\"Email\"],\"Filters\":{\"IsActive\":\"true\"}}",
            CreatedDateTime = DateTime.UtcNow
        };

        _dataExportServiceMock
            .Setup(s => s.StartExportAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExportJobDto>.Success(createdDto));

        // Act
        var result = await _controller.StartExport(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(ExportsController.GetExport));

        var response = createdResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var exportJob = dataProperty!.GetValue(response).Should().BeAssignableTo<ExportJobDto>().Subject;
        exportJob.ExportType.Should().Be(ExportType.People);
        exportJob.Status.Should().Be(ReportStatus.Pending);
    }

    [Fact]
    public async Task StartExport_WithValidationError_Returns400()
    {
        // Arrange
        var request = new StartExportRequest
        {
            ExportType = ExportType.People,
            OutputFormat = ReportOutputFormat.Excel
        };

        var error = new Error(
            "VALIDATION_ERROR",
            "Validation failed",
            new Dictionary<string, string[]>
            {
                { "Fields", new[] { "At least one field is required" } }
            });

        _dataExportServiceMock
            .Setup(s => s.StartExportAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExportJobDto>.Failure(error));

        // Act
        var result = await _controller.StartExport(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Validation failed");
        problemDetails.Extensions.Should().ContainKey("errors");
    }

    [Fact]
    public async Task StartExport_WithBusinessRuleViolation_Returns422()
    {
        // Arrange
        var request = new StartExportRequest
        {
            ExportType = ExportType.Custom,
            OutputFormat = ReportOutputFormat.Excel,
            Fields = new List<string> { "Field1" }
        };

        var error = new Error("CUSTOM_ENTITY_REQUIRED", "EntityType must be specified for Custom export type");

        _dataExportServiceMock
            .Setup(s => s.StartExportAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExportJobDto>.Failure(error));

        // Act
        var result = await _controller.StartExport(request);

        // Assert
        var unprocessableResult = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        unprocessableResult.StatusCode.Should().Be(422);
        var problemDetails = unprocessableResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("CUSTOM_ENTITY_REQUIRED");
    }

    #endregion

    #region DownloadExport Tests

    [Fact]
    public async Task DownloadExport_WithCompletedExport_ReturnsFile()
    {
        // Arrange
        var stream = new MemoryStream();
        var fileName = "people_export_2024-01-01.xlsx";
        var mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        _dataExportServiceMock
            .Setup(s => s.DownloadExportAsync(_exportJobIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((stream, fileName, mimeType));

        // Act
        var result = await _controller.DownloadExport(_exportJobIdKey);

        // Assert
        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.FileStream.Should().BeSameAs(stream);
        fileResult.FileDownloadName.Should().Be(fileName);
        fileResult.ContentType.Should().Be(mimeType);
        fileResult.EnableRangeProcessing.Should().BeTrue();
    }

    [Fact]
    public async Task DownloadExport_WithNonExistentExport_Returns404()
    {
        // Arrange
        _dataExportServiceMock
            .Setup(s => s.DownloadExportAsync(_exportJobIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Stream, string, string)?)null);

        // Act
        var result = await _controller.DownloadExport(_exportJobIdKey);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Export file not found");
    }

    [Fact]
    public async Task DownloadExport_WithPendingExport_Returns404()
    {
        // Arrange
        _dataExportServiceMock
            .Setup(s => s.DownloadExportAsync(_exportJobIdKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Stream, string, string)?)null);

        // Act
        var result = await _controller.DownloadExport(_exportJobIdKey);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("may still be processing");
    }

    #endregion

    #region GetAvailableFields Tests

    [Fact]
    public void GetAvailableFields_WithValidExportType_Returns200()
    {
        // Arrange
        var fields = new List<ExportFieldDto>
        {
            new()
            {
                FieldName = "FirstName",
                DisplayName = "First Name",
                DataType = "string",
                IsDefaultField = true
            },
            new()
            {
                FieldName = "LastName",
                DisplayName = "Last Name",
                DataType = "string",
                IsDefaultField = true
            },
            new()
            {
                FieldName = "Email",
                DisplayName = "Email Address",
                DataType = "string",
                IsDefaultField = true
            }
        };

        _dataExportServiceMock
            .Setup(s => s.GetAvailableFields(ExportType.People))
            .Returns(fields);

        // Act
        var result = _controller.GetAvailableFields(ExportType.People);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!;
        var dataProperty = response.GetType().GetProperty("data");
        var returnedFields = dataProperty!.GetValue(response).Should().BeAssignableTo<List<ExportFieldDto>>().Subject;
        returnedFields.Should().HaveCount(3);
        returnedFields.Should().Contain(f => f.FieldName == "FirstName");
    }

    [Fact]
    public void GetAvailableFields_WithInvalidExportType_Returns400()
    {
        // Arrange
        var invalidExportType = (ExportType)999;

        // Act
        var result = _controller.GetAvailableFields(invalidExportType);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Invalid export type");
        problemDetails.Detail.Should().Contain("Valid values are");
    }

    [Theory]
    [InlineData(ExportType.People)]
    [InlineData(ExportType.Families)]
    [InlineData(ExportType.Groups)]
    [InlineData(ExportType.Contributions)]
    [InlineData(ExportType.Attendance)]
    public void GetAvailableFields_WithAllValidExportTypes_Returns200(ExportType exportType)
    {
        // Arrange
        var fields = new List<ExportFieldDto>
        {
            new()
            {
                FieldName = "Field1",
                DisplayName = "Field 1",
                DataType = "string",
                IsDefaultField = true
            }
        };

        _dataExportServiceMock
            .Setup(s => s.GetAvailableFields(exportType))
            .Returns(fields);

        // Act
        var result = _controller.GetAvailableFields(exportType);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion
}
