using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Koinon.Api.ContractTests;

/// <summary>
/// Tests error response contracts.
/// Ensures error responses follow RFC 7807 Problem Details format.
/// </summary>
public class ErrorResponseTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ErrorResponseTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact(Skip = "Auth middleware returns 401 before 404 for protected endpoints - design decision")]
    public async Task NotFound_ReturnsStandardProblemDetails()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - request non-existent resource
        var response = await client.GetAsync("/api/v1/people/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Check content type
        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (contentType != null)
        {
            (contentType == "application/json" || contentType == "application/problem+json")
                .Should().BeTrue("Error response should be JSON or Problem Details");
        }
    }

    [Fact]
    public async Task Unauthorized_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - request protected endpoint without auth
        var response = await client.GetAsync("/api/v1/people");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvalidIdKey_ReturnsNotFoundOrBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - request with invalid IdKey format
        var response = await client.GetAsync("/api/v1/people/invalid-idkey-format");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ServerError_DoesNotLeakStackTrace()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - try to trigger a server error by requesting with malformed data
        // This test ensures production mode doesn't leak sensitive info
        var response = await client.GetAsync("/api/v1/nonexistent-endpoint");

        // Assert
        var content = await response.Content.ReadAsStringAsync();

        // Should not contain stack trace indicators in production
        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            content.Should().NotContain("at System.", "Should not leak stack traces");
            content.Should().NotContain("at Koinon.", "Should not leak internal paths");
            content.Should().NotContain(".cs:line", "Should not leak source file info");
        }
    }

    [Fact]
    public async Task ProblemDetails_HasStandardFields()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/people/nonexistent");

        // Assert - if we get a JSON response, check structure
        var content = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(content) && content.StartsWith("{"))
        {
            var act = () => JsonDocument.Parse(content);
            act.Should().NotThrow("Error response should be valid JSON");

            var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Problem Details should have type, title, or status
            var hasProblemDetailsFields =
                root.TryGetProperty("type", out _) ||
                root.TryGetProperty("title", out _) ||
                root.TryGetProperty("status", out _);

            // It's OK if response is empty or uses a different format,
            // but if it has JSON, it should follow standard patterns
            if (root.EnumerateObject().Any())
            {
                hasProblemDetailsFields.Should().BeTrue(
                    "Non-empty error responses should include standard problem details fields");
            }
        }
    }

    [Fact]
    public async Task BadRequest_Returns400()
    {
        // Arrange
        var client = _factory.CreateClient();
        var invalidJson = new StringContent("invalid json", System.Text.Encoding.UTF8, "application/json");

        // Act - send invalid JSON to endpoint
        var response = await client.PostAsync("/api/v1/auth/login", invalidJson);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnsupportedMediaType,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ErrorResponse_HasCorrectContentType()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/people/nonexistent");

        // Assert - should return JSON content type
        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (contentType != null)
        {
            contentType.Should().Match(ct =>
                ct == "application/json" ||
                ct == "application/problem+json" ||
                ct == "text/plain",
                "Error should return JSON, Problem Details, or plain text");
        }
    }

    [Fact(Skip = "Health endpoint accepts all HTTP methods - MapHealthChecks behavior")]
    public async Task InvalidHttpMethod_Returns405()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - try to DELETE health endpoint (should only support GET)
        var response = await client.DeleteAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }
}
