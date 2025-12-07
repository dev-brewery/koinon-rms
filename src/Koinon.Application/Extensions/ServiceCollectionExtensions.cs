using FluentValidation;
using Koinon.Application.DTOs.Auth;
using Koinon.Application.Interfaces;
using Koinon.Application.Services;
using Koinon.Application.Services.Common;
using Koinon.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace Koinon.Application.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to configure Koinon application services.
/// Provides dependency injection registration for application-layer services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Koinon application services to the service collection.
    /// Registers services for person management, groups, families, and check-in operations.
    /// NOTE: IUserContext must be registered by the API/Infrastructure layer before calling this method.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKoinonApplicationServices(
        this IServiceCollection services)
    {
        // NOTE: IUserContext is required by services but must be registered by the calling layer
        // (API or Infrastructure) before this method is called. This is intentional to keep
        // the Application layer independent of HTTP context or authentication implementation.

        // Authentication service
        services.AddScoped<IAuthService, AuthService>();

        // Validators
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();

        // Core entity services
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IFamilyService, FamilyService>();

        // Check-in common services (foundation classes for consistent patterns)
        services.AddScoped<CheckinDataLoader>();
        services.AddScoped<ConcurrentOperationHelper>();

        // Check-in services
        services.AddScoped<ICheckinConfigurationService, CheckinConfigurationService>();
        services.AddScoped<ICheckinSearchService, CheckinSearchService>();
        services.AddScoped<ICheckinAttendanceService, CheckinAttendanceService>();
        services.AddScoped<ILabelGenerationService, LabelGenerationService>();

        // Device/Kiosk validation service
        services.AddScoped<IDeviceValidationService, DeviceValidationService>();

        // Future: Add MediatR, AutoMapper, FluentValidation when needed
        // services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly));
        // services.AddAutoMapper(typeof(ServiceCollectionExtensions).Assembly);
        // services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);

        return services;
    }
}
