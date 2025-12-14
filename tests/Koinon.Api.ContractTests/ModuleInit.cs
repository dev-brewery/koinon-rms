using System.Runtime.CompilerServices;

namespace Koinon.Api.ContractTests;

/// <summary>
/// Module initializer to set up test environment before any tests run.
/// This ensures configuration is available when WebApplicationFactory creates the host.
/// </summary>
public static class ModuleInit
{
    /// <summary>
    /// Test-only JWT key for contract tests. Not a production secret.
    /// Must be at least 32 characters for HMAC-SHA256.
    /// </summary>
    private const string TestJwtKey = "this-is-a-test-jwt-key-with-minimum-32-characters-required-for-testing";

    [ModuleInitializer]
    public static void Initialize()
    {
        // Set environment variables for test configuration
        // These are read by Program.cs via WebApplicationBuilder.Configuration
        // Use __ for hierarchy (Jwt__Key becomes Jwt:Key in config)
        // Set BOTH Secret and Key to ensure one is found regardless of config order
        Environment.SetEnvironmentVariable("Jwt__Secret", TestJwtKey);
        Environment.SetEnvironmentVariable("Jwt__Key", TestJwtKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "Koinon.Test");
        Environment.SetEnvironmentVariable("Jwt__Audience", "Koinon.Test");
        Environment.SetEnvironmentVariable("UseHangfire", "false");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }
}
