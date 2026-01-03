using Hangfire;
using Hangfire.PostgreSql;
using Koinon.Application.Interfaces;
using Koinon.Application.Services;
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

        // Register SMS queue service
        services.AddScoped<ISmsQueueService, SmsQueueService>();

        // Register email sender service
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // Register background service for sending communications
        services.AddHostedService<CommunicationSenderBackgroundService>();

        // Register file storage service
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // Register session cleanup service
        services.AddScoped<ISessionCleanupService, SessionCleanupService>();

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

        // Register file storage service
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

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

        // Register SMS queue service
        services.AddScoped<ISmsQueueService, SmsQueueService>();

        // Register email sender service
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // Register background service for sending communications
        services.AddHostedService<CommunicationSenderBackgroundService>();

        // Register file storage service
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // Register session cleanup service
        services.AddScoped<ISessionCleanupService, SessionCleanupService>();

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

        // Register file storage service
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // Register session cleanup service
        services.AddScoped<ISessionCleanupService, SessionCleanupService>();

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

    /// <summary>
    /// Adds Hangfire background job processing with PostgreSQL storage.
    /// Configures retry policies with exponential backoff.
    /// </summary>
    /// <param name="services">The service collection to add Hangfire to.</param>
    /// <param name="connectionString">PostgreSQL connection string for Hangfire storage.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKoinonHangfire(
        this IServiceCollection services,
        string connectionString)
    {
        // Register the background job service
        services.AddScoped<IBackgroundJobService, HangfireJobService>();

        // Configure Hangfire with PostgreSQL storage
        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
            {
                options.UseNpgsqlConnection(connectionString);
            }, new PostgreSqlStorageOptions
            {
                SchemaName = "hangfire",
                QueuePollInterval = TimeSpan.FromSeconds(15),
                JobExpirationCheckInterval = TimeSpan.FromHours(1),
                CountersAggregateInterval = TimeSpan.FromMinutes(5),
                PrepareSchemaIfNecessary = true,
                InvisibilityTimeout = TimeSpan.FromMinutes(30)
            }));

        // Configure automatic retry with exponential backoff
        GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
        {
            Attempts = 3,
            DelaysInSeconds = new[] { 60, 300, 900 }, // 1 min, 5 min, 15 min
            LogEvents = true,
            OnAttemptsExceeded = AttemptsExceededAction.Fail
        });

        // Add Hangfire server for background job processing
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 2;
            options.Queues = new[] { "default", "critical", "low" };
            options.ServerName = $"{Environment.MachineName}:{Guid.NewGuid():N}";
        });

        // Register Hangfire health check
        services.AddHealthChecks()
            .AddHangfire(options =>
            {
                options.MinimumAvailableServers = 1;
            },
            name: "hangfire",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
            tags: new[] { "ready", "hangfire" });

        // Register hosted service to configure recurring jobs after startup
        services.AddHostedService<HangfireRecurringJobsService>();

        return services;
    }
}
