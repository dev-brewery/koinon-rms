using Koinon.Application.Interfaces;
using Koinon.Infrastructure.Data;
using Koinon.Infrastructure.Options;
using Koinon.Infrastructure.Providers;
using Koinon.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Koinon.Infrastructure.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to configure Koinon infrastructure services.
/// Provides dependency injection registration for DbContext, providers, and repositories.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Koinon infrastructure services to the service collection.
    /// Registers DbContext with PostgreSQL provider, repositories, and unit of work.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="configuration">Application configuration for service options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKoinonInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        return services.AddKoinonInfrastructure(connectionString, configuration, options => { });
    }

    /// <summary>
    /// Adds Koinon infrastructure services to the service collection.
    /// Registers DbContext with PostgreSQL provider, repositories, and unit of work.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    [Obsolete("Use overload with IConfiguration parameter to enable service options")]
    public static IServiceCollection AddKoinonInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        return services.AddKoinonInfrastructure(connectionString, options => { });
    }

    /// <summary>
    /// Adds Koinon infrastructure services to the service collection with configuration options.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="configuration">Application configuration for service options.</param>
    /// <param name="configureOptions">Action to configure additional DbContext options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKoinonInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder> configureOptions)
    {
        // Register database provider
        services.AddSingleton<IDatabaseProvider, PostgreSqlProvider>();

        // Register DbContext with PostgreSQL
        services.AddDbContext<KoinonDbContext>((serviceProvider, options) =>
        {
            var provider = serviceProvider.GetRequiredService<IDatabaseProvider>();

            // Configure with provider-specific options
            provider.ConfigureDbContext(options, connectionString);

            // Apply any additional configuration
            configureOptions?.Invoke(options);
        });

        // Configure Twilio options
        services.Configure<TwilioOptions>(configuration.GetSection(TwilioOptions.SectionName));

        // Register SMS service (Singleton - has shared rate-limiting state via SemaphoreSlim)
        services.AddSingleton<ISmsService, TwilioSmsService>();

        // Future: Register repositories and unit of work (WU-1.3.4)
        // services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        // services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Adds Koinon infrastructure services to the service collection with configuration options.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="configureOptions">Action to configure additional DbContext options.</param>
    /// <returns>The service collection for chaining.</returns>
    [Obsolete("Use overload with IConfiguration parameter to enable service options")]
    public static IServiceCollection AddKoinonInfrastructure(
        this IServiceCollection services,
        string connectionString,
        Action<DbContextOptionsBuilder> configureOptions)
    {
        // Register database provider
        services.AddSingleton<IDatabaseProvider, PostgreSqlProvider>();

        // Register DbContext with PostgreSQL
        services.AddDbContext<KoinonDbContext>((serviceProvider, options) =>
        {
            var provider = serviceProvider.GetRequiredService<IDatabaseProvider>();

            // Configure with provider-specific options
            provider.ConfigureDbContext(options, connectionString);

            // Apply any additional configuration
            configureOptions?.Invoke(options);
        });

        // Future: Register repositories and unit of work (WU-1.3.4)
        // services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        // services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Adds Koinon infrastructure services with explicit database provider configuration.
    /// Allows using a custom database provider implementation.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="connectionString">Database connection string.</param>
    /// <param name="configuration">Application configuration for service options.</param>
    /// <param name="provider">Custom database provider implementation.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKoinonInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration,
        IDatabaseProvider provider)
    {
        // Register the custom provider
        services.AddSingleton(provider);

        // Register DbContext with the provider
        services.AddDbContext<KoinonDbContext>((serviceProvider, options) =>
        {
            var dbProvider = serviceProvider.GetRequiredService<IDatabaseProvider>();
            dbProvider.ConfigureDbContext(options, connectionString);
        });

        // Configure Twilio options
        services.Configure<TwilioOptions>(configuration.GetSection(TwilioOptions.SectionName));

        // Register SMS service (Singleton - has shared rate-limiting state via SemaphoreSlim)
        services.AddSingleton<ISmsService, TwilioSmsService>();

        // Future: Register repositories and unit of work
        // services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        // services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Adds Koinon infrastructure services with explicit database provider configuration.
    /// Allows using a custom database provider implementation.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="connectionString">Database connection string.</param>
    /// <param name="provider">Custom database provider implementation.</param>
    /// <returns>The service collection for chaining.</returns>
    [Obsolete("Use overload with IConfiguration parameter to enable service options")]
    public static IServiceCollection AddKoinonInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IDatabaseProvider provider)
    {
        // Register the custom provider
        services.AddSingleton(provider);

        // Register DbContext with the provider
        services.AddDbContext<KoinonDbContext>((serviceProvider, options) =>
        {
            var dbProvider = serviceProvider.GetRequiredService<IDatabaseProvider>();
            dbProvider.ConfigureDbContext(options, connectionString);
        });

        // Future: Register repositories and unit of work
        // services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        // services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Adds DbContext health checks for monitoring database connectivity.
    /// </summary>
    /// <param name="services">The service collection to add health checks to.</param>
    /// <param name="name">Name of the health check (defaults to "database").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKoinonDbContextHealthChecks(
        this IServiceCollection services,
        string name = "database")
    {
        services.AddHealthChecks()
            .AddDbContextCheck<KoinonDbContext>(
                name: name,
                tags: new[] { "db", "postgresql", "ready" });

        return services;
    }

    /// <summary>
    /// Adds Redis caching services for Koinon (to be implemented in future work units).
    /// </summary>
    /// <param name="services">The service collection to add caching to.</param>
    /// <param name="connectionString">Redis connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKoinonCaching(
        this IServiceCollection services,
        string connectionString)
    {
        // Future: Configure Redis caching
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = "Koinon:";
        });

        return services;
    }
}
