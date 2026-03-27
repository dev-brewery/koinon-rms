using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Koinon.Api.Tests;

public class CorsConfigurationTests
{
    [Fact]
    public void AllowedOrigins_WhenNotConfigured_ReturnsEmptyArray()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        // Assert
        allowedOrigins.Should().BeEmpty();
    }

    [Fact]
    public void AllowedOrigins_WhenConfigured_ReturnsConfiguredOrigins()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                ["Cors:AllowedOrigins:1"] = "https://app.koinon.church"
            })
            .Build();

        // Act
        var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        // Assert
        allowedOrigins.Should().HaveCount(2);
        allowedOrigins.Should().Contain("http://localhost:5173");
        allowedOrigins.Should().Contain("https://app.koinon.church");
    }

    [Fact]
    public void AllowedOrigins_EmptyArrayInConfig_DoesNotCrash()
    {
        // Arrange — mirrors production appsettings.json where AllowedOrigins is []
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // An empty JSON array produces no keys, so the section exists but has no children
            })
            .Build();

        // Act
        var act = () =>
        {
            var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            return allowedOrigins.Length; // access result to ensure no lazy exception
        };

        // Assert
        act.Should().NotThrow();
    }
}
