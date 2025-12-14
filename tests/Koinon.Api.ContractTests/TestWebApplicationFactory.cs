using Koinon.Application.Interfaces;
using Koinon.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Koinon.Api.ContractTests;

/// <summary>
/// Custom WebApplicationFactory for contract tests.
/// Configuration is provided by ModuleInit via environment variables.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove Hangfire services to avoid requiring PostgreSQL/Redis connections
            // SYNC OK: In-memory service collection
            var hangfireDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("Hangfire") == true)
                .ToList();

            foreach (var descriptor in hangfireDescriptors)
            {
                services.Remove(descriptor);
            }

            // Remove health check services that require external infrastructure
            // SYNC OK: In-memory service collection
            var healthCheckDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("HealthCheck") == true)
                .ToList();

            foreach (var descriptor in healthCheckDescriptors)
            {
                services.Remove(descriptor);
            }

            // Remove the real DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<KoinonDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Remove IApplicationDbContext registration
            var appDbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IApplicationDbContext));
            if (appDbContextDescriptor != null)
            {
                services.Remove(appDbContextDescriptor);
            }

            // Add in-memory database for tests
            services.AddDbContext<KoinonDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });

            // Register DbContext as IApplicationDbContext for Application layer
            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<KoinonDbContext>());

            // Add a simple health check that always returns healthy
            services.AddHealthChecks();
        });

        builder.UseEnvironment("Testing");
    }
}
