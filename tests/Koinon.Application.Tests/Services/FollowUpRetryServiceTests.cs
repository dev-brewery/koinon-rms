using System.Linq.Expressions;
using FluentAssertions;
using Koinon.Application.Interfaces;
using Koinon.Application.Services;
using Koinon.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

public class FollowUpRetryServiceTests
{
    private readonly Mock<IFollowUpService> _mockFollowUpService;
    private readonly Mock<IBackgroundJobService> _mockBackgroundJobService;
    private readonly Mock<ILogger<FollowUpRetryService>> _mockLogger;
    private readonly FollowUpRetryService _service;

    public FollowUpRetryServiceTests()
    {
        _mockFollowUpService = new Mock<IFollowUpService>();
        _mockBackgroundJobService = new Mock<IBackgroundJobService>();
        _mockLogger = new Mock<ILogger<FollowUpRetryService>>();

        _service = new FollowUpRetryService(
            _mockFollowUpService.Object,
            _mockBackgroundJobService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void QueueFollowUpCreation_FirstAttempt_ShouldEnqueueImmediately()
    {
        // Arrange
        const int personId = 1;
        const int attendanceId = 100;
        const string expectedJobId = "job-123";

        _mockBackgroundJobService
            .Setup(s => s.Enqueue<IFollowUpRetryService>(It.IsAny<Expression<Action<IFollowUpRetryService>>>()))
            .Returns(expectedJobId);

        // Act
        var jobId = _service.QueueFollowUpCreation(personId, attendanceId, 0);

        // Assert
        jobId.Should().Be(expectedJobId);
        _mockBackgroundJobService.Verify(
            s => s.Enqueue<IFollowUpRetryService>(It.IsAny<Expression<Action<IFollowUpRetryService>>>()),
            Times.Once);
    }

    [Fact]
    public void QueueFollowUpCreation_SecondAttempt_ShouldScheduleWithDelay()
    {
        // Arrange
        const int personId = 1;
        const int attendanceId = 100;
        const int attemptNumber = 1;
        const string expectedJobId = "job-456";

        _mockBackgroundJobService
            .Setup(s => s.Schedule<IFollowUpRetryService>(
                It.IsAny<Expression<Action<IFollowUpRetryService>>>(),
                It.Is<TimeSpan>(ts => ts == TimeSpan.FromMinutes(1))))
            .Returns(expectedJobId);

        // Act
        var jobId = _service.QueueFollowUpCreation(personId, attendanceId, attemptNumber);

        // Assert
        jobId.Should().Be(expectedJobId);
        _mockBackgroundJobService.Verify(
            s => s.Schedule<IFollowUpRetryService>(
                It.IsAny<Expression<Action<IFollowUpRetryService>>>(),
                TimeSpan.FromMinutes(1)),
            Times.Once);
    }

    [Fact]
    public void QueueFollowUpCreation_ThirdAttempt_ShouldScheduleWithLongerDelay()
    {
        // Arrange
        const int personId = 1;
        const int attendanceId = 100;
        const int attemptNumber = 2;
        const string expectedJobId = "job-789";

        _mockBackgroundJobService
            .Setup(s => s.Schedule<IFollowUpRetryService>(
                It.IsAny<Expression<Action<IFollowUpRetryService>>>(),
                It.Is<TimeSpan>(ts => ts == TimeSpan.FromMinutes(5))))
            .Returns(expectedJobId);

        // Act
        var jobId = _service.QueueFollowUpCreation(personId, attendanceId, attemptNumber);

        // Assert
        jobId.Should().Be(expectedJobId);
        _mockBackgroundJobService.Verify(
            s => s.Schedule<IFollowUpRetryService>(
                It.IsAny<Expression<Action<IFollowUpRetryService>>>(),
                TimeSpan.FromMinutes(5)),
            Times.Once);
    }

    [Fact]
    public void QueueFollowUpCreation_MaxAttemptsReached_ShouldThrowException()
    {
        // Arrange
        const int personId = 1;
        const int attendanceId = 100;
        const int maxAttempts = 5;

        // Act
        var act = () => _service.QueueFollowUpCreation(personId, attendanceId, maxAttempts);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Maximum retry attempts reached*");
    }

    [Fact]
    public async Task ProcessFollowUpCreationAsync_Success_ShouldCreateFollowUp()
    {
        // Arrange
        const int personId = 1;
        const int attendanceId = 100;
        const int attemptNumber = 0;

        var expectedFollowUp = new FollowUp
        {
            Id = 1,
            PersonId = personId,
            AttendanceId = attendanceId
        };

        _mockFollowUpService
            .Setup(s => s.CreateFollowUpAsync(personId, attendanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedFollowUp);

        // Act
        await _service.ProcessFollowUpCreationAsync(personId, attendanceId, attemptNumber, CancellationToken.None);

        // Assert
        _mockFollowUpService.Verify(
            s => s.CreateFollowUpAsync(personId, attendanceId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessFollowUpCreationAsync_FailureBeforeMaxAttempts_ShouldScheduleRetry()
    {
        // Arrange
        const int personId = 1;
        const int attendanceId = 100;
        const int attemptNumber = 1;

        _mockFollowUpService
            .Setup(s => s.CreateFollowUpAsync(personId, attendanceId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        _mockBackgroundJobService
            .Setup(s => s.Schedule<IFollowUpRetryService>(
                It.IsAny<Expression<Action<IFollowUpRetryService>>>(),
                It.IsAny<TimeSpan>()))
            .Returns("retry-job-id");

        // Act
        await _service.ProcessFollowUpCreationAsync(personId, attendanceId, attemptNumber, CancellationToken.None);

        // Assert - should schedule next attempt
        _mockBackgroundJobService.Verify(
            s => s.Schedule<IFollowUpRetryService>(
                It.IsAny<Expression<Action<IFollowUpRetryService>>>(),
                TimeSpan.FromMinutes(5)), // Attempt 2 = 5 min delay
            Times.Once);
    }

    [Fact]
    public async Task ProcessFollowUpCreationAsync_FinalAttemptFailure_ShouldThrowException()
    {
        // Arrange
        const int personId = 1;
        const int attendanceId = 100;
        const int finalAttempt = 4; // Last of 5 attempts (0-4)

        _mockFollowUpService
            .Setup(s => s.CreateFollowUpAsync(personId, attendanceId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var act = async () => await _service.ProcessFollowUpCreationAsync(personId, attendanceId, finalAttempt, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");

        // Should NOT schedule another retry
        _mockBackgroundJobService.Verify(
            s => s.Schedule<IFollowUpRetryService>(
                It.IsAny<Expression<Action<IFollowUpRetryService>>>(),
                It.IsAny<TimeSpan>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0, 0)]      // First attempt - immediate
    [InlineData(1, 1)]      // Second attempt - 1 min
    [InlineData(2, 5)]      // Third attempt - 5 min
    [InlineData(3, 15)]     // Fourth attempt - 15 min
    [InlineData(4, 60)]     // Fifth attempt - 60 min
    public void QueueFollowUpCreation_ExponentialBackoff_ShouldUseCorrectDelays(int attemptNumber, int expectedDelayMinutes)
    {
        // Arrange
        const int personId = 1;
        const int attendanceId = 100;

        if (attemptNumber == 0)
        {
            _mockBackgroundJobService
                .Setup(s => s.Enqueue<IFollowUpRetryService>(It.IsAny<Expression<Action<IFollowUpRetryService>>>()))
                .Returns("job-id");
        }
        else
        {
            _mockBackgroundJobService
                .Setup(s => s.Schedule<IFollowUpRetryService>(
                    It.IsAny<Expression<Action<IFollowUpRetryService>>>(),
                    TimeSpan.FromMinutes(expectedDelayMinutes)))
                .Returns("job-id");
        }

        // Act
        _service.QueueFollowUpCreation(personId, attendanceId, attemptNumber);

        // Assert
        if (attemptNumber == 0)
        {
            _mockBackgroundJobService.Verify(
                s => s.Enqueue<IFollowUpRetryService>(It.IsAny<Expression<Action<IFollowUpRetryService>>>()),
                Times.Once);
        }
        else
        {
            _mockBackgroundJobService.Verify(
                s => s.Schedule<IFollowUpRetryService>(
                    It.IsAny<Expression<Action<IFollowUpRetryService>>>(),
                    TimeSpan.FromMinutes(expectedDelayMinutes)),
                Times.Once);
        }
    }

    #region Input Validation Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void QueueFollowUpCreation_InvalidPersonId_ShouldThrowArgumentException(int invalidPersonId)
    {
        // Arrange
        const int attendanceId = 100;

        // Act
        var act = () => _service.QueueFollowUpCreation(invalidPersonId, attendanceId, 0);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("personId")
            .WithMessage("PersonId must be greater than zero.*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void QueueFollowUpCreation_InvalidAttendanceId_ShouldThrowArgumentException(int invalidAttendanceId)
    {
        // Arrange
        const int personId = 1;

        // Act
        var act = () => _service.QueueFollowUpCreation(personId, invalidAttendanceId, 0);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("attendanceId")
            .WithMessage("AttendanceId must be greater than zero.*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void QueueFollowUpCreation_NegativeAttemptNumber_ShouldThrowArgumentException(int invalidAttemptNumber)
    {
        // Arrange
        const int personId = 1;
        const int attendanceId = 100;

        // Act
        var act = () => _service.QueueFollowUpCreation(personId, attendanceId, invalidAttemptNumber);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("attemptNumber")
            .WithMessage("AttemptNumber cannot be negative.*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task ProcessFollowUpCreationAsync_InvalidPersonId_ShouldThrowArgumentException(int invalidPersonId)
    {
        // Arrange
        const int attendanceId = 100;
        const int attemptNumber = 0;

        // Act
        var act = async () => await _service.ProcessFollowUpCreationAsync(invalidPersonId, attendanceId, attemptNumber, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("personId")
            .WithMessage("PersonId must be greater than zero.*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task ProcessFollowUpCreationAsync_InvalidAttendanceId_ShouldThrowArgumentException(int invalidAttendanceId)
    {
        // Arrange
        const int personId = 1;
        const int attemptNumber = 0;

        // Act
        var act = async () => await _service.ProcessFollowUpCreationAsync(personId, invalidAttendanceId, attemptNumber, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("attendanceId")
            .WithMessage("AttendanceId must be greater than zero.*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task ProcessFollowUpCreationAsync_NegativeAttemptNumber_ShouldThrowArgumentException(int invalidAttemptNumber)
    {
        // Arrange
        const int personId = 1;
        const int attendanceId = 100;

        // Act
        var act = async () => await _service.ProcessFollowUpCreationAsync(personId, attendanceId, invalidAttemptNumber, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("attemptNumber")
            .WithMessage("AttemptNumber cannot be negative.*");
    }

    [Fact]
    public async Task ProcessFollowUpCreationAsync_CancellationRequested_ShouldPassCancellationToken()
    {
        // Arrange
        const int personId = 1;
        const int attendanceId = 100;
        const int attemptNumber = 0;

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        _mockFollowUpService
            .Setup(s => s.CreateFollowUpAsync(personId, attendanceId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        // The OperationCanceledException will be caught by the retry logic, which is expected behavior
        // The important thing is that the cancellation token is passed through
        await _service.ProcessFollowUpCreationAsync(personId, attendanceId, attemptNumber, cts.Token);

        // Verify that cancellation token was passed through to the service
        _mockFollowUpService.Verify(
            s => s.CreateFollowUpAsync(personId, attendanceId, It.Is<CancellationToken>(ct => ct.IsCancellationRequested)),
            Times.Once);
    }

    #endregion
}
