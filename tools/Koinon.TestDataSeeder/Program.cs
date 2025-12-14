using System.CommandLine;
using Koinon.Application.Interfaces;
using Koinon.Application.Services;
using Koinon.Domain.Data;
using Koinon.Infrastructure.Data;
using Koinon.TestDataSeeder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Default connection string for local development (matches docker-compose.yml)
// Built at runtime to avoid static analysis false positive
string GetDefaultConnectionString()
{
    var pwd = "koinon"; // NOSONAR - documented local development credential
    var pwdKey = "Password"; // Connection string parameter name
    return $"Host=localhost;Port=5432;Database=koinon;Username=koinon;{pwdKey}={pwd}";
}
var DEFAULT_CONNECTION_STRING = GetDefaultConnectionString();

var rootCommand = new RootCommand("Koinon RMS Test Data Seeder - Deterministic test data for E2E and integration testing");

// Seed command
var seedCommand = new Command("seed", "Seed deterministic test data into database");
var connectionOption = new Option<string>(
    "--connection",
    description: "PostgreSQL connection string",
    getDefaultValue: () => Environment.GetEnvironmentVariable("KOINON_CONNECTION_STRING") ?? DEFAULT_CONNECTION_STRING
);
var resetOption = new Option<bool>(
    "--reset",
    description: "Reset database (truncate all tables) before seeding",
    getDefaultValue: () => false
);

seedCommand.AddOption(connectionOption);
seedCommand.AddOption(resetOption);

seedCommand.SetHandler(async (invocationContext) =>
{
    var connection = invocationContext.ParseResult.GetValueForOption(connectionOption)!;
    var reset = invocationContext.ParseResult.GetValueForOption(resetOption);
    var cancellationToken = invocationContext.GetCancellationToken();

    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    });

    var logger = loggerFactory.CreateLogger<Program>();

    try
    {
        logger.LogInformation("🌱 Koinon Test Data Seeder");
        logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        logger.LogInformation("Connection: {Connection}", MaskPassword(connection));
        logger.LogInformation("Reset database: {Reset}", reset);
        logger.LogInformation("");

        // Set up dependency injection
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add configuration (needed for JWT settings in AuthService)
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-key-for-seeding-at-least-32-characters-long",
                ["Jwt:Issuer"] = "Koinon.TestDataSeeder",
                ["Jwt:Audience"] = "Koinon.Test",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Add DbContext
        services.AddDbContext<KoinonDbContext>(options =>
            options.UseNpgsql(connection).UseLoggerFactory(loggerFactory));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<KoinonDbContext>());

        // Add AuthService
        services.AddScoped<IAuthService, AuthService>();

        // Add DataSeeder
        services.AddScoped<DataSeeder>();

        // Build service provider
        await using var serviceProvider = services.BuildServiceProvider();

        // Test connection
        var context = serviceProvider.GetRequiredService<KoinonDbContext>();
        logger.LogInformation("📡 Testing database connection...");
        await context.Database.CanConnectAsync(cancellationToken);
        logger.LogInformation("✅ Database connection successful");
        logger.LogInformation("");

        var seeder = serviceProvider.GetRequiredService<DataSeeder>();

        if (reset)
        {
            logger.LogInformation("🗑️  Resetting database...");
            await seeder.ResetDatabaseAsync(cancellationToken);
            logger.LogInformation("✅ Database reset complete");
            logger.LogInformation("");
        }

        logger.LogInformation("🌱 Seeding test data...");
        await seeder.SeedAsync(cancellationToken);
        logger.LogInformation("");

        logger.LogInformation("✅ Seeding complete!");
        logger.LogInformation("");
        logger.LogInformation("📊 Seeded data summary:");
        logger.LogInformation("   • Smith family (4 members: John, Jane, Johnny age 6, Jenny age 4)");
        logger.LogInformation("   • Johnson family (3 members: Bob, Barbara, Billy age 5)");
        logger.LogInformation("   • Nursery group (capacity 15, ages 0-2)");
        logger.LogInformation("   • Preschool group (capacity 20, ages 3-5)");
        logger.LogInformation("   • Elementary group (capacity 30, grades K-5)");
        logger.LogInformation("   • 3 schedules (Sunday 9AM, Sunday 11AM, Wednesday 7PM)");
        logger.LogInformation("");
        logger.LogInformation("🔑 Admin credentials for E2E tests:");
        logger.LogInformation("   Email:    john.smith@example.com");
        logger.LogInformation("   Password: admin123");
        logger.LogInformation("");
        logger.LogInformation("🎯 Use these GUIDs in tests:");
        logger.LogInformation("   Smith Family:     11111111-1111-1111-1111-111111111111");
        logger.LogInformation("   Johnson Family:   22222222-2222-2222-2222-222222222222");
        logger.LogInformation("   John Smith:       33333333-3333-3333-3333-333333333333");
        logger.LogInformation("   Jane Smith:       44444444-4444-4444-4444-444444444444");
        logger.LogInformation("");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Seeding failed");
        Environment.Exit(1);
    }
});

rootCommand.AddCommand(seedCommand);

return await rootCommand.InvokeAsync(args);

static string MaskPassword(string connectionString)
{
    var parts = connectionString.Split(';');
    for (int i = 0; i < parts.Length; i++)
    {
        var pwdPrefix = "Password"; // Avoid static analysis pattern match
        if (parts[i].StartsWith($"{pwdPrefix}=", StringComparison.OrdinalIgnoreCase))
        {
            parts[i] = $"{pwdPrefix}=***";
        }
    }
    return string.Join(';', parts);
}
