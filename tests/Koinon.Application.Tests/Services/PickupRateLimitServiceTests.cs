using FluentAssertions;
using Koinon.Application.Configuration;
using Koinon.Application.Services;
using Koinon.Domain.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Koinon.Application.Tests.Services;

public class PickupRateLimitServiceTests
{
    private readonly IMemoryCache _cache;
    private readonly Mock<IOptions<RateLimitOptions>> _optionsMock;
    private readonly Mock<ILogger<PickupRateLimitService>> _loggerMock;
    private readonly PickupRateLimitService _service;

    // Use unique keys for each test to avoid interference between tests
    private string GetUniqueAttendanceId() => IdKeyHelper.Encode(Random.Shared.Next(1000000, 9999999));
    private string GetUniqueClientIp() => $"192.168.{Random.Shared.Next(1, 255)}.{Random.Shared.Next(1, 255)}";

    public PickupRateLimitServiceTests()
    {
        // Use real MemoryCache for testing (simpler than mocking)
        _cache = new MemoryCache(new MemoryCacheOptions());

        // Mock options with default values
        _optionsMock = new Mock<IOptions<RateLimitOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(new RateLimitOptions
        {
            MaxAttempts = 5,
            WindowMinutes = 15
        });

        _loggerMock = new Mock<ILogger<PickupRateLimitService>>();

        _service = new PickupRateLimitService(_cache, _optionsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void IsRateLimited_WithNoAttempts_ReturnsFalse()
    {
        // Arrange
        var attendanceIdKey = GetUniqueAttendanceId();
        var clientIp = GetUniqueClientIp();

        // Act
        var result = _service.IsRateLimited(attendanceIdKey, clientIp);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRateLimited_WithFourAttempts_ReturnsFalse()
    {
        // Arrange
        var attendanceIdKey = GetUniqueAttendanceId();
        var clientIp = GetUniqueClientIp();

        for (int i = 0; i < 4; i++)
        {
            _service.RecordFailedAttempt(attendanceIdKey, clientIp);
        }

        // Act
        var result = _service.IsRateLimited(attendanceIdKey, clientIp);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRateLimited_WithFiveAttempts_ReturnsTrue()
    {
        // Arrange
        var attendanceIdKey = GetUniqueAttendanceId();
        var clientIp = GetUniqueClientIp();

        for (int i = 0; i < 5; i++)
        {
            _service.RecordFailedAttempt(attendanceIdKey, clientIp);
        }

        // Act
        var result = _service.IsRateLimited(attendanceIdKey, clientIp);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsRateLimited_WithSixAttempts_ReturnsTrue()
    {
        // Arrange
        var attendanceIdKey = GetUniqueAttendanceId();
        var clientIp = GetUniqueClientIp();

        for (int i = 0; i < 6; i++)
        {
            _service.RecordFailedAttempt(attendanceIdKey, clientIp);
        }

        // Act
        var result = _service.IsRateLimited(attendanceIdKey, clientIp);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RecordFailedAttempt_TracksMultipleAttempts()
    {
        // Arrange
        var attendanceIdKey = GetUniqueAttendanceId();
        var clientIp = GetUniqueClientIp();

        // Act & Assert
        _service.RecordFailedAttempt(attendanceIdKey, clientIp);
        var result1 = _service.IsRateLimited(attendanceIdKey, clientIp);

        _service.RecordFailedAttempt(attendanceIdKey, clientIp);
        _service.RecordFailedAttempt(attendanceIdKey, clientIp);
        _service.RecordFailedAttempt(attendanceIdKey, clientIp);
        _service.RecordFailedAttempt(attendanceIdKey, clientIp);
        var result2 = _service.IsRateLimited(attendanceIdKey, clientIp);

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeTrue();
    }

    [Fact]
    public void ResetAttempts_ClearsFailedAttempts()
    {
        // Arrange
        var attendanceIdKey = GetUniqueAttendanceId();
        var clientIp = GetUniqueClientIp();

        for (int i = 0; i < 5; i++)
        {
            _service.RecordFailedAttempt(attendanceIdKey, clientIp);
        }
        var beforeReset = _service.IsRateLimited(attendanceIdKey, clientIp);

        // Act
        _service.ResetAttempts(attendanceIdKey, clientIp);
        var afterReset = _service.IsRateLimited(attendanceIdKey, clientIp);

        // Assert
        beforeReset.Should().BeTrue();
        afterReset.Should().BeFalse();
    }

    [Fact]
    public void IsRateLimited_TracksSeparatelyPerAttendanceIdKey()
    {
        // Arrange
        var attendance1 = GetUniqueAttendanceId();
        var attendance2 = GetUniqueAttendanceId();
        var clientIp = GetUniqueClientIp();

        for (int i = 0; i < 5; i++)
        {
            _service.RecordFailedAttempt(attendance1, clientIp);
        }

        // Act
        var result1 = _service.IsRateLimited(attendance1, clientIp);
        var result2 = _service.IsRateLimited(attendance2, clientIp);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeFalse();
    }

    [Fact]
    public void IsRateLimited_TracksSeparatelyPerClientIp()
    {
        // Arrange
        var attendanceIdKey = GetUniqueAttendanceId();
        var ip1 = GetUniqueClientIp();
        var ip2 = GetUniqueClientIp();

        for (int i = 0; i < 5; i++)
        {
            _service.RecordFailedAttempt(attendanceIdKey, ip1);
        }

        // Act
        var result1 = _service.IsRateLimited(attendanceIdKey, ip1);
        var result2 = _service.IsRateLimited(attendanceIdKey, ip2);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeFalse();
    }

    [Fact]
    public void GetRetryAfter_WhenNotRateLimited_ReturnsNull()
    {
        // Arrange
        var attendanceIdKey = GetUniqueAttendanceId();
        var clientIp = GetUniqueClientIp();

        // Act
        var result = _service.GetRetryAfter(attendanceIdKey, clientIp);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetRetryAfter_WhenRateLimited_ReturnsTimeSpan()
    {
        // Arrange
        var attendanceIdKey = GetUniqueAttendanceId();
        var clientIp = GetUniqueClientIp();

        for (int i = 0; i < 5; i++)
        {
            _service.RecordFailedAttempt(attendanceIdKey, clientIp);
        }

        // Act
        var result = _service.GetRetryAfter(attendanceIdKey, clientIp);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeGreaterThan(TimeSpan.Zero);
        result.Should().BeLessThanOrEqualTo(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void GetRetryAfter_WithFourAttempts_ReturnsNull()
    {
        // Arrange
        var attendanceIdKey = GetUniqueAttendanceId();
        var clientIp = GetUniqueClientIp();

        for (int i = 0; i < 4; i++)
        {
            _service.RecordFailedAttempt(attendanceIdKey, clientIp);
        }

        // Act
        var result = _service.GetRetryAfter(attendanceIdKey, clientIp);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ResetAttempts_WithNoExistingAttempts_DoesNotThrow()
    {
        // Arrange
        var attendanceIdKey = GetUniqueAttendanceId();
        var clientIp = GetUniqueClientIp();

        // Act
        var act = () => _service.ResetAttempts(attendanceIdKey, clientIp);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordFailedAttempt_WithDifferentCombinations_TracksIndependently()
    {
        // Arrange
        var attendance1 = GetUniqueAttendanceId();
        var attendance2 = GetUniqueAttendanceId();
        var ip1 = GetUniqueClientIp();
        var ip2 = GetUniqueClientIp();

        // Record 5 attempts for attendance1 + ip1
        for (int i = 0; i < 5; i++)
        {
            _service.RecordFailedAttempt(attendance1, ip1);
        }

        // Record 3 attempts for attendance1 + ip2
        for (int i = 0; i < 3; i++)
        {
            _service.RecordFailedAttempt(attendance1, ip2);
        }

        // Record 2 attempts for attendance2 + ip1
        for (int i = 0; i < 2; i++)
        {
            _service.RecordFailedAttempt(attendance2, ip1);
        }

        // Act & Assert
        _service.IsRateLimited(attendance1, ip1).Should().BeTrue();
        _service.IsRateLimited(attendance1, ip2).Should().BeFalse();
        _service.IsRateLimited(attendance2, ip1).Should().BeFalse();
        _service.IsRateLimited(attendance2, ip2).Should().BeFalse();
    }

    [Fact]
    public void IsRateLimited_LogsWarning_WhenRateLimitExceeded()
    {
        // Arrange
        var attendanceIdKey = GetUniqueAttendanceId();
        var clientIp = GetUniqueClientIp();

        for (int i = 0; i < 5; i++)
        {
            _service.RecordFailedAttempt(attendanceIdKey, clientIp);
        }

        // Act
        _service.IsRateLimited(attendanceIdKey, clientIp);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Rate limit exceeded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordFailedAttempt_LogsWarning()
    {
        // Arrange
        var attendanceIdKey = GetUniqueAttendanceId();
        var clientIp = GetUniqueClientIp();

        // Act
        _service.RecordFailedAttempt(attendanceIdKey, clientIp);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed pickup verification attempt recorded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ResetAttempts_LogsInformation_WhenAttemptsExist()
    {
        // Arrange
        var attendanceIdKey = GetUniqueAttendanceId();
        var clientIp = GetUniqueClientIp();

        _service.RecordFailedAttempt(attendanceIdKey, clientIp);

        // Act
        _service.ResetAttempts(attendanceIdKey, clientIp);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Pickup rate limit reset")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
