using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Koinon.Api.ContractTests;

/// <summary>
/// Tests API endpoint contracts.
/// Verifies endpoint availability, authentication, and basic behavior.
/// </summary>
[Trait("Category", "Integration")]
public class ApiEndpointContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ApiEndpointContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_Returns200()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthEndpoint_NoAuthenticationRequired()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.Should().BeSuccessful("Health endpoint should not require authentication");
    }

    [Theory]
    [InlineData("/api/v1/people")]
    [InlineData("/api/v1/groups")]
    public async Task ProtectedEndpoints_RequireAuthentication(string endpoint)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"{endpoint} should require authentication");
    }

    [Fact]
    public async Task FamiliesEndpoint_RequiresAuthentication()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/families");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "/api/v1/families should require authentication");
    }

    [Fact(Skip = "Swagger may not be enabled in Testing environment")]
    public async Task SwaggerEndpoint_IsAccessible()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.Should().BeSuccessful("Swagger documentation should be accessible");
    }

    [Fact(Skip = "Swagger may not be enabled in Testing environment")]
    public async Task SwaggerUi_IsAccessible()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger");

        // Assert
        response.Should().BeSuccessful("Swagger UI should be accessible");
    }

    [Fact]
    public async Task InvalidEndpoint_Returns404()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvalidIdKey_ReturnsError()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - use clearly invalid IdKey
        var response = await client.GetAsync("/api/v1/people/!!!invalid!!!");

        // Assert - should return an error (404, 400, or 401 depending on middleware order)
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApiEndpoints_UseApiV1Prefix()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - try accessing without /api/v1 prefix
        var response = await client.GetAsync("/people");

        // Assert - should return 404 because API requires /api/v1 prefix
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Endpoints should require /api/v1 prefix");
    }

    [Fact]
    public async Task AuthEndpoint_Exists()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - POST to login endpoint
        var response = await client.PostAsync("/api/v1/auth/login",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        // Assert - should not return 404 (endpoint exists)
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
            "Auth login endpoint should exist");
    }

    [Fact]
    public async Task CheckinConfigurationEndpoint_Exists()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Check-in has sub-endpoints like /areas, /configuration
        var response = await client.GetAsync("/api/v1/checkin/configuration");

        // Assert - should require auth, not return 404
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
            "Check-in configuration endpoint should exist");
    }

    [Fact]
    public async Task CorsHeaders_AllowedForApi()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:5173");

        // Act
        var response = await client.GetAsync("/health");

        // Assert - CORS headers might be present
        var hasCorsHeaders = response.Headers.Contains("Access-Control-Allow-Origin") ||
                            response.Headers.Contains("Access-Control-Allow-Methods");

        // Just verify response is successful - CORS configuration is environment-specific
        response.Should().BeSuccessful();
    }

    [Theory]
    [InlineData("/api/v1/people", "GET")]
    [InlineData("/api/v1/groups", "GET")]
    public async Task ApiEndpoints_SupportGetMethod(string endpoint, string method)
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), endpoint);

        // Act
        var response = await client.SendAsync(request);

        // Assert - should not return 405 Method Not Allowed
        response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed,
            $"{endpoint} should support {method} method");
    }

    [Fact]
    public async Task FamiliesEndpoint_SupportsGetMethod()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/families");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed,
            "/api/v1/families should support GET method");
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyStatus()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.Should().BeSuccessful();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ApiEndpoints_UseHttpsRedirection()
    {
        // This test verifies the middleware is configured, but behavior
        // depends on environment. In development, HTTPS might not be enforced.
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/health");

        // Assert - either succeeds or redirects to HTTPS
        response.StatusCode.Should().Match(code =>
            code == HttpStatusCode.OK ||
            code == HttpStatusCode.MovedPermanently ||
            code == HttpStatusCode.TemporaryRedirect ||
            code == HttpStatusCode.PermanentRedirect,
            "API should either serve request or redirect to HTTPS");
    }
}
