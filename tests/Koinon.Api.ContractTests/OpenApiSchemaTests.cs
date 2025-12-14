using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Koinon.Api.ContractTests;

/// <summary>
/// Tests OpenAPI schema contracts.
/// Ensures API documentation matches expected structure.
/// </summary>
public class OpenApiSchemaTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public OpenApiSchemaTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact(Skip = "Swagger/OpenAPI not yet configured in API - pending implementation")]
    public async Task OpenApiSchema_HasSwaggerEndpoint()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.Should().BeSuccessful();
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact(Skip = "Swagger/OpenAPI not yet configured in API - pending implementation")]
    public async Task OpenApiSchema_HasRequiredPaths()
    {
        // Arrange
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();
        var schema = JsonDocument.Parse(content);

        // Assert
        var paths = schema.RootElement.GetProperty("paths");

        // Check for core API paths
        paths.TryGetProperty("/api/v1/people", out _).Should().BeTrue("People endpoint should exist");
        paths.TryGetProperty("/api/v1/families", out _).Should().BeTrue("Families endpoint should exist");
        paths.TryGetProperty("/api/v1/groups", out _).Should().BeTrue("Groups endpoint should exist");
        paths.TryGetProperty("/api/v1/checkin", out _).Should().BeTrue("Check-in endpoint should exist");
    }

    [Fact(Skip = "Swagger/OpenAPI not yet configured in API - pending implementation")]
    public async Task OpenApiSchema_HasSecurityDefinitions()
    {
        // Arrange
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();
        var schema = JsonDocument.Parse(content);

        // Assert
        schema.RootElement.TryGetProperty("components", out var components).Should().BeTrue();
        components.TryGetProperty("securitySchemes", out var securitySchemes).Should().BeTrue("OpenAPI schema should define security schemes");

        // Should have JWT Bearer authentication
        securitySchemes.TryGetProperty("Bearer", out _).Should().BeTrue("Should have Bearer security scheme");
    }

    [Fact(Skip = "Swagger/OpenAPI not yet configured in API - pending implementation")]
    public async Task OpenApiSchema_UsesIdKeyPattern()
    {
        // Arrange
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - should use {idKey} not {id} in paths
        content.Should().Contain("{idKey}", "API paths should use IdKey pattern");
        content.Should().NotContain("/api/v1/people/{id}", "API should not expose integer IDs");
    }

    [Fact(Skip = "Swagger/OpenAPI not yet configured in API - pending implementation")]
    public async Task OpenApiSchema_HasApiInfo()
    {
        // Arrange
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();
        var schema = JsonDocument.Parse(content);

        // Assert
        var info = schema.RootElement.GetProperty("info");
        info.GetProperty("title").GetString().Should().NotBeNullOrEmpty();
        info.GetProperty("version").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact(Skip = "Swagger/OpenAPI not yet configured in API - pending implementation")]
    public async Task OpenApiSchema_HasSchemas()
    {
        // Arrange
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();
        var schema = JsonDocument.Parse(content);

        // Assert
        schema.RootElement.TryGetProperty("components", out var components).Should().BeTrue();
        components.TryGetProperty("schemas", out var schemas).Should().BeTrue("OpenAPI schema should define component schemas");

        // Check for key DTOs
        schemas.TryGetProperty("PersonDto", out _).Should().BeTrue("Should have PersonDto schema");
        schemas.TryGetProperty("FamilyDto", out _).Should().BeTrue("Should have FamilyDto schema");
        schemas.TryGetProperty("GroupDto", out _).Should().BeTrue("Should have GroupDto schema");
    }

    [Fact(Skip = "Swagger/OpenAPI not yet configured in API - pending implementation")]
    public async Task OpenApiSchema_IsValidJson()
    {
        // Arrange
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();

        // Act & Assert - should parse without exception
        var act = () => JsonDocument.Parse(content);
        act.Should().NotThrow("OpenAPI schema should be valid JSON");
    }

    [Fact(Skip = "Swagger/OpenAPI not yet configured in API - pending implementation")]
    public async Task OpenApiSchema_HasCorrectOpenApiVersion()
    {
        // Arrange
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var content = await response.Content.ReadAsStringAsync();
        var schema = JsonDocument.Parse(content);

        // Assert
        var openapi = schema.RootElement.GetProperty("openapi").GetString();
        openapi.Should().StartWith("3.", "Should use OpenAPI 3.x specification");
    }
}
